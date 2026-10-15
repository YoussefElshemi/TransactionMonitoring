using Core.Configs;
using Core.Enums;
using Core.Exceptions;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Mappers;
using Core.Models.Common;
using Core.Models.Status;
using Core.ValueObjects;
using Microsoft.Extensions.Options;

namespace Core.Services;

public class MaintenanceWindowService(
    IMaintenanceWindowRepository maintenanceWindowRepository,
    IStatusNotifier statusNotifier,
    IUnitOfWork unitOfWork,
    IOptions<AppConfig> appConfig,
    TimeProvider timeProvider) : IMaintenanceWindowService
{
    private readonly StatusConfig _statusConfig = appConfig.Value.StatusConfig;

    public async Task<MaintenanceWindow> CreateAsync(CreateMaintenanceWindowRequest createMaintenanceWindowRequest, CancellationToken stoppingToken)
    {
        var maintenanceWindow = createMaintenanceWindowRequest.ToModel(timeProvider);

        if (maintenanceWindow.ScheduledEnd <= (DateTimeOffset)maintenanceWindow.ScheduledStart)
        {
            throw new InvalidTimeRangeException("maintenance window");
        }

        await maintenanceWindowRepository.AddAsync(maintenanceWindow, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);

        var created = await GetAsync(maintenanceWindow.MaintenanceWindowId, stoppingToken);

        await statusNotifier.NotifySubscribersAsync(created, stoppingToken);

        return created;
    }

    public Task<MaintenanceWindow> GetAsync(MaintenanceWindowId maintenanceWindowId, CancellationToken stoppingToken)
    {
        return maintenanceWindowRepository.GetAsync(maintenanceWindowId, stoppingToken);
    }

    public Task<Exists> ExistsAsync(MaintenanceWindowId maintenanceWindowId, CancellationToken stoppingToken)
    {
        return maintenanceWindowRepository.ExistsAsync(maintenanceWindowId, stoppingToken);
    }

    public async Task<MaintenanceWindow> UpdateAsync(UpdateMaintenanceWindowRequest updateMaintenanceWindowRequest, CancellationToken stoppingToken)
    {
        var maintenanceWindow = await GetAsync(updateMaintenanceWindowRequest.MaintenanceWindowId, stoppingToken);

        if (maintenanceWindow.Status != MaintenanceStatus.Scheduled)
        {
            throw new MaintenanceWindowStartedException(maintenanceWindow.MaintenanceWindowId);
        }

        if (updateMaintenanceWindowRequest.ScheduledEnd <= (DateTimeOffset)updateMaintenanceWindowRequest.ScheduledStart)
        {
            throw new InvalidTimeRangeException("maintenance window");
        }

        var amended = maintenanceWindow with
        {
            Title = updateMaintenanceWindowRequest.Title,
            Description = updateMaintenanceWindowRequest.Description,
            Components = updateMaintenanceWindowRequest.Components.Distinct().OrderBy(x => x).ToList(),
            ScheduledStart = updateMaintenanceWindowRequest.ScheduledStart,
            ScheduledEnd = updateMaintenanceWindowRequest.ScheduledEnd,

            AnnouncedAt = new AnnouncedAt(timeProvider.GetUtcNow())
        };

        await maintenanceWindowRepository.UpdateAsync(amended, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);

        return await GetAsync(maintenanceWindow.MaintenanceWindowId, stoppingToken);
    }

    public async Task<MaintenanceWindow> CancelAsync(MaintenanceWindowId maintenanceWindowId, CancellationToken stoppingToken)
    {
        var maintenanceWindow = await GetAsync(maintenanceWindowId, stoppingToken);

        if (maintenanceWindow.Status != MaintenanceStatus.Scheduled)
        {
            throw new MaintenanceWindowStartedException(maintenanceWindowId);
        }

        await maintenanceWindowRepository.UpdateAsync(
            maintenanceWindow with { Status = MaintenanceStatus.Cancelled }, stoppingToken);

        await unitOfWork.SaveChangesAsync(stoppingToken);

        return await GetAsync(maintenanceWindowId, stoppingToken);
    }

    public Task<List<MaintenanceWindow>> ListUpcomingAsync(CancellationToken stoppingToken)
    {
        var horizon = new PeriodEnd(timeProvider.GetUtcNow().AddDays(30));

        return maintenanceWindowRepository.ListUpcomingAsync(horizon, stoppingToken);
    }

    public async Task<List<MaintenanceWindow>> ListAnnouncedOverlappingAsync(PeriodStart periodStart, PeriodEnd periodEnd, CancellationToken stoppingToken)
    {
        var maintenanceWindows = await maintenanceWindowRepository.ListAnnouncedOverlappingAsync(periodStart, periodEnd, stoppingToken);

        return maintenanceWindows
            .Where(x => x.AnnouncedAt is not null &&
                        x.ScheduledStart - (DateTimeOffset)x.AnnouncedAt.Value >=
                        TimeSpan.FromHours(_statusConfig.MinimumMaintenanceNoticeHours))
            .ToList();
    }

    public async Task<Count> TransitionDueAsync(CancellationToken stoppingToken)
    {
        var maintenanceWindows = await maintenanceWindowRepository.ListDueForTransitionAsync(stoppingToken);

        foreach (var maintenanceWindow in maintenanceWindows)
        {
            var status = maintenanceWindow.Status == MaintenanceStatus.Scheduled
                ? MaintenanceStatus.InProgress
                : MaintenanceStatus.Completed;

            await maintenanceWindowRepository.UpdateAsync(maintenanceWindow with { Status = status }, stoppingToken);
        }

        await unitOfWork.SaveChangesAsync(stoppingToken);

        return new Count(maintenanceWindows.Count);
    }

    public Task<PagedResponse<MaintenanceWindow>> ListAsync(ListMaintenanceWindowRequest listMaintenanceWindowRequest, CancellationToken stoppingToken)
    {
        return maintenanceWindowRepository.ListAsync(listMaintenanceWindowRequest, stoppingToken);
    }

    public Task<TotalCount> GetCountAsync(ListMaintenanceWindowRequest listMaintenanceWindowRequest, CancellationToken stoppingToken)
    {
        return maintenanceWindowRepository.GetCountAsync(listMaintenanceWindowRequest, stoppingToken);
    }
}
