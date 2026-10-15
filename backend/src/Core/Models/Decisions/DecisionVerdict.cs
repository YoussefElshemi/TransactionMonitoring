using Core.Enums;

namespace Core.Models.Decisions;

public record DecisionVerdict
{
    public required DecisionStatus Status { get; init; }
    public required DecisionReasonCode ReasonCode { get; init; }
}
