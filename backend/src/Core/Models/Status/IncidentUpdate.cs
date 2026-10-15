using Core.Enums;
using Core.Models.BaseModels;
using Core.ValueObjects;

namespace Core.Models.Status;

public record IncidentUpdate : BaseModel
{
    public required IncidentUpdateId IncidentUpdateId { get; init; }
    public required IncidentId IncidentId { get; init; }
    public required IncidentStatus Status { get; init; }
    public required IncidentUpdateBody Body { get; init; }
    public required ObservedAt PostedAt { get; init; }

    public required ApplicationUserId? PostedByUserId { get; init; }
}
