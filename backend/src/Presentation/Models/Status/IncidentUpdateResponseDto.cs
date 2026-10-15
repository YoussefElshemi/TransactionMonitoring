namespace Presentation.Models.Status;

public record IncidentUpdateResponseDto
{
    public required Guid IncidentUpdateId { get; init; }
    public required string Status { get; init; }
    public required string Body { get; init; }
    public required DateTimeOffset PostedAt { get; init; }
}
