using Core.Configs;
using Core.Documents;
using Core.Enums;
using Core.Exceptions;
using Core.Factories;
using Core.Helpers;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Localization;
using Core.Models.Accounts;
using Core.Models.Common;
using Core.Models.Invoices;
using Core.Models.Payments;
using Core.Models.Providers;
using Core.ValueObjects;
using Microsoft.Extensions.Options;
using QuestPDF.Fluent;

namespace Core.Services;

public class PaymentService(
    IPaymentRepository paymentRepository,
    IAccountEntryRepository accountEntryRepository,
    ICreditNoteRepository creditNoteRepository,
    IInvoiceRepository invoiceRepository,
    IBillingProfileService billingProfileService,
    IBillingEntityService billingEntityService,
    IDocumentNumberRepository documentNumberRepository,
    IDisplayPreferenceService displayPreferenceService,
    IPaymentProvider paymentProvider,
    IUnitOfWork unitOfWork,
    IModeContext modeContext,
    TimeProvider timeProvider,
    IOptions<AppConfig> appConfig) : IPaymentService
{
    public async Task<Payment> CollectAsync(CollectPaymentRequest collectPaymentRequest, CancellationToken stoppingToken)
    {
        using var live = modeContext.Pin(Mode.Live);

        var invoice = await invoiceRepository.GetAsync(collectPaymentRequest.InvoiceId, stoppingToken);

        if (invoice.InvoiceStatus is not InvoiceStatus.Issued || invoice.InvoiceNumber is null)
        {
            throw new InvoiceNotIssuedException(invoice.InvoiceId);
        }

        var settlement = await GetSettlementAsync(invoice.InvoiceId, stoppingToken);

        if (settlement.IsSettled)
        {
            throw new InvoiceSettledException(invoice.InvoiceId, invoice.InvoiceNumber);
        }

        var customerReference = await billingProfileService.EnsureProviderCustomerAsync(invoice.OrganizationId, stoppingToken);

        var paymentMethodReference = collectPaymentRequest.PaymentMethodReference
                                     ?? await ResolveDefaultMethodAsync(customerReference, invoice.OrganizationId, stoppingToken);

        var amount = new PaymentAmount(settlement.OutstandingAmount);

        var providerChargeResult = await paymentProvider.ChargeAsync(
            new ProviderChargeRequest
            {
                CustomerReference = customerReference,
                PaymentMethodReference = paymentMethodReference,
                Amount = amount,

                Currency = invoice.Currency,
                InvoiceNumber = invoice.InvoiceNumber.Value,

                IdempotencyKey = await BuildIdempotencyKeyAsync(invoice, stoppingToken)
            },
            stoppingToken);

        var payment = PaymentFactory.FromCharge(invoice, amount, providerChargeResult, timeProvider);

        await paymentRepository.AddAsync(payment, stoppingToken);

        if (payment.PaymentStatus is PaymentStatus.Settled)
        {
            await SettleAsync(payment, invoice, stoppingToken);
        }

        await unitOfWork.SaveChangesAsync(stoppingToken);

        return await paymentRepository.GetAsync(payment.PaymentId, stoppingToken);
    }

    public async Task<Payment> RecordAsync(RecordPaymentRequest recordPaymentRequest, CancellationToken stoppingToken)
    {
        using var live = modeContext.Pin(Mode.Live);

        var invoice = recordPaymentRequest.InvoiceId.HasValue
            ? await invoiceRepository.GetAsync(recordPaymentRequest.InvoiceId.Value, stoppingToken)
            : null;

        if (invoice is not null && invoice.Currency != recordPaymentRequest.Currency)
        {
            throw new CurrencyMismatchException(invoice.InvoiceId, invoice.Currency, recordPaymentRequest.Currency);
        }

        return await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var receiptNumber = await NextReceiptNumberAsync(recordPaymentRequest.OrganizationId, stoppingToken);
            var payment = PaymentFactory.FromRecord(recordPaymentRequest, receiptNumber, timeProvider);

            await paymentRepository.AddAsync(payment, stoppingToken);

            await ReceiptAsync(payment, invoice, stoppingToken);
            await unitOfWork.SaveChangesAsync(stoppingToken);

            return await paymentRepository.GetAsync(payment.PaymentId, stoppingToken);
        }, stoppingToken);
    }

    // Redelivery never reaches here: the event record is inserted under a unique reference first.
    public async Task<PaymentId?> HandleEventAsync(ProviderEvent providerEvent, CancellationToken stoppingToken)
    {
        using var live = modeContext.Pin(Mode.Live);

        if (providerEvent.PaymentReference is null || providerEvent.PaymentStatus is null)
        {
            return null;
        }

        var payment = await paymentRepository.FindByProviderReferenceAsync(
            providerEvent.PaymentReference.Value, stoppingToken);

        if (payment is null)
        {
            return null;
        }

        if (providerEvent.PaymentMethodReference is not null)
        {
            await paymentRepository.RecordMethodAsync(
                payment.PaymentId,
                providerEvent.PaymentMethodKind ?? payment.PaymentMethodKind,
                providerEvent.PaymentMethodReference,
                providerEvent.PaymentMethodLabel,
                stoppingToken);
        }

        await ApplyStatusAsync(payment, providerEvent, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);

        return payment.PaymentId;
    }

    // Each transition guards on the current state because provider events arrive out of order.
    private async Task ApplyStatusAsync(Payment payment, ProviderEvent providerEvent, CancellationToken stoppingToken)
    {
        switch (providerEvent.PaymentStatus)
        {
            case PaymentStatus.Settled when payment.PaymentStatus is PaymentStatus.Pending:
                var invoice = payment.RaisedForInvoiceId.HasValue
                    ? await invoiceRepository.GetAsync(payment.RaisedForInvoiceId.Value, stoppingToken)
                    : null;

                await SettleAsync(payment, invoice, stoppingToken);
                break;

            case PaymentStatus.Failed when payment.PaymentStatus is PaymentStatus.Pending:
                await paymentRepository.FailAsync(
                    payment.PaymentId,
                    providerEvent.FailureCode,
                    providerEvent.FailureReason ?? new FailureReason("The payment was refused."),
                    stoppingToken);
                break;

            case PaymentStatus.Returned when payment.PaymentStatus is PaymentStatus.Settled:
                await paymentRepository.ReturnAsync(
                    payment.PaymentId,
                    new ReturnedAt(timeProvider.GetUtcNow()),
                    providerEvent.FailureCode,
                    providerEvent.FailureReason ?? new FailureReason("The payment was returned."),
                    stoppingToken);

                await accountEntryRepository.AddAsync(
                    AccountEntryFactory.Return(
                        payment,
                        new EntryDescription($"Payment {payment.ReceiptNumber?.ToString() ?? payment.PaymentId.ToString()} returned"),
                        timeProvider),
                    stoppingToken);

                if (payment.RaisedForInvoiceId.HasValue)
                {
                    await invoiceRepository.UnprocessAsync(payment.RaisedForInvoiceId.Value, stoppingToken);
                }

                break;
        }
    }

    private async Task SettleAsync(Payment payment, Invoice? invoice, CancellationToken stoppingToken)
    {
        var receiptNumber = await NextReceiptNumberAsync(payment.OrganizationId, stoppingToken);

        await paymentRepository.SettleAsync(
            payment.PaymentId, new SettledAt(timeProvider.GetUtcNow()), receiptNumber, stoppingToken);

        await ReceiptAsync(payment with { ReceiptNumber = receiptNumber }, invoice, stoppingToken);
    }

    private async Task ReceiptAsync(Payment payment, Invoice? invoice, CancellationToken stoppingToken)
    {
        var accountEntries = new List<AccountEntry>
        {
            AccountEntryFactory.Receipt(payment, new EntryDescription(DescribeReceipt(payment)), timeProvider)
        };

        var allocated = 0m;

        if (invoice is not null && invoice.Currency == payment.Currency)
        {
            var settlement = await GetSettlementAsync(invoice.InvoiceId, stoppingToken);

            var allocatable = PaymentAllocator.Allocatable(
                new BalanceAmount(payment.Amount), settlement.OutstandingAmount);

            if (allocatable > 0)
            {
                allocated = allocatable;

                accountEntries.Add(AccountEntryFactory.Allocation(
                    payment.OrganizationId,
                    payment.Currency,
                    allocatable,
                    invoice.InvoiceId,
                    payment.PaymentId,
                    new EntryDescription($"Allocated to invoice {invoice.InvoiceNumber?.ToString() ?? invoice.InvoiceId.ToString()}"),
                    allocatedByUserId: null,
                    timeProvider));
            }
        }

        await accountEntryRepository.AddManyAsync(accountEntries, stoppingToken);

        if (invoice is not null)
        {
            await SynchroniseSettlementAsync(invoice.InvoiceId, allocated, stoppingToken);
        }
    }

    private async Task SynchroniseSettlementAsync(InvoiceId invoiceId, decimal appliedNow, CancellationToken stoppingToken)
    {
        var settlement = await GetSettlementAsync(invoiceId, stoppingToken);

        // The ledger rows just written are uncommitted, so the figure read back excludes them.
        var outstanding = settlement.OutstandingAmount - appliedNow;

        if (outstanding <= 0)
        {
            await invoiceRepository.ProcessAsync(invoiceId, stoppingToken);
        }
    }

    public async Task<ExportedReceipt> ExportReceiptAsync(PaymentId paymentId, CancellationToken stoppingToken)
    {
        using var live = modeContext.Pin(Mode.Live);

        var payment = await paymentRepository.GetAsync(paymentId, stoppingToken);

        if (payment.PaymentStatus is not PaymentStatus.Settled || payment.ReceiptNumber is null)
        {
            throw new PaymentNotSettledException(paymentId, payment.PaymentStatus);
        }

        var invoice = payment.RaisedForInvoiceId.HasValue
            ? await invoiceRepository.GetAsync(payment.RaisedForInvoiceId.Value, stoppingToken)
            : null;

        var billingProfile = await billingProfileService.GetAsync(payment.OrganizationId, stoppingToken);
        var billingEntity = await billingEntityService.GetAsync(billingProfile.BillingEntityId, stoppingToken);

        var translator = new Translator(await displayPreferenceService.ForStatedAsync(
            invoice?.Locale ?? billingProfile.Locale, payment.OrganizationId, stoppingToken));

        var receiptDocument = new ReceiptDocument(payment, invoice, billingEntity, billingProfile, translator);

        return new ExportedReceipt
        {
            Payment = payment,
            Invoice = invoice,
            Content = receiptDocument.GeneratePdf()
        };
    }

    public async Task<InvoiceSettlement> GetSettlementAsync(InvoiceId invoiceId, CancellationToken stoppingToken)
    {
        var invoice = await invoiceRepository.GetAsync(invoiceId, stoppingToken);
        var allocated = await accountEntryRepository.GetAllocatedAsync(invoiceId, stoppingToken);
        var credited = await creditNoteRepository.GetCreditedAmountAsync(invoiceId, stoppingToken);

        return Compose(invoice.InvoiceId, invoice.Amount, allocated, credited);
    }

    public async Task<List<InvoiceSettlement>> GetSettlementsAsync(List<InvoiceId> invoiceIds, CancellationToken stoppingToken)
    {
        if (invoiceIds.Count == 0)
        {
            return [];
        }

        var invoices = await invoiceRepository.GetManyAsync(invoiceIds, stoppingToken);
        var allocated = await accountEntryRepository.GetAllocatedManyAsync(invoiceIds, stoppingToken);
        var credited = await creditNoteRepository.GetCreditedAmountManyAsync(invoiceIds, stoppingToken);

        return invoices.ConvertAll(x => Compose(
            x.InvoiceId,
            x.Amount,
            allocated.GetValueOrDefault(x.InvoiceId, new InvoiceAmount(0m)),
            credited.GetValueOrDefault(x.InvoiceId, new InvoiceAmount(0m))));
    }

    private static InvoiceSettlement Compose(
        InvoiceId invoiceId, InvoiceAmount amount, InvoiceAmount allocated, InvoiceAmount credited)
    {
        var outstanding = Math.Max(amount - allocated - credited, 0);

        return new InvoiceSettlement
        {
            InvoiceId = invoiceId,
            Amount = amount,
            AllocatedAmount = allocated,
            CreditedAmount = credited,
            OutstandingAmount = new OutstandingAmount(outstanding),

            IsSettled = new IsSettled(outstanding == 0)
        };
    }

    private async Task<ReceiptNumber> NextReceiptNumberAsync(OrganizationId organizationId, CancellationToken stoppingToken)
    {
        var billingProfile = await billingProfileService.GetAsync(organizationId, stoppingToken);
        var issuedYear = new Year(timeProvider.GetUtcNow().Year);

        var position = await documentNumberRepository.NextReceiptPositionAsync(
            billingProfile.BillingEntityId, issuedYear, stoppingToken);

        return DocumentNumberFormatter.Receipt(
            new NumberPrefix(appConfig.Value.PaymentConfig.ReceiptNumberPrefix), issuedYear, position);
    }

    private async Task<PaymentMethodReference?> ResolveDefaultMethodAsync(
        ProviderCustomerReference providerCustomerReference, OrganizationId organizationId, CancellationToken stoppingToken)
    {
        var paymentMethods = await paymentProvider.ListPaymentMethodsAsync(providerCustomerReference, stoppingToken);

        if (paymentMethods.Count == 0)
        {
            throw new NoPaymentMethodException(organizationId);
        }

        var paymentMethod = paymentMethods.Find(x => x.IsDefault) ?? paymentMethods[0];

        return paymentMethod.PaymentMethodReference;
    }

    // Stable across retries of one attempt, different across attempts.
    private async Task<ProviderIdempotencyKey> BuildIdempotencyKeyAsync(Invoice invoice, CancellationToken stoppingToken)
    {
        var attempts = await paymentRepository.GetAttemptCountAsync(invoice.InvoiceId, stoppingToken);

        return new ProviderIdempotencyKey($"{invoice.InvoiceId}:{(int)attempts}");
    }

    private static string DescribeReceipt(Payment payment)
    {
        return payment.PaymentMethodLabel is not null
            ? $"Payment received, {payment.PaymentMethodLabel}"
            : $"Payment received, {payment.PaymentMethodKind}";
    }

    public Task<Payment> GetAsync(PaymentId paymentId, CancellationToken stoppingToken)
    {
        return paymentRepository.GetAsync(paymentId, stoppingToken);
    }

    public Task<Exists> ExistsAsync(OrganizationId organizationId, PaymentId paymentId, IncludeDeleted includeDeleted, CancellationToken stoppingToken)
    {
        return paymentRepository.ExistsAsync(organizationId, paymentId, includeDeleted, stoppingToken);
    }

    public Task<PagedResponse<Payment>> ListAsync(ListPaymentRequest listPaymentRequest, CancellationToken stoppingToken)
    {
        return paymentRepository.ListAsync(listPaymentRequest, stoppingToken);
    }

    public Task<TotalCount> GetCountAsync(ListPaymentRequest listPaymentRequest, CancellationToken stoppingToken)
    {
        return paymentRepository.GetCountAsync(listPaymentRequest, stoppingToken);
    }
}
