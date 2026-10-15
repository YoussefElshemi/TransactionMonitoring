using Core.Enums;
using Core.Exceptions;
using Core.Factories;
using Core.Helpers;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Models.Accounts;
using Core.Models.Common;
using Core.Models.Payments;
using Core.ValueObjects;

namespace Core.Services;

public class AccountService(
    IAccountEntryRepository accountEntryRepository,
    IInvoiceRepository invoiceRepository,
    ICreditNoteRepository creditNoteRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IAccountService
{
    public Task<List<AccountBalance>> GetBalancesAsync(OrganizationId organizationId, CancellationToken stoppingToken)
    {
        return accountEntryRepository.GetBalancesAsync(organizationId, stoppingToken);
    }

    public async Task<AccountEntry> AllocateAsync(AllocatePaymentRequest allocatePaymentRequest, CancellationToken stoppingToken)
    {
        var invoice = await invoiceRepository.GetAsync(allocatePaymentRequest.InvoiceId, stoppingToken);

        if (invoice.InvoiceStatus is not InvoiceStatus.Issued)
        {
            throw new InvoiceNotIssuedException(invoice.InvoiceId);
        }

        var balance = await accountEntryRepository.GetBalanceAsync(
            allocatePaymentRequest.OrganizationId, invoice.Currency, stoppingToken);

        var outstanding = await GetOutstandingAsync(invoice.InvoiceId, invoice.Amount, stoppingToken);

        if (outstanding == 0)
        {
            throw new InvoiceSettledException(invoice.InvoiceId, invoice.InvoiceNumber);
        }

        var amount = allocatePaymentRequest.Amount.HasValue
            ? PaymentAllocator.Allocatable(balance, outstanding, allocatePaymentRequest.Amount.Value)
            : PaymentAllocator.Allocatable(balance, outstanding);

        if (amount <= 0)
        {
            throw new InsufficientBalanceException(
                allocatePaymentRequest.OrganizationId,
                invoice.Currency,
                balance,
                allocatePaymentRequest.Amount.HasValue ? allocatePaymentRequest.Amount.Value : outstanding);
        }

        var accountEntry = AccountEntryFactory.Allocation(
            allocatePaymentRequest.OrganizationId,
            invoice.Currency,
            amount,
            invoice.InvoiceId,
            paymentId: null,
            new EntryDescription(DescribeAllocation(invoice.InvoiceNumber, invoice.InvoiceId)),
            allocatePaymentRequest.AllocatedByUserId,
            timeProvider);

        await accountEntryRepository.AddAsync(accountEntry, stoppingToken);

        // IsProcessed caches what the ledger says; the collection and arrears runs read it.
        if (outstanding - amount <= 0)
        {
            await invoiceRepository.ProcessAsync(invoice.InvoiceId, stoppingToken);
        }

        await unitOfWork.SaveChangesAsync(stoppingToken);

        return accountEntry;
    }

    public async Task<AccountEntry> RefundAsync(RefundBalanceRequest refundBalanceRequest, CancellationToken stoppingToken)
    {
        var balance = await accountEntryRepository.GetBalanceAsync(
            refundBalanceRequest.OrganizationId, refundBalanceRequest.Currency, stoppingToken);

        var requested = Math.Abs(refundBalanceRequest.Amount);

        if (requested > balance)
        {
            throw new InsufficientBalanceException(
                refundBalanceRequest.OrganizationId, refundBalanceRequest.Currency, balance, requested);
        }

        var accountEntry = AccountEntryFactory.Refund(
            refundBalanceRequest.OrganizationId,
            refundBalanceRequest.Currency,
            requested,
            refundBalanceRequest.Reason,
            refundBalanceRequest.RefundedByUserId,
            timeProvider);

        await accountEntryRepository.AddAsync(accountEntry, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);

        return accountEntry;
    }

    public async Task<PagedResponse<AccountBalance>> ListUnappliedAsync(
        ListUnappliedReceiptRequest listUnappliedReceiptRequest, CancellationToken stoppingToken)
    {
        var totalCount = await accountEntryRepository.GetUnappliedCountAsync(listUnappliedReceiptRequest, stoppingToken);
        var balances = await accountEntryRepository.ListUnappliedAsync(listUnappliedReceiptRequest, stoppingToken);

        return new PagedResponse<AccountBalance>
        {
            PageNumber = listUnappliedReceiptRequest.PageNumber,
            PageSize = listUnappliedReceiptRequest.PageSize,
            PageCount = listUnappliedReceiptRequest.GetPageCount(totalCount),
            TotalCount = totalCount,
            HasMore = new HasMore(listUnappliedReceiptRequest.PageNumber * listUnappliedReceiptRequest.PageSize < totalCount),
            Data = balances
        };
    }

    public Task<TotalCount> GetUnappliedCountAsync(
        ListUnappliedReceiptRequest listUnappliedReceiptRequest, CancellationToken stoppingToken)
    {
        return accountEntryRepository.GetUnappliedCountAsync(listUnappliedReceiptRequest, stoppingToken);
    }

    public Task<PagedResponse<AccountEntry>> ListAsync(ListAccountEntryRequest listAccountEntryRequest, CancellationToken stoppingToken)
    {
        return accountEntryRepository.ListAsync(listAccountEntryRequest, stoppingToken);
    }

    public Task<TotalCount> GetCountAsync(ListAccountEntryRequest listAccountEntryRequest, CancellationToken stoppingToken)
    {
        return accountEntryRepository.GetCountAsync(listAccountEntryRequest, stoppingToken);
    }

    private async Task<OutstandingAmount> GetOutstandingAsync(InvoiceId invoiceId, decimal amount, CancellationToken stoppingToken)
    {
        var allocated = await accountEntryRepository.GetAllocatedAsync(invoiceId, stoppingToken);
        var credited = await creditNoteRepository.GetCreditedAmountAsync(invoiceId, stoppingToken);

        return new OutstandingAmount(Math.Max(amount - allocated - credited, 0));
    }

    private static string DescribeAllocation(InvoiceNumber? invoiceNumber, InvoiceId invoiceId)
    {
        return $"Allocated to invoice {invoiceNumber?.ToString() ?? invoiceId.ToString()}";
    }
}
