using Core.Enums;
using Core.Models.PartyMappings;
using Core.Models.TransactionSchemas;
using Core.ValueObjects;

namespace Core.Models.Simulations;

public record SimulationPlan
{
    public required SimulationId SimulationId { get; init; }
    public required SimulationScenario Scenario { get; init; }
    public required PaymentCount PaymentCount { get; init; }
    public required SimulationSeed Seed { get; init; }
    public required TransactionSchema? TransactionSchema { get; init; }
    public required PartyMapping? PartyMapping { get; init; }
    public required EvaluatedAt GeneratedAt { get; init; }
}
