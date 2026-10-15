using Core.Models.RuleSets;
using Presentation.Models.RuleSets;

namespace Presentation.Mappers.RuleSets;

public static class RuleSetDtoMapper
{
    public static RuleSetResponseDto ToDto(this RuleSet ruleSet)
    {
        return new RuleSetResponseDto
        {
            Mode = ruleSet.Mode.ToString(),
            RuleSetId = ruleSet.RuleSetId,
            FriendlyName = ruleSet.FriendlyName,
            RuleCount = ruleSet.RuleCount,
            OrganizationId = ruleSet.OrganizationId,
            IsEnabled = ruleSet.IsEnabled,
            ReviewThreshold = ruleSet.ReviewThreshold,
            CreatedByUserId = ruleSet.CreatedByUserId,
            CreatedAt = ruleSet.CreatedAt,
            UpdatedAt = ruleSet.UpdatedAt
        };
    }
}
