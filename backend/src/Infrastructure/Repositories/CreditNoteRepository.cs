using Core.Interfaces.Repositories;
using Core.Models.Common;
using Core.Models.CreditNotes;
using Core.ValueObjects;
using Infrastructure.Entities;
using Infrastructure.Expressions;
using Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class CreditNoteRepository(
    TransactionMonitoringDbContext context) : PagedRepository<ListCreditNoteRequest, CreditNoteEntity, CreditNote>, ICreditNoteRepository
{
    public async Task AddAsync(CreditNote creditNote, CancellationToken stoppingToken)
    {
        await context.CreditNotes.AddAsync(creditNote.ToEntity(), stoppingToken);

        foreach (var line in creditNote.Lines)
        {
            await context.CreditNoteLines.AddAsync(line.ToEntity(), stoppingToken);
        }
    }

    public async Task<CreditNote> GetAsync(CreditNoteId creditNoteId, CancellationToken stoppingToken)
    {
        var creditNoteEntity = await context.CreditNotes
            .FirstAsync(x => x.CreditNoteId == creditNoteId, stoppingToken);

        return creditNoteEntity.ToModel();
    }

    public async Task<Exists> ExistsAsync(OrganizationId organizationId, CreditNoteId creditNoteId, CancellationToken stoppingToken)
    {
        var exists = await context.CreditNotes
            .AnyAsync(x => x.CreditNoteId == creditNoteId && x.OrganizationId == organizationId, stoppingToken);

        return new Exists(exists);
    }

    public async Task<NetAmount> GetCreditedNetAsync(InvoiceId invoiceId, CancellationToken stoppingToken)
    {
        var credited = await context.CreditNotes
            .Where(x => x.InvoiceId == invoiceId)
            .SumAsync(x => x.NetAmount, stoppingToken);

        return new NetAmount(credited);
    }

    public async Task<InvoiceAmount> GetCreditedAmountAsync(InvoiceId invoiceId, CancellationToken stoppingToken)
    {
        var credited = await context.CreditNotes
            .Where(x => x.InvoiceId == invoiceId)
            .SumAsync(x => x.Amount, stoppingToken);

        return new InvoiceAmount(credited);
    }

    public async Task<Dictionary<Guid, InvoiceAmount>> GetCreditedAmountManyAsync(List<InvoiceId> invoiceIds, CancellationToken stoppingToken)
    {
        var ids = invoiceIds.ConvertAll(x => (Guid)x);

        var credited = await context.CreditNotes
            .Where(x => ids.Contains(x.InvoiceId))
            .GroupBy(x => x.InvoiceId)
            .Select(x => new { InvoiceId = x.Key, Amount = x.Sum(y => y.Amount) })
            .ToListAsync(stoppingToken);

        return credited.ToDictionary(x => x.InvoiceId, x => new InvoiceAmount(x.Amount));
    }

    public async Task<List<CreditNote>> ListByInvoicePeriodAsync(int firstMonthKey, int lastMonthKey, CancellationToken stoppingToken)
    {
        var creditNoteEntities = await context.CreditNotes
            .Include(x => x.Invoice)
            .ThenInclude(x => x.Organization)
            .Where(x => !x.Invoice.IsDeleted &&
                        x.Invoice.Year * 12 + x.Invoice.Month >= firstMonthKey &&
                        x.Invoice.Year * 12 + x.Invoice.Month <= lastMonthKey)
            .OrderBy(x => x.Invoice.Year)
            .ThenBy(x => x.Invoice.Month)
            .ThenBy(x => x.CreditNoteNumber)
            .ToListAsync(stoppingToken);

        return creditNoteEntities.ConvertAll(x => x.ToModel());
    }

    protected override Task<List<CreditNote>> MapPageAsync(List<CreditNoteEntity> items, ListCreditNoteRequest request, CancellationToken stoppingToken)
    {
        return Task.FromResult(items.ConvertAll(x => x.ToModel()));
    }

    protected override IOrderByExpression<CreditNoteEntity> OrderByExpression()
    {
        return new OrderByExpression<CreditNoteEntity, DateTimeOffset>(x => x.IssuedAt);
    }

    protected override IQueryable<CreditNoteEntity> FilterByPagedRequest(ListCreditNoteRequest listCreditNoteRequest)
    {
        var queryable = context.CreditNotes
            .Where(x => x.OrganizationId == listCreditNoteRequest.OrganizationId);

        if (listCreditNoteRequest.InvoiceId.HasValue)
        {
            queryable = queryable.Where(x => x.InvoiceId == listCreditNoteRequest.InvoiceId.Value);
        }

        return queryable;
    }

    protected override IQueryable<CreditNoteEntity> FilterByDateRange(IQueryable<CreditNoteEntity> queryable, DatedRequest datedRequest)
    {
        if (datedRequest.FromDate != DateOnly.MinValue)
        {
            queryable = queryable.Where(x => DateOnly.FromDateTime(x.IssuedAt.UtcDateTime) >= datedRequest.FromDate);
        }

        if (datedRequest.ToDate != DateOnly.MaxValue)
        {
            queryable = queryable.Where(x => DateOnly.FromDateTime(x.IssuedAt.UtcDateTime) <= datedRequest.ToDate);
        }

        return queryable;
    }
}
