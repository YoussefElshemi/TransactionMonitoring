namespace Presentation.Models.Status;

public record ComponentHealthResponseDto
{
    public required string Component { get; init; }
    public required string Status { get; init; }
    public required DateTimeOffset? LastProbedAt { get; init; }
    public required int? LatencyMilliseconds { get; init; }
    public required decimal UptimePercentage { get; init; }
    public required List<ComponentUptimeDayResponseDto> History { get; init; }
}
