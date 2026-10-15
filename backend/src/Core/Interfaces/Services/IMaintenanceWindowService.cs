using Core.Models.Status;
using Core.ValueObjects;

namespace Core.Interfaces.Services;

public interface IMaintenanceWindowService : IPagedService<ListMaintenanceWindowRequest, MaintenanceWindow>
{
    Task<MaintenanceWindow> CreateAsync(CreateMaintenanceWindowRequest createMaintenanceWindowRequest, CancellationToken stoppingToken);
    Task<MaintenanceWindow> GetAsync(MaintenanceWindowId maintenanceWindowId, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(MaintenanceWindowId maintenanceWindowId, CancellationToken stoppingToken);
    Task<MaintenanceWindow> UpdateAsync(UpdateMaintenanceWindowRequest updateMaintenanceWindowRequest, CancellationToken stoppingToken);
    Task<MaintenanceWindow> CancelAsync(MaintenanceWindowId maintenanceWindowId, CancellationToken stoppingToken);
    Task<List<MaintenanceWindow>> ListUpcomingAsync(CancellationToken stoppingToken);

    Task<List<MaintenanceWindow>> ListAnnouncedOverlappingAsync(PeriodStart periodStart, PeriodEnd periodEnd, CancellationToken stoppingToken);

    Task<Count> TransitionDueAsync(CancellationToken stoppingToken);
}
