using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.Simulations;

public record UpdateSimulationRequest
{
    public required SimulationId SimulationId { get; init; }
    public required SimulationStatus Status { get; init; }
    public required ProcessedCount ProcessedCount { get; init; }
    public required CompletedAt? CompletedAt { get; init; }
    public required FailureReason? FailureReason { get; init; }
}
