using Core.Models.Rules;
using Presentation.Models.Rules;

namespace Presentation.Mappers.Rules;

public static class RuleDtoMapper
{
    public static RuleResponseDto ToDto(this Rule rule)
    {
        return new RuleResponseDto
        {
            Mode = rule.Mode.ToString(),
            RuleId = rule.RuleId,
            FriendlyName = rule.FriendlyName,
            RuleBody = rule.RuleBody.ToDto(),
            MatchPolarity = rule.MatchPolarity.ToString(),
            Action = rule.Action.ToString(),
            Severity = rule.Severity.ToString(),
            RiskWeight = rule.RiskWeight,
            HoldWindow = rule.HoldWindow,
            HoldExpiryVerdict = rule.HoldExpiryVerdict?.ToString(),
            OrganizationId = rule.OrganizationId,
            IsEnabled = rule.IsEnabled,
            CreatedByUserId = rule.CreatedByUserId,
            RuleSetId = rule.RuleSetId,
            CreatedAt = rule.CreatedAt,
            UpdatedAt = rule.UpdatedAt,
            IsDeleted = rule.IsDeleted
        };
    }
}
