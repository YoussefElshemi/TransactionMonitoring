using Core.Models.Decisions;
using Presentation.Mappers.RuleEvaluations;
using Presentation.Models.Decisions;

namespace Presentation.Mappers.Decisions;

public static class DecisionDtoMapper
{
    public static DecisionResponseDto ToDto(this Decision decision)
    {
        return new DecisionResponseDto
        {
            Mode = decision.Mode.ToString(),
            DecisionId = decision.DecisionId,
            TransactionId = decision.TransactionId,
            TransactionReference = decision.ExternalReference?.ToString(),
            Status = decision.Status.ToString(),
            ReasonCode = decision.ReasonCode.ToString(),
            CaseId = decision.CaseId,
            ResolvedByUserId = decision.ResolvedByUserId,
            FiredRules = decision.RuleEvaluations.Where(x => x.Matched).Select(x => x.ToDto()).ToArray(),
            HeldAt = decision.HeldAt,
            ExpiresAt = decision.ExpiresAt,
            HoldExpiryVerdict = decision.HoldExpiryVerdict?.ToString(),
            ResolvedAt = decision.ResolvedAt,
            CreatedAt = decision.CreatedAt,
            UpdatedAt = decision.UpdatedAt
        };
    }
}
