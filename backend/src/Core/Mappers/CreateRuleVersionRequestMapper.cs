using Core.Enums;
using Core.Models.Rules;
using Core.ValueObjects;

namespace Core.Mappers;

public static class CreateRuleVersionRequestMapper
{
    public static RuleVersion ToVersion(this CreateRuleVersionRequest request, Rule rule, RuleVersionId ruleVersionId, VersionNumber versionNumber, TimeProvider timeProvider, Mode mode)
    {
        return new RuleVersion
        {
            Mode = mode,
            RuleVersionId = ruleVersionId,
            RuleId = rule.RuleId,
            VersionNumber = versionNumber,
            RuleBody = request.RuleBody,
            MatchPolarity = rule.MatchPolarity,
            Action = request.Action,
            Severity = request.Severity,
            RiskWeight = request.RiskWeight,
            RuleSetId = rule.RuleSetId,
            OrganizationId = rule.OrganizationId,
            CreatedByUserId = request.CreatedByUserId,
            CreatedAt = new CreatedAt(timeProvider.GetUtcNow())
        };
    }
}
