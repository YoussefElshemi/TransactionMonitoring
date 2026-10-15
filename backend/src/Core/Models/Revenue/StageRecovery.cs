using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.Revenue;

public record StageRecovery
{
    public required DunningStage Stage { get; init; }
    public required Count Resolved { get; init; }
}
