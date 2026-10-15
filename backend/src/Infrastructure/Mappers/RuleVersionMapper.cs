using Core.Enums;
using Core.Extensions;
using Core.Models.Rules;
using Core.ValueObjects;
using Infrastructure.Entities;

namespace Infrastructure.Mappers;

public static class RuleVersionMapper
{
    public static RuleVersion ToModel(this RuleVersionEntity ruleVersionEntity)
    {
        return new RuleVersion
        {
            Mode = (Mode)ruleVersionEntity.ModeId,
            RuleVersionId = new RuleVersionId(ruleVersionEntity.RuleVersionId),
            RuleId = new RuleId(ruleVersionEntity.RuleId),
            VersionNumber = new VersionNumber(ruleVersionEntity.VersionNumber),
            RuleBody = ruleVersionEntity.RuleBody.FromBase64String().TryDeserialize<RuleBodyModel>().ToModel(),
            MatchPolarity = (RuleMatchPolarity)ruleVersionEntity.MatchPolarityId,
            Action = (RuleAction)ruleVersionEntity.ActionId,
            Severity = (RuleSeverity)ruleVersionEntity.SeverityId,
            RiskWeight = new RiskWeight(ruleVersionEntity.RiskWeight),
            RuleSetId = new RuleSetId(ruleVersionEntity.RuleSetId),
            OrganizationId = new OrganizationId(ruleVersionEntity.OrganizationId),
            CreatedByUserId = new ApplicationUserId(ruleVersionEntity.CreatedByUserId),
            CreatedAt = new CreatedAt(ruleVersionEntity.CreatedAt)
        };
    }

    public static RuleVersionEntity ToEntity(this RuleVersion ruleVersion)
    {
        return new RuleVersionEntity
        {
            ModeId = (int)ruleVersion.Mode,
            RuleVersionId = ruleVersion.RuleVersionId,
            RuleId = ruleVersion.RuleId,
            VersionNumber = ruleVersion.VersionNumber,
            RuleBody = ruleVersion.RuleBody.ToBase64String(),
            MatchPolarityId = (int)ruleVersion.MatchPolarity,
            ActionId = (int)ruleVersion.Action,
            SeverityId = (int)ruleVersion.Severity,
            RiskWeight = ruleVersion.RiskWeight,
            RuleSetId = ruleVersion.RuleSetId,
            OrganizationId = ruleVersion.OrganizationId,
            CreatedByUserId = ruleVersion.CreatedByUserId,
            CreatedAt = ruleVersion.CreatedAt
        };
    }
}
