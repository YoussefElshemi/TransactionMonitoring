using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.Status;

public record CreateIncidentRequest
{
    public required IncidentTitle Title { get; init; }
    public required IncidentSummary Summary { get; init; }
    public required IncidentImpact Impact { get; init; }
    public required List<ServiceComponent> Components { get; init; }

    public required StartedAt? StartedAt { get; init; }

    public required ApplicationUserId? CreatedByUserId { get; init; }
}
