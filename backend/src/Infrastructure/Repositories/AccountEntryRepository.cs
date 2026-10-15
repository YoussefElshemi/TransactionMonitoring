using Core.Enums;
using Core.Interfaces.Repositories;
using Core.Models.Accounts;
using Core.Models.Common;
using Core.Models.Payments;
using Core.ValueObjects;
using Infrastructure.Entities;
using Infrastructure.Expressions;
using Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class AccountEntryRepository(TransactionMonitoringDbContext context)
    : PagedRepository<ListAccountEntryRequest, AccountEntryEntity, AccountEntry>, IAccountEntryRepository
{
    public async Task AddAsync(AccountEntry accountEntry, CancellationToken stoppingToken)
    {
        await context.AccountEntries.AddAsync(accountEntry.ToEntity(), stoppingToken);
    }

    public async Task AddManyAsync(List<AccountEntry> accountEntries, CancellationToken stoppingToken)
    {
        await context.AccountEntries.AddRangeAsync(accountEntries.ConvertAll(x => x.ToEntity()), stoppingToken);
    }

    public async Task<BalanceAmount> GetBalanceAsync(OrganizationId organizationId, Currency currency, CancellationToken stoppingToken)
    {
        var balance = await context.AccountEntries
            .Where(x => x.OrganizationId == organizationId && x.CurrencyId == (int)currency)
            .SumAsync(x => x.Amount, stoppingToken);

        return new BalanceAmount(balance);
    }

    public async Task<List<AccountBalance>> GetBalancesAsync(OrganizationId organizationId, CancellationToken stoppingToken)
    {
        var balances = await context.AccountEntries
            .Where(x => x.OrganizationId == organizationId)
            .GroupBy(x => x.CurrencyId)
            .Select(x => new
            {
                CurrencyId = x.Key,
                Amount = x.Sum(y => y.Amount),
                EntryCount = x.Count()
            })
            .ToListAsync(stoppingToken);

        return balances.ConvertAll(x => new AccountBalance
        {
            OrganizationId = organizationId,
            Currency = (Currency)x.CurrencyId,
            Amount = new BalanceAmount(x.Amount),
            EntryCount = new Count(x.EntryCount)
        });
    }

    public async Task<InvoiceAmount> GetAllocatedAsync(InvoiceId invoiceId, CancellationToken stoppingToken)
    {
        var allocated = await context.AccountEntries
            .Where(x => x.InvoiceId == (Guid)invoiceId && x.AccountEntryReasonId == (int)AccountEntryReason.Allocation)
            .SumAsync(x => x.Amount, stoppingToken);

        return new InvoiceAmount(-allocated);
    }

    public async Task<Dictionary<Guid, InvoiceAmount>> GetAllocatedManyAsync(List<InvoiceId> invoiceIds, CancellationToken stoppingToken)
    {
        var ids = invoiceIds.ConvertAll(x => (Guid)x);

        var allocated = await context.AccountEntries
            .Where(x => x.InvoiceId != null &&
                        ids.Contains(x.InvoiceId.Value) &&
                        x.AccountEntryReasonId == (int)AccountEntryReason.Allocation)
            .GroupBy(x => x.InvoiceId!.Value)
            .Select(x => new { InvoiceId = x.Key, Amount = x.Sum(y => y.Amount) })
            .ToListAsync(stoppingToken);

        return allocated.ToDictionary(x => x.InvoiceId, x => new InvoiceAmount(-x.Amount));
    }

    public async Task<List<AccountBalance>> ListUnappliedAsync(
        ListUnappliedReceiptRequest listUnappliedReceiptRequest, CancellationToken stoppingToken)
    {
        var balances = await UnappliedQueryable(listUnappliedReceiptRequest)
            .OrderByDescending(x => x.Amount)
            .Skip((listUnappliedReceiptRequest.PageNumber - 1) * listUnappliedReceiptRequest.PageSize)
            .Take(listUnappliedReceiptRequest.PageSize)
            .ToListAsync(stoppingToken);

        return balances.ConvertAll(x => new AccountBalance
        {
            OrganizationId = new OrganizationId(x.OrganizationId),
            Currency = (Currency)x.CurrencyId,
            Amount = new BalanceAmount(x.Amount),
            EntryCount = new Count(x.EntryCount)
        });
    }

    public async Task<TotalCount> GetUnappliedCountAsync(
        ListUnappliedReceiptRequest listUnappliedReceiptRequest, CancellationToken stoppingToken)
    {
        var count = await UnappliedQueryable(listUnappliedReceiptRequest).CountAsync(stoppingToken);

        return new TotalCount(count);
    }

    private IQueryable<UnappliedBalance> UnappliedQueryable(ListUnappliedReceiptRequest listUnappliedReceiptRequest)
    {
        var queryable = context.AccountEntries.AsQueryable();

        if (listUnappliedReceiptRequest.FromDate != DateOnly.MinValue)
        {
            queryable = queryable.Where(x => DateOnly.FromDateTime(x.CreatedAt.UtcDateTime) >= listUnappliedReceiptRequest.FromDate);
        }

        if (listUnappliedReceiptRequest.ToDate != DateOnly.MaxValue)
        {
            queryable = queryable.Where(x => DateOnly.FromDateTime(x.CreatedAt.UtcDateTime) <= listUnappliedReceiptRequest.ToDate);
        }

        return queryable
            .GroupBy(x => new { x.OrganizationId, x.CurrencyId })
            .Select(x => new UnappliedBalance
            {
                OrganizationId = x.Key.OrganizationId,
                CurrencyId = x.Key.CurrencyId,
                Amount = x.Sum(y => y.Amount),
                EntryCount = x.Count()
            })
            .Where(x => x.Amount > 0);
    }

    protected override Task<List<AccountEntry>> MapPageAsync(
        List<AccountEntryEntity> items, ListAccountEntryRequest request, CancellationToken stoppingToken)
    {
        return Task.FromResult(items.ConvertAll(x => x.ToModel()));
    }

    protected override IOrderByExpression<AccountEntryEntity> OrderByExpression()
    {
        return new OrderByExpression<AccountEntryEntity, DateTimeOffset>(x => x.CreatedAt);
    }

    protected override IQueryable<AccountEntryEntity> FilterByPagedRequest(ListAccountEntryRequest listAccountEntryRequest)
    {
        return context.AccountEntries.Where(x => x.OrganizationId == listAccountEntryRequest.OrganizationId);
    }

    protected override IQueryable<AccountEntryEntity> FilterByDateRange(IQueryable<AccountEntryEntity> queryable, DatedRequest datedRequest)
    {
        if (datedRequest.FromDate != DateOnly.MinValue)
        {
            queryable = queryable
                .Where(x => DateOnly.FromDateTime(x.CreatedAt.UtcDateTime) >= datedRequest.FromDate);
        }

        if (datedRequest.ToDate != DateOnly.MaxValue)
        {
            queryable = queryable
                .Where(x => DateOnly.FromDateTime(x.CreatedAt.UtcDateTime) <= datedRequest.ToDate);
        }

        return queryable;
    }

    private sealed record UnappliedBalance
    {
        public required Guid OrganizationId { get; init; }
        public required int CurrencyId { get; init; }
        public required decimal Amount { get; init; }
        public required int EntryCount { get; init; }
    }
}
