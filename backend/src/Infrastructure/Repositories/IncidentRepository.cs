using Core.Constants;
using Core.Enums;
using Core.Interfaces.Repositories;
using Core.Models.Common;
using Core.Models.Status;
using Core.ValueObjects;
using Infrastructure.Constants;
using Infrastructure.Entities;
using Infrastructure.Expressions;
using Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class IncidentRepository(
    TransactionMonitoringDbContext context,
    TimeProvider timeProvider) : PagedRepository<ListIncidentRequest, IncidentEntity, Incident>, IIncidentRepository
{
    public async Task AddAsync(Incident incident, CancellationToken stoppingToken)
    {
        var incidentEntity = incident.ToEntity();
        await context.Incidents.AddAsync(incidentEntity, stoppingToken);
    }

    public async Task<Incident> GetAsync(IncidentId incidentId, CancellationToken stoppingToken)
    {
        var incidentEntity = await Queryable()
            .FirstAsync(x => x.IncidentId == incidentId && !x.IsDeleted, stoppingToken);

        return incidentEntity.ToModel();
    }

    public async Task<Exists> ExistsAsync(IncidentId incidentId, CancellationToken stoppingToken)
    {
        var exists = await context.Incidents
            .AnyAsync(x => x.IncidentId == incidentId && !x.IsDeleted, stoppingToken);

        return new Exists(exists);
    }

    public async Task<List<Incident>> ListActiveAsync(CancellationToken stoppingToken)
    {
        var incidentEntities = await Queryable()
            .Where(x => !x.IsDeleted && x.StatusId != (int)IncidentStatus.Resolved)
            .OrderByDescending(x => x.StartedAt)
            .ToListAsync(stoppingToken);

        return incidentEntities.ConvertAll(x => x.ToModel());
    }

    public async Task<List<Incident>> ListActiveForComponentAsync(ServiceComponent component, CancellationToken stoppingToken)
    {
        var incidentEntities = await Queryable()
            .Where(x => !x.IsDeleted &&
                        x.StatusId != (int)IncidentStatus.Resolved &&
                        x.Components.Any(y => y.ComponentId == (int)component))
            .OrderByDescending(x => x.StartedAt)
            .ToListAsync(stoppingToken);

        return incidentEntities.ConvertAll(x => x.ToModel());
    }

    public async Task UpdateAsync(Incident incident, CancellationToken stoppingToken)
    {
        var incidentEntity = await context.Incidents
            .Include(x => x.Components)
            .FirstAsync(x => x.IncidentId == incident.IncidentId && !x.IsDeleted, stoppingToken);

        var now = timeProvider.GetUtcNow();

        incidentEntity.Title = incident.Title;
        incidentEntity.Summary = incident.Summary;
        incidentEntity.StatusId = (int)incident.Status;
        incidentEntity.ImpactId = (int)incident.Impact;
        incidentEntity.StartedAt = incident.StartedAt;
        incidentEntity.ResolvedAt = incident.ResolvedAt.HasValue ? incident.ResolvedAt.Value : null;
        incidentEntity.RootCause = incident.RootCause?.ToString();
        incidentEntity.Remediation = incident.Remediation?.ToString();
        incidentEntity.ReviewPublishedAt = incident.ReviewPublishedAt.HasValue ? incident.ReviewPublishedAt.Value : null;
        incidentEntity.UpdatedAt = now;

        context.IncidentComponents.RemoveRange(incidentEntity.Components);

        foreach (var component in incident.Components)
        {
            await context.IncidentComponents.AddAsync(new IncidentComponentEntity
            {
                IncidentId = incident.IncidentId,
                ComponentId = (int)component,
                CreatedAt = now,
                UpdatedAt = now
            }, stoppingToken);
        }

        context.Incidents.Update(incidentEntity);
    }

    public async Task AddUpdateAsync(IncidentUpdate incidentUpdate, CancellationToken stoppingToken)
    {
        var incidentUpdateEntity = incidentUpdate.ToEntity();
        await context.IncidentUpdates.AddAsync(incidentUpdateEntity, stoppingToken);
    }

    public async Task<IncidentNumber> GenerateIncidentNumberAsync(CancellationToken stoppingToken)
    {
        var result = await context.Database
            .SqlQueryRaw<int>($"SELECT nextval('\"{Schemas.Service}\".\"{Sequences.IncidentNumber}\"') as \"Value\"")
            .SingleAsync(stoppingToken);

        return new IncidentNumber(StatusConstants.IncidentNumberPrefix + result.ToString("D10"));
    }

    protected override Task<List<Incident>> MapPageAsync(List<IncidentEntity> items, ListIncidentRequest request, CancellationToken stoppingToken)
    {
        return Task.FromResult(items.ConvertAll(x => x.ToModel()));
    }

    protected override IOrderByExpression<IncidentEntity> OrderByExpression()
    {
        return new OrderByExpression<IncidentEntity, DateTimeOffset>(x => x.StartedAt);
    }

    protected override IQueryable<IncidentEntity> FilterByPagedRequest(ListIncidentRequest listIncidentRequest)
    {
        var queryable = Queryable().Where(x => !x.IsDeleted);

        if (!listIncidentRequest.IncludeResolved)
        {
            queryable = queryable.Where(x => x.StatusId != (int)IncidentStatus.Resolved);
        }

        if (listIncidentRequest.Status.HasValue)
        {
            queryable = queryable.Where(x => x.StatusId == (int)listIncidentRequest.Status.Value);
        }

        if (listIncidentRequest.Component.HasValue)
        {
            queryable = queryable.Where(x => x.Components.Any(y => y.ComponentId == (int)listIncidentRequest.Component.Value));
        }

        return queryable;
    }

    protected override IQueryable<IncidentEntity> FilterByDateRange(IQueryable<IncidentEntity> queryable, DatedRequest datedRequest)
    {
        if (datedRequest.FromDate != DateOnly.MinValue)
        {
            queryable = queryable
                .Where(x => DateOnly.FromDateTime(x.StartedAt.UtcDateTime) >= datedRequest.FromDate);
        }

        if (datedRequest.ToDate != DateOnly.MaxValue)
        {
            queryable = queryable
                .Where(x => DateOnly.FromDateTime(x.StartedAt.UtcDateTime) <= datedRequest.ToDate);
        }

        return queryable;
    }

    private IQueryable<IncidentEntity> Queryable()
    {
        return context.Incidents
            .Include(x => x.Components)
            .Include(x => x.Updates);
    }
}
