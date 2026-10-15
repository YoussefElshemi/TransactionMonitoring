using Core.Models.RuleDeployments;
using Presentation.Models.Rules;

namespace Presentation.Mappers.Rules;

public static class RuleDeploymentDtoMapper
{
    public static RuleDeploymentResponseDto ToDto(this RuleDeployment ruleDeployment)
    {
        return new RuleDeploymentResponseDto
        {
            Mode = ruleDeployment.Mode.ToString(),
            RuleDeploymentId = ruleDeployment.RuleDeploymentId,
            RuleId = ruleDeployment.RuleId,
            TargetRuleVersionId = ruleDeployment.TargetRuleVersionId,
            PreviousRuleVersionId = ruleDeployment.PreviousRuleVersionId.HasValue
                ? (Guid)ruleDeployment.PreviousRuleVersionId.Value
                : null,
            Strategy = ruleDeployment.Strategy.ToString(),
            Status = ruleDeployment.Status.ToString(),
            CurrentPercentage = ruleDeployment.CurrentPercentage,
            StepPercentage = ruleDeployment.StepPercentage,
            StepIntervalSeconds = ruleDeployment.StepIntervalSeconds,
            CreatedByUserId = ruleDeployment.CreatedByUserId,
            CreatedAt = ruleDeployment.CreatedAt,
            UpdatedAt = ruleDeployment.UpdatedAt,
            CompletedAt = ruleDeployment.CompletedAt
        };
    }
}
