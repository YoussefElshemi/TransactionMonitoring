using Core.Enums;
using Core.Models.Rules;
using Core.ValueObjects;

namespace Core.Mappers;

public static class RuleVersionMapper
{
    public static RuleVersion ToVersion(this Rule rule, RuleVersionId ruleVersionId, VersionNumber versionNumber, ApplicationUserId createdByUserId, TimeProvider timeProvider, Mode mode)
    {
        return new RuleVersion
        {
            Mode = mode,
            RuleVersionId = ruleVersionId,
            RuleId = rule.RuleId,
            VersionNumber = versionNumber,
            RuleBody = rule.RuleBody,
            MatchPolarity = rule.MatchPolarity,
            Action = rule.Action,
            Severity = rule.Severity,
            RiskWeight = rule.RiskWeight,
            RuleSetId = rule.RuleSetId,
            OrganizationId = rule.OrganizationId,
            CreatedByUserId = createdByUserId,
            CreatedAt = new CreatedAt(timeProvider.GetUtcNow())
        };
    }
}
