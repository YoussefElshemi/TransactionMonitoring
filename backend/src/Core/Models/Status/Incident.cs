using Core.Enums;
using Core.Models.BaseModels;
using Core.ValueObjects;

namespace Core.Models.Status;

public record Incident : DeletableModel
{
    public required IncidentId IncidentId { get; init; }
    public required IncidentNumber IncidentNumber { get; init; }
    public required IncidentTitle Title { get; init; }
    public required IncidentSummary Summary { get; init; }
    public required IncidentStatus Status { get; init; }
    public required IncidentImpact Impact { get; init; }

    public required List<ServiceComponent> Components { get; init; }

    public required StartedAt StartedAt { get; init; }
    public required ResolvedAt? ResolvedAt { get; init; }

    public required RootCause? RootCause { get; init; }
    public required Remediation? Remediation { get; init; }
    public required PublishedAt? ReviewPublishedAt { get; init; }

    public required ApplicationUserId? CreatedByUserId { get; init; }
    public required List<IncidentUpdate> Updates { get; init; }
}
