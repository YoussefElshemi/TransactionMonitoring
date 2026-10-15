using Core.Interfaces.Repositories;
using Core.Models.Common;
using Core.Models.Dunning;
using Core.ValueObjects;
using Infrastructure.Entities;
using Infrastructure.Expressions;
using Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class DunningRepository(
    TransactionMonitoringDbContext context,
    TimeProvider timeProvider) : PagedRepository<ListDunningStateRequest, DunningStateEntity, DunningState>, IDunningRepository
{
    public async Task AddAsync(DunningState dunningState, CancellationToken stoppingToken)
    {
        await context.DunningStates.AddAsync(dunningState.ToEntity(), stoppingToken);
    }

    public async Task<DunningState> GetAsync(DunningStateId dunningStateId, CancellationToken stoppingToken)
    {
        var dunningStateEntity = await context.DunningStates
            .Include(x => x.Invoice)
            .FirstAsync(x => x.DunningStateId == dunningStateId, stoppingToken);

        return dunningStateEntity.ToModel();
    }

    public async Task<DunningState?> FindOpenAsync(InvoiceId invoiceId, CancellationToken stoppingToken)
    {
        var dunningStateEntity = await context.DunningStates
            .FirstOrDefaultAsync(x => x.InvoiceId == invoiceId && x.ResolvedAt == null, stoppingToken);

        return dunningStateEntity?.ToModel();
    }

    public async Task<List<DunningState>> ListDueAsync(NextAttemptAt asOf, CancellationToken stoppingToken)
    {
        var dunningStateEntities = await context.DunningStates
            .Where(x => x.ResolvedAt == null && x.NextAttemptAt <= asOf)
            .OrderBy(x => x.NextAttemptAt)
            .ToListAsync(stoppingToken);

        return dunningStateEntities.ConvertAll(x => x.ToModel());
    }

    public async Task AdvanceAsync(DunningState dunningState, CancellationToken stoppingToken)
    {
        var dunningStateEntity = await context.DunningStates
            .FirstAsync(x => x.DunningStateId == dunningState.DunningStateId, stoppingToken);

        dunningStateEntity.DunningStageId = (int)dunningState.Stage;
        dunningStateEntity.NextAttemptAt = dunningState.NextAttemptAt;
        dunningStateEntity.LastNoticedAt = dunningState.LastNoticedAt.HasValue ? dunningState.LastNoticedAt.Value : null;
        dunningStateEntity.CollectionAttempts = dunningState.CollectionAttempts;
        dunningStateEntity.LastAttemptedAt = dunningState.LastAttemptedAt.HasValue ? dunningState.LastAttemptedAt.Value : null;
        dunningStateEntity.RestrictionProposedAt = dunningState.RestrictionProposedAt.HasValue
            ? dunningState.RestrictionProposedAt.Value
            : null;
        dunningStateEntity.UpdatedAt = timeProvider.GetUtcNow();

        context.DunningStates.Update(dunningStateEntity);
    }

    public async Task ResolveAsync(DunningStateId dunningStateId, ResolvedAt resolvedAt, CancellationToken stoppingToken)
    {
        var dunningStateEntity = await context.DunningStates
            .FirstAsync(x => x.DunningStateId == dunningStateId, stoppingToken);

        dunningStateEntity.ResolvedAt = resolvedAt;
        dunningStateEntity.UpdatedAt = timeProvider.GetUtcNow();

        context.DunningStates.Update(dunningStateEntity);
    }

    public async Task<List<DunningState>> ListOpenedBetweenAsync(PeriodStart periodStart, PeriodEnd periodEnd, CancellationToken stoppingToken)
    {
        var from = (DateTimeOffset)periodStart;
        var to = (DateTimeOffset)periodEnd;

        var dunningStateEntities = await context.DunningStates
            .Include(x => x.Invoice)
            .ThenInclude(x => x.Organization)
            .Where(x => x.CreatedAt >= from && x.CreatedAt <= to)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(stoppingToken);

        return dunningStateEntities.ConvertAll(x => x.ToModel());
    }

    public async Task<List<DunningState>> ListOpenAsync(CancellationToken stoppingToken)
    {
        var dunningStateEntities = await context.DunningStates
            .Where(x => x.ResolvedAt == null)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(stoppingToken);

        return dunningStateEntities.ConvertAll(x => x.ToModel());
    }

    protected override Task<List<DunningState>> MapPageAsync(
        List<DunningStateEntity> items, ListDunningStateRequest request, CancellationToken stoppingToken)
    {
        return Task.FromResult(items.ConvertAll(x => x.ToModel()));
    }

    protected override IOrderByExpression<DunningStateEntity> OrderByExpression()
    {
        return new OrderByExpression<DunningStateEntity, DateTimeOffset>(x => x.CreatedAt, ascendingByDefault: true);
    }

    protected override IQueryable<DunningStateEntity> FilterByPagedRequest(ListDunningStateRequest listDunningStateRequest)
    {
        var queryable = context.DunningStates.Include(x => x.Invoice).AsQueryable();

        if (listDunningStateRequest.OrganizationId.HasValue)
        {
            queryable = queryable.Where(x => x.OrganizationId == listDunningStateRequest.OrganizationId.Value);
        }

        if (!listDunningStateRequest.IncludeResolved)
        {
            queryable = queryable.Where(x => x.ResolvedAt == null);
        }

        return queryable;
    }

    protected override IQueryable<DunningStateEntity> FilterByDateRange(IQueryable<DunningStateEntity> queryable, DatedRequest datedRequest)
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
