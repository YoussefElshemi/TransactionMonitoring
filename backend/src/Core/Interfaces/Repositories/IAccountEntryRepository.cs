using Core.Enums;
using Core.Models.Accounts;
using Core.Models.Payments;
using Core.ValueObjects;

namespace Core.Interfaces.Repositories;

public interface IAccountEntryRepository : IPagedRepository<ListAccountEntryRequest, AccountEntry>
{
    Task AddAsync(AccountEntry accountEntry, CancellationToken stoppingToken);
    Task AddManyAsync(List<AccountEntry> accountEntries, CancellationToken stoppingToken);

    Task<BalanceAmount> GetBalanceAsync(OrganizationId organizationId, Currency currency, CancellationToken stoppingToken);
    Task<List<AccountBalance>> GetBalancesAsync(OrganizationId organizationId, CancellationToken stoppingToken);

    Task<InvoiceAmount> GetAllocatedAsync(InvoiceId invoiceId, CancellationToken stoppingToken);
    Task<Dictionary<Guid, InvoiceAmount>> GetAllocatedManyAsync(List<InvoiceId> invoiceIds, CancellationToken stoppingToken);

    Task<List<AccountBalance>> ListUnappliedAsync(ListUnappliedReceiptRequest listUnappliedReceiptRequest, CancellationToken stoppingToken);
    Task<TotalCount> GetUnappliedCountAsync(ListUnappliedReceiptRequest listUnappliedReceiptRequest, CancellationToken stoppingToken);
}
