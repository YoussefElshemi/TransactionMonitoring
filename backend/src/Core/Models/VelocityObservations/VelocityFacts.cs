using Core.ValueObjects;

namespace Core.Models.VelocityObservations;

public record VelocityFacts
{
    public required IReadOnlyList<VelocityWindowFact> Windows { get; init; }

    public required IReadOnlyDictionary<string, decimal> Thresholds { get; init; }
    public required TransactionId? CurrentTransactionId { get; init; }

    public static VelocityFacts Empty { get; } = new()
    {
        Windows = [],
        Thresholds = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase),
        CurrentTransactionId = null
    };
}

public record VelocityWindowFact
{
    public required VelocityWindowQuery Query { get; init; }
    public required PartyId PartyId { get; init; }
    public required VelocityAggregate Aggregate { get; init; }
}
