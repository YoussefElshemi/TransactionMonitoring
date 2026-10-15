namespace Presentation.Models.Status;

public record ServiceStatusResponseDto
{
    public required string Status { get; init; }
    public required DateTimeOffset ObservedAt { get; init; }
    public required List<ComponentHealthResponseDto> Components { get; init; }
    public required List<IncidentResponseDto> ActiveIncidents { get; init; }
    public required List<MaintenanceWindowResponseDto> UpcomingMaintenance { get; init; }
}
