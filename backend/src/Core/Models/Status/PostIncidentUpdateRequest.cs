using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.Status;

public record PostIncidentUpdateRequest
{
    public required IncidentId IncidentId { get; init; }
    public required IncidentStatus Status { get; init; }
    public required IncidentUpdateBody Body { get; init; }
    public required ApplicationUserId? PostedByUserId { get; init; }
}
