using Core.Models.Status;
using Core.ValueObjects;
using Presentation.Models.Status;

namespace Presentation.Mappers.Status;

public static class MaintenanceWindowMapper
{
    public static CreateMaintenanceWindowRequest ToModel(this CreateMaintenanceWindowRequestDto dto)
    {
        return new CreateMaintenanceWindowRequest
        {
            Title = new MaintenanceWindowTitle(dto.Body.Title),
            Description = new MaintenanceWindowDescription(dto.Body.Description),
            Components = dto.Body.Components,
            ScheduledStart = new ScheduledStart(dto.Body.ScheduledStart),
            ScheduledEnd = new ScheduledEnd(dto.Body.ScheduledEnd),
            CreatedByUserId = dto.CreatedByUserId.HasValue ? new ApplicationUserId(dto.CreatedByUserId.Value) : null
        };
    }

    public static UpdateMaintenanceWindowRequest ToModel(this UpdateMaintenanceWindowRequestDto dto)
    {
        return new UpdateMaintenanceWindowRequest
        {
            MaintenanceWindowId = new MaintenanceWindowId(dto.MaintenanceWindowId),
            Title = new MaintenanceWindowTitle(dto.Body.Title),
            Description = new MaintenanceWindowDescription(dto.Body.Description),
            Components = dto.Body.Components,
            ScheduledStart = new ScheduledStart(dto.Body.ScheduledStart),
            ScheduledEnd = new ScheduledEnd(dto.Body.ScheduledEnd)
        };
    }

    public static ListMaintenanceWindowRequest ToModel(this ListMaintenanceWindowRequestDto dto)
    {
        return new ListMaintenanceWindowRequest
        {
            Component = dto.Component,
            Status = dto.Status,
            IncludePast = new IncludePast(dto.IncludePast ?? false),
            PageNumber = new PageNumber(dto.PageNumber),
            PageSize = new PageSize(dto.PageSize),
            FromDate = new DateRange(dto.FromDate),
            ToDate = new DateRange(dto.ToDate)
        };
    }

    public static MaintenanceWindowResponseDto ToDto(this MaintenanceWindow maintenanceWindow)
    {
        return new MaintenanceWindowResponseDto
        {
            MaintenanceWindowId = maintenanceWindow.MaintenanceWindowId,
            Title = maintenanceWindow.Title,
            Description = maintenanceWindow.Description,
            Status = maintenanceWindow.Status.ToString(),
            Components = maintenanceWindow.Components.ConvertAll(x => x.ToString()),
            ScheduledStart = maintenanceWindow.ScheduledStart,
            ScheduledEnd = maintenanceWindow.ScheduledEnd,
            AnnouncedAt = maintenanceWindow.AnnouncedAt.HasValue ? maintenanceWindow.AnnouncedAt.Value : null,
            CreatedAt = maintenanceWindow.CreatedAt,
            UpdatedAt = maintenanceWindow.UpdatedAt
        };
    }
}
