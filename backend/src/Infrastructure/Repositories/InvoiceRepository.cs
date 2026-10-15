using Core.Enums;
using Core.Helpers;
using Core.Interfaces.Repositories;
using Core.Models.Common;
using Core.Models.Invoices;
using Core.Models.Revenue;
using Core.ValueObjects;
using Infrastructure.Entities;
using Infrastructure.Expressions;
using Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class InvoiceRepository(
    TransactionMonitoringDbContext context,
    TimeProvider timeProvider) : PagedRepository<ListInvoiceRequest, InvoiceEntity, Invoice>, IInvoiceRepository
{
    public async Task AddAsync(Invoice invoice, CancellationToken stoppingToken)
    {
        var invoiceEntity = invoice.ToEntity();
        await context.Invoices.AddAsync(invoiceEntity, stoppingToken);

        foreach (var line in invoice.Lines)
        {
            await context.InvoiceLines.AddAsync(line.ToEntity(), stoppingToken);
        }
    }

    public async Task<CommitmentAmount> GetCommitmentDrawnAsync(
        OrganizationId organizationId,
        EffectiveFrom effectiveFrom,
        EffectiveTo effectiveTo,
        CancellationToken stoppingToken)
    {
        var (firstKey, lastKey) = BillingPeriod.KeysWithin(effectiveFrom, effectiveTo);

        var drawn = await context.InvoiceLines
            .Where(x => x.InvoiceLineTypeId == (int)InvoiceLineType.CommitmentDrawdown &&
                        x.Invoice.OrganizationId == organizationId &&
                        !x.Invoice.IsDeleted &&
                        x.Invoice.Year * 12 + x.Invoice.Month >= firstKey &&
                        x.Invoice.Year * 12 + x.Invoice.Month <= lastKey)
            .SumAsync(x => x.Amount, stoppingToken);

        return new CommitmentAmount(Math.Abs(drawn));
    }

    public async Task IssueAsync(InvoiceId invoiceId, InvoiceNumber invoiceNumber, IssuedAt issuedAt, DueAt dueAt, CancellationToken stoppingToken)
    {
        var invoiceEntity = await context.Invoices
            .FirstAsync(x => x.InvoiceId == invoiceId && !x.IsDeleted, stoppingToken);

        invoiceEntity.InvoiceNumber = invoiceNumber;
        invoiceEntity.InvoiceStatusId = (int)InvoiceStatus.Issued;
        invoiceEntity.IssuedAt = issuedAt;
        invoiceEntity.DueAt = dueAt;
        invoiceEntity.UpdatedAt = timeProvider.GetUtcNow();

        context.Invoices.Update(invoiceEntity);
    }

    public async Task ProcessAsync(InvoiceId invoiceId, CancellationToken stoppingToken)
    {
        var invoiceEntity = await context.Invoices
            .FirstAsync(x => x.InvoiceId == invoiceId && !x.IsDeleted, stoppingToken);

        invoiceEntity.IsProcessed = true;
        invoiceEntity.ProcessedAt = timeProvider.GetUtcNow();
        invoiceEntity.UpdatedAt = timeProvider.GetUtcNow();

        context.Invoices.Update(invoiceEntity);
    }

    public async Task UnprocessAsync(InvoiceId invoiceId, CancellationToken stoppingToken)
    {
        var invoiceEntity = await context.Invoices
            .FirstAsync(x => x.InvoiceId == invoiceId && !x.IsDeleted, stoppingToken);

        invoiceEntity.IsProcessed = false;
        invoiceEntity.ProcessedAt = null;
        invoiceEntity.UpdatedAt = timeProvider.GetUtcNow();

        context.Invoices.Update(invoiceEntity);
    }

    public async Task<List<Invoice>> GetManyAsync(List<InvoiceId> invoiceIds, CancellationToken stoppingToken)
    {
        var ids = invoiceIds.ConvertAll(x => (Guid)x);

        var invoiceEntities = await context.Invoices
            .Where(x => ids.Contains(x.InvoiceId) && !x.IsDeleted)
            .ToListAsync(stoppingToken);

        return invoiceEntities.ConvertAll(x => x.ToModel());
    }

    public async Task<Exists> ExistsAsync(OrganizationId organizationId, InvoiceId invoiceId, IncludeDeleted includeDeleted, CancellationToken stoppingToken)
    {
        var exists = await context.Invoices
            .AnyAsync(x => x.InvoiceId == invoiceId &&
                           x.OrganizationId == organizationId &&
                           (includeDeleted || x.IsDeleted == false), stoppingToken);

        return new Exists(exists);
    }

    public async Task<Exists> ExistsByDateAsync(OrganizationId organizationId, Month month, Year year, IncludeDeleted includeDeleted, CancellationToken stoppingToken)
    {
        var exists = await context.Invoices
            .AnyAsync(x => x.OrganizationId == organizationId &&
                           x.Month == month &&
                           x.Year == year &&
                           (includeDeleted || x.IsDeleted == false), stoppingToken);

        return new Exists(exists);
    }

    public async Task<Invoice> GetAsync(InvoiceId invoiceId, CancellationToken stoppingToken)
    {
        var invoiceEntity = await context.Invoices
            .FirstAsync(x => x.InvoiceId == invoiceId && !x.IsDeleted, stoppingToken);

        return invoiceEntity.ToModel();
    }

    public async Task<Invoice?> GetIssuedByDateAsync(OrganizationId organizationId, Month month, Year year, CancellationToken stoppingToken)
    {
        var invoiceEntity = await context.Invoices
            .Where(x => x.OrganizationId == organizationId &&
                        x.Month == month &&
                        x.Year == year &&
                        x.InvoiceStatusId == (int)InvoiceStatus.Issued &&
                        !x.IsDeleted)
            .OrderByDescending(x => x.IssuedAt)
            .FirstOrDefaultAsync(stoppingToken);

        return invoiceEntity?.ToModel();
    }

    public async Task<List<Invoice>> GetOverdueAsync(CancellationToken stoppingToken)
    {
        var now = timeProvider.GetUtcNow();

        var invoiceEntities = await context.Invoices
            .Where(x => !x.IsDeleted &&
                        !x.IsProcessed &&
                        x.InvoiceStatusId == (int)InvoiceStatus.Issued &&
                        x.DueAt != null &&
                        x.DueAt < now)
            .ToListAsync(stoppingToken);

        return invoiceEntities.Select(x => x.ToModel()).ToList();
    }

    public async Task<List<BilledMonth>> GetBilledMonthsAsync(int firstMonthKey, int lastMonthKey, CancellationToken stoppingToken)
    {
        var billed = await context.Invoices
            .Where(x => !x.IsDeleted &&
                        x.InvoiceStatusId == (int)InvoiceStatus.Issued &&
                        x.Year * 12 + x.Month >= firstMonthKey &&
                        x.Year * 12 + x.Month <= lastMonthKey)
            .GroupBy(x => new
            {
                x.OrganizationId,
                x.Organization.FriendlyName,
                x.Year,
                x.Month,
                x.CurrencyId
            })
            .Select(group => new
            {
                group.Key.OrganizationId,
                group.Key.FriendlyName,
                group.Key.Year,
                group.Key.Month,
                group.Key.CurrencyId,
                NetAmount = group.Sum(x => x.NetAmount)
            })
            .ToListAsync(stoppingToken);

        var credited = await context.CreditNotes
            .Where(x => !x.Invoice.IsDeleted &&
                        x.Invoice.Year * 12 + x.Invoice.Month >= firstMonthKey &&
                        x.Invoice.Year * 12 + x.Invoice.Month <= lastMonthKey)
            .GroupBy(x => new
            {
                x.OrganizationId,
                x.Invoice.Year,
                x.Invoice.Month,
                x.CurrencyId
            })
            .Select(group => new
            {
                group.Key.OrganizationId,
                group.Key.Year,
                group.Key.Month,
                group.Key.CurrencyId,
                NetAmount = group.Sum(x => x.NetAmount)
            })
            .ToDictionaryAsync(
                x => (x.OrganizationId, x.Year, x.Month, x.CurrencyId),
                x => x.NetAmount,
                stoppingToken);

        return billed.ConvertAll(x => new BilledMonth
        {
            OrganizationId = new OrganizationId(x.OrganizationId),
            OrganizationName = new FriendlyName(x.FriendlyName),
            Month = new Month(x.Month),
            Year = new Year(x.Year),
            Currency = (Currency)x.CurrencyId,
            NetAmount = new NetAmount(x.NetAmount),
            CreditedNetAmount = new NetAmount(
                credited.GetValueOrDefault((x.OrganizationId, x.Year, x.Month, x.CurrencyId)))
        });
    }

    public async Task<List<Invoice>> ListIssuedByPeriodAsync(int firstMonthKey, int lastMonthKey, CancellationToken stoppingToken)
    {
        var invoiceEntities = await context.Invoices
            .Include(x => x.Organization)
            .Where(x => !x.IsDeleted &&
                        x.InvoiceStatusId == (int)InvoiceStatus.Issued &&
                        x.Year * 12 + x.Month >= firstMonthKey &&
                        x.Year * 12 + x.Month <= lastMonthKey)
            .OrderBy(x => x.Year)
            .ThenBy(x => x.Month)
            .ThenBy(x => x.InvoiceNumber)
            .ToListAsync(stoppingToken);

        return invoiceEntities.ConvertAll(x => x.ToModel());
    }

    public async Task DeleteAsync(InvoiceId invoiceId, CancellationToken stoppingToken)
    {
        var invoiceEntity = await context.Invoices
            .FirstAsync(x => x.InvoiceId == invoiceId && !x.IsDeleted, stoppingToken);

        invoiceEntity.IsDeleted = true;
        invoiceEntity.DeletedAt = timeProvider.GetUtcNow();

        context.Invoices.Update(invoiceEntity);
    }

    public async Task RecoverAsync(InvoiceId invoiceId, CancellationToken stoppingToken)
    {
        var invoiceEntity = await context.Invoices
            .FirstAsync(x => x.InvoiceId == invoiceId, stoppingToken);

        invoiceEntity.IsDeleted = false;
        invoiceEntity.DeletedAt = null;

        context.Invoices.Update(invoiceEntity);
    }

    protected override Task<List<Invoice>> MapPageAsync(List<InvoiceEntity> items, ListInvoiceRequest request, CancellationToken stoppingToken)
    {
        return Task.FromResult(items.ConvertAll(x => x.ToModel()));
    }

    protected override IOrderByExpression<InvoiceEntity> OrderByExpression()
    {
        return new OrderByExpression<InvoiceEntity, DateTimeOffset>(x => x.UpdatedAt);
    }

    protected override IQueryable<InvoiceEntity> FilterByPagedRequest(ListInvoiceRequest listInvoiceRequest)
    {
        var queryable = context.Invoices
            .Where(x => x.OrganizationId == listInvoiceRequest.OrganizationId && !x.IsDeleted);

        return queryable;
    }

    protected override IQueryable<InvoiceEntity> FilterByDateRange(IQueryable<InvoiceEntity> queryable, DatedRequest datedRequest)
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
}