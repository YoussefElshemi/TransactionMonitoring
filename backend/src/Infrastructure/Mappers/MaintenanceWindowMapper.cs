using Core.Enums;
using Core.Models.Status;
using Core.ValueObjects;
using Infrastructure.Entities;

namespace Infrastructure.Mappers;

public static class MaintenanceWindowMapper
{
    public static MaintenanceWindow ToModel(this MaintenanceWindowEntity maintenanceWindowEntity)
    {
        return new MaintenanceWindow
        {
            MaintenanceWindowId = new MaintenanceWindowId(maintenanceWindowEntity.MaintenanceWindowId),
            Title = new MaintenanceWindowTitle(maintenanceWindowEntity.Title),
            Description = new MaintenanceWindowDescription(maintenanceWindowEntity.Description),
            Status = (MaintenanceStatus)maintenanceWindowEntity.StatusId,
            Components = maintenanceWindowEntity.Components
                .Select(x => (ServiceComponent)x.ComponentId)
                .OrderBy(x => x)
                .ToList(),
            ScheduledStart = new ScheduledStart(maintenanceWindowEntity.ScheduledStart),
            ScheduledEnd = new ScheduledEnd(maintenanceWindowEntity.ScheduledEnd),
            AnnouncedAt = maintenanceWindowEntity.AnnouncedAt.HasValue ? new AnnouncedAt(maintenanceWindowEntity.AnnouncedAt.Value) : null,
            CreatedByUserId = maintenanceWindowEntity.CreatedByUserId.HasValue ? new ApplicationUserId(maintenanceWindowEntity.CreatedByUserId.Value) : null,
            CreatedAt = new CreatedAt(maintenanceWindowEntity.CreatedAt),
            UpdatedAt = new UpdatedAt(maintenanceWindowEntity.UpdatedAt),
            IsDeleted = new IsDeleted(maintenanceWindowEntity.IsDeleted),
            DeletedAt = maintenanceWindowEntity.DeletedAt.HasValue ? new DeletedAt(maintenanceWindowEntity.DeletedAt.Value) : null
        };
    }

    public static MaintenanceWindowEntity ToEntity(this MaintenanceWindow maintenanceWindow)
    {
        return new MaintenanceWindowEntity
        {
            MaintenanceWindowId = maintenanceWindow.MaintenanceWindowId,
            Title = maintenanceWindow.Title,
            Description = maintenanceWindow.Description,
            StatusId = (int)maintenanceWindow.Status,
            ScheduledStart = maintenanceWindow.ScheduledStart,
            ScheduledEnd = maintenanceWindow.ScheduledEnd,
            AnnouncedAt = maintenanceWindow.AnnouncedAt.HasValue ? maintenanceWindow.AnnouncedAt.Value : null,
            CreatedByUserId = maintenanceWindow.CreatedByUserId.HasValue ? maintenanceWindow.CreatedByUserId.Value : null,
            Components = maintenanceWindow.Components
                .Select(x => new MaintenanceWindowComponentEntity
                {
                    MaintenanceWindowId = maintenanceWindow.MaintenanceWindowId,
                    ComponentId = (int)x,
                    CreatedAt = maintenanceWindow.CreatedAt,
                    UpdatedAt = maintenanceWindow.UpdatedAt
                })
                .ToList(),
            CreatedAt = maintenanceWindow.CreatedAt,
            UpdatedAt = maintenanceWindow.UpdatedAt,
            IsDeleted = maintenanceWindow.IsDeleted,
            DeletedAt = maintenanceWindow.DeletedAt
        };
    }
}
