using Core.Enums;
using Core.Models.Status;
using Core.ValueObjects;

namespace Core.Mappers;

public static class CreateMaintenanceWindowRequestMapper
{
    public static MaintenanceWindow ToModel(this CreateMaintenanceWindowRequest request, TimeProvider timeProvider)
    {
        var now = timeProvider.GetUtcNow();

        return new MaintenanceWindow
        {
            MaintenanceWindowId = new MaintenanceWindowId(Guid.NewGuid()),
            Title = request.Title,
            Description = request.Description,
            Status = MaintenanceStatus.Scheduled,
            Components = request.Components.Distinct().OrderBy(x => x).ToList(),
            ScheduledStart = request.ScheduledStart,
            ScheduledEnd = request.ScheduledEnd,

            AnnouncedAt = new AnnouncedAt(now),

            CreatedByUserId = request.CreatedByUserId,
            CreatedAt = new CreatedAt(now),
            UpdatedAt = new UpdatedAt(now),
            IsDeleted = new IsDeleted(false),
            DeletedAt = null
        };
    }
}
