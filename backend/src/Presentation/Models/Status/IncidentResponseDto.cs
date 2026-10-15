namespace Presentation.Models.Status;

public record IncidentResponseDto
{
    public required Guid IncidentId { get; init; }
    public required string IncidentNumber { get; init; }
    public required string Title { get; init; }
    public required string Summary { get; init; }
    public required string Status { get; init; }
    public required string Impact { get; init; }
    public required List<string> Components { get; init; }
    public required DateTimeOffset StartedAt { get; init; }
    public required DateTimeOffset? ResolvedAt { get; init; }
    public required string? RootCause { get; init; }
    public required string? Remediation { get; init; }
    public required DateTimeOffset? ReviewPublishedAt { get; init; }
    public required List<IncidentUpdateResponseDto> Updates { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
}
