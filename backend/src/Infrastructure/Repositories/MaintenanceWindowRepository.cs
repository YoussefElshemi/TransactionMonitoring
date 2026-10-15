using Core.Enums;
using Core.Interfaces.Repositories;
using Core.Models.Common;
using Core.Models.Status;
using Core.ValueObjects;
using Infrastructure.Entities;
using Infrastructure.Expressions;
using Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class MaintenanceWindowRepository(
    TransactionMonitoringDbContext context,
    TimeProvider timeProvider) : PagedRepository<ListMaintenanceWindowRequest, MaintenanceWindowEntity, MaintenanceWindow>, IMaintenanceWindowRepository
{
    public async Task AddAsync(MaintenanceWindow maintenanceWindow, CancellationToken stoppingToken)
    {
        var maintenanceWindowEntity = maintenanceWindow.ToEntity();
        await context.MaintenanceWindows.AddAsync(maintenanceWindowEntity, stoppingToken);
    }

    public async Task<MaintenanceWindow> GetAsync(MaintenanceWindowId maintenanceWindowId, CancellationToken stoppingToken)
    {
        var maintenanceWindowEntity = await Queryable()
            .FirstAsync(x => x.MaintenanceWindowId == maintenanceWindowId && !x.IsDeleted, stoppingToken);

        return maintenanceWindowEntity.ToModel();
    }

    public async Task<Exists> ExistsAsync(MaintenanceWindowId maintenanceWindowId, CancellationToken stoppingToken)
    {
        var exists = await context.MaintenanceWindows
            .AnyAsync(x => x.MaintenanceWindowId == maintenanceWindowId && !x.IsDeleted, stoppingToken);

        return new Exists(exists);
    }

    public async Task UpdateAsync(MaintenanceWindow maintenanceWindow, CancellationToken stoppingToken)
    {
        var maintenanceWindowEntity = await context.MaintenanceWindows
            .Include(x => x.Components)
            .FirstAsync(x => x.MaintenanceWindowId == maintenanceWindow.MaintenanceWindowId && !x.IsDeleted, stoppingToken);

        var now = timeProvider.GetUtcNow();

        maintenanceWindowEntity.Title = maintenanceWindow.Title;
        maintenanceWindowEntity.Description = maintenanceWindow.Description;
        maintenanceWindowEntity.StatusId = (int)maintenanceWindow.Status;
        maintenanceWindowEntity.ScheduledStart = maintenanceWindow.ScheduledStart;
        maintenanceWindowEntity.ScheduledEnd = maintenanceWindow.ScheduledEnd;
        maintenanceWindowEntity.AnnouncedAt = maintenanceWindow.AnnouncedAt.HasValue ? maintenanceWindow.AnnouncedAt.Value : null;
        maintenanceWindowEntity.UpdatedAt = now;

        context.MaintenanceWindowComponents.RemoveRange(maintenanceWindowEntity.Components);

        foreach (var component in maintenanceWindow.Components)
        {
            await context.MaintenanceWindowComponents.AddAsync(new MaintenanceWindowComponentEntity
            {
                MaintenanceWindowId = maintenanceWindow.MaintenanceWindowId,
                ComponentId = (int)component,
                CreatedAt = now,
                UpdatedAt = now
            }, stoppingToken);
        }

        context.MaintenanceWindows.Update(maintenanceWindowEntity);
    }

    public async Task<List<MaintenanceWindow>> ListAnnouncedOverlappingAsync(PeriodStart periodStart, PeriodEnd periodEnd, CancellationToken stoppingToken)
    {
        var from = (DateTimeOffset)periodStart;
        var to = (DateTimeOffset)periodEnd;

        var maintenanceWindowEntities = await Queryable()
            .Where(x => !x.IsDeleted &&
                        x.AnnouncedAt != null &&
                        x.StatusId != (int)MaintenanceStatus.Cancelled &&
                        x.ScheduledStart < to &&
                        x.ScheduledEnd > from)
            .OrderBy(x => x.ScheduledStart)
            .ToListAsync(stoppingToken);

        return maintenanceWindowEntities.ConvertAll(x => x.ToModel());
    }

    public async Task<List<MaintenanceWindow>> ListUpcomingAsync(PeriodEnd horizon, CancellationToken stoppingToken)
    {
        var now = timeProvider.GetUtcNow();
        var to = (DateTimeOffset)horizon;

        var maintenanceWindowEntities = await Queryable()
            .Where(x => !x.IsDeleted &&
                        x.StatusId != (int)MaintenanceStatus.Cancelled &&
                        x.StatusId != (int)MaintenanceStatus.Completed &&
                        x.ScheduledStart <= to &&
                        x.ScheduledEnd > now)
            .OrderBy(x => x.ScheduledStart)
            .ToListAsync(stoppingToken);

        return maintenanceWindowEntities.ConvertAll(x => x.ToModel());
    }

    public async Task<List<MaintenanceWindow>> ListDueForTransitionAsync(CancellationToken stoppingToken)
    {
        var now = timeProvider.GetUtcNow();

        var maintenanceWindowEntities = await Queryable()
            .Where(x => !x.IsDeleted &&
                        ((x.StatusId == (int)MaintenanceStatus.Scheduled && x.ScheduledStart <= now) ||
                         (x.StatusId == (int)MaintenanceStatus.InProgress && x.ScheduledEnd <= now)))
            .OrderBy(x => x.ScheduledStart)
            .ToListAsync(stoppingToken);

        return maintenanceWindowEntities.ConvertAll(x => x.ToModel());
    }

    protected override Task<List<MaintenanceWindow>> MapPageAsync(List<MaintenanceWindowEntity> items, ListMaintenanceWindowRequest request, CancellationToken stoppingToken)
    {
        return Task.FromResult(items.ConvertAll(x => x.ToModel()));
    }

    protected override IOrderByExpression<MaintenanceWindowEntity> OrderByExpression()
    {
        return new OrderByExpression<MaintenanceWindowEntity, DateTimeOffset>(x => x.ScheduledStart);
    }

    protected override IQueryable<MaintenanceWindowEntity> FilterByPagedRequest(ListMaintenanceWindowRequest listMaintenanceWindowRequest)
    {
        var queryable = Queryable().Where(x => !x.IsDeleted);

        if (!listMaintenanceWindowRequest.IncludePast)
        {
            var now = timeProvider.GetUtcNow();
            queryable = queryable.Where(x => x.ScheduledEnd > now);
        }

        if (listMaintenanceWindowRequest.Status.HasValue)
        {
            queryable = queryable.Where(x => x.StatusId == (int)listMaintenanceWindowRequest.Status.Value);
        }

        if (listMaintenanceWindowRequest.Component.HasValue)
        {
            queryable = queryable.Where(x => x.Components.Any(y => y.ComponentId == (int)listMaintenanceWindowRequest.Component.Value));
        }

        return queryable;
    }

    protected override IQueryable<MaintenanceWindowEntity> FilterByDateRange(IQueryable<MaintenanceWindowEntity> queryable, DatedRequest datedRequest)
    {
        if (datedRequest.FromDate != DateOnly.MinValue)
        {
            queryable = queryable
                .Where(x => DateOnly.FromDateTime(x.ScheduledStart.UtcDateTime) >= datedRequest.FromDate);
        }

        if (datedRequest.ToDate != DateOnly.MaxValue)
        {
            queryable = queryable
                .Where(x => DateOnly.FromDateTime(x.ScheduledStart.UtcDateTime) <= datedRequest.ToDate);
        }

        return queryable;
    }

    private IQueryable<MaintenanceWindowEntity> Queryable()
    {
        return context.MaintenanceWindows.Include(x => x.Components);
    }
}
