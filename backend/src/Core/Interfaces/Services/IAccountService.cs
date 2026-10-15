using Core.Models.Accounts;
using Core.Models.Common;
using Core.Models.Payments;
using Core.ValueObjects;

namespace Core.Interfaces.Services;

public interface IAccountService : IPagedService<ListAccountEntryRequest, AccountEntry>
{
    Task<List<AccountBalance>> GetBalancesAsync(OrganizationId organizationId, CancellationToken stoppingToken);

    Task<AccountEntry> AllocateAsync(AllocatePaymentRequest allocatePaymentRequest, CancellationToken stoppingToken);

    Task<AccountEntry> RefundAsync(RefundBalanceRequest refundBalanceRequest, CancellationToken stoppingToken);

    Task<PagedResponse<AccountBalance>> ListUnappliedAsync(
        ListUnappliedReceiptRequest listUnappliedReceiptRequest, CancellationToken stoppingToken);

    Task<TotalCount> GetUnappliedCountAsync(
        ListUnappliedReceiptRequest listUnappliedReceiptRequest, CancellationToken stoppingToken);
}
