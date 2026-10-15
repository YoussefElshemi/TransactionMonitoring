using Core.Enums;

namespace Core.Configs;

public record DecisionConfig
{
    public int DefaultHoldWindowMinutes { get; init; } = 1440;
    public HoldExpiryVerdict DefaultHoldExpiryVerdict { get; init; } = HoldExpiryVerdict.Block;
    public int HoldExpiryBatchSize { get; init; } = 100;
}
