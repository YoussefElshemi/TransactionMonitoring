using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.Simulations;

public record StartSimulationRequest
{
    public required OrganizationId OrganizationId { get; init; }
    public required SimulationScenario Scenario { get; init; }
    public required PaymentCount PaymentCount { get; init; }

    public required SimulationSeed? Seed { get; init; }
    public required ApplicationUserId CreatedByUserId { get; init; }
}
