using Core.Enums;
using Core.Models.BaseModels;
using Core.ValueObjects;

namespace Core.Models.Simulations;

public record Simulation : BaseModel
{
    public required SimulationId SimulationId { get; init; }
    public required OrganizationId OrganizationId { get; init; }
    public required SimulationScenario Scenario { get; init; }
    public required SimulationStatus Status { get; init; }
    public required PaymentCount TotalPayments { get; init; }
    public required ProcessedCount ProcessedCount { get; init; }
    public required SimulationSeed Seed { get; init; }
    public required ApplicationUserId CreatedByUserId { get; init; }
    public required CompletedAt? CompletedAt { get; init; }
    public required FailureReason? FailureReason { get; init; }

    public required Mode Mode { get; init; }
}
