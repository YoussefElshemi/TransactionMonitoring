using Core.Models.Status;
using Core.ValueObjects;

namespace Core.Interfaces.Repositories;

public interface IMaintenanceWindowRepository : IPagedRepository<ListMaintenanceWindowRequest, MaintenanceWindow>
{
    Task AddAsync(MaintenanceWindow maintenanceWindow, CancellationToken stoppingToken);
    Task<MaintenanceWindow> GetAsync(MaintenanceWindowId maintenanceWindowId, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(MaintenanceWindowId maintenanceWindowId, CancellationToken stoppingToken);
    Task UpdateAsync(MaintenanceWindow maintenanceWindow, CancellationToken stoppingToken);

    Task<List<MaintenanceWindow>> ListAnnouncedOverlappingAsync(PeriodStart periodStart, PeriodEnd periodEnd, CancellationToken stoppingToken);

    Task<List<MaintenanceWindow>> ListUpcomingAsync(PeriodEnd horizon, CancellationToken stoppingToken);
    Task<List<MaintenanceWindow>> ListDueForTransitionAsync(CancellationToken stoppingToken);
}
