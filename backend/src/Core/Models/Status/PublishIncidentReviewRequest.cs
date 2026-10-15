using Core.ValueObjects;

namespace Core.Models.Status;

public record PublishIncidentReviewRequest
{
    public required IncidentId IncidentId { get; init; }
    public required RootCause RootCause { get; init; }
    public required Remediation Remediation { get; init; }
}
