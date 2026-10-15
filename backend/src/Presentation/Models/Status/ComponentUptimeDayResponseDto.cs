namespace Presentation.Models.Status;

public record ComponentUptimeDayResponseDto
{
    public required DateOnly Date { get; init; }
    public required int TotalProbes { get; init; }
    public required int HealthyProbes { get; init; }
    public required int DowntimeSeconds { get; init; }
    public required decimal UptimePercentage { get; init; }
}
