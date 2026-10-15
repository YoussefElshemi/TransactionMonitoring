using Core.Models.Rules;
using Presentation.Models.Rules;

namespace Presentation.Mappers.Rules;

public static class RuleVersionDtoMapper
{
    public static RuleVersionResponseDto ToDto(this RuleVersion ruleVersion)
    {
        return new RuleVersionResponseDto
        {
            Mode = ruleVersion.Mode.ToString(),
            RuleVersionId = ruleVersion.RuleVersionId,
            RuleId = ruleVersion.RuleId,
            VersionNumber = ruleVersion.VersionNumber,
            RuleBody = ruleVersion.RuleBody.ToDto(),
            MatchPolarity = ruleVersion.MatchPolarity.ToString(),
            Action = ruleVersion.Action.ToString(),
            Severity = ruleVersion.Severity.ToString(),
            RiskWeight = ruleVersion.RiskWeight,
            RuleSetId = ruleVersion.RuleSetId,
            CreatedAt = ruleVersion.CreatedAt
        };
    }
}
