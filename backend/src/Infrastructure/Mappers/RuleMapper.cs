using Core.Enums;
using Core.Extensions;
using Core.Models.Rules;
using Core.ValueObjects;
using Infrastructure.Entities;

namespace Infrastructure.Mappers;

public static class RuleMapper
{
    public static Rule ToModel(this RuleEntity ruleEntity)
    {
        return new Rule
        {
            Mode = (Mode)ruleEntity.ModeId,
            RuleId = new RuleId(ruleEntity.RuleId),
            FriendlyName = new FriendlyName(ruleEntity.FriendlyName),
            RuleBody = ruleEntity.RuleBody.FromBase64String().TryDeserialize<RuleBodyModel>().ToModel(),
            MatchPolarity = (RuleMatchPolarity)ruleEntity.MatchPolarityId,
            Action = (RuleAction)ruleEntity.ActionId,
            Severity = (RuleSeverity)ruleEntity.SeverityId,
            RiskWeight = new RiskWeight(ruleEntity.RiskWeight),
            HoldWindow = ruleEntity.HoldWindow.HasValue ? new HoldWindow(ruleEntity.HoldWindow.Value) : null,
            HoldExpiryVerdict = (HoldExpiryVerdict?)ruleEntity.HoldExpiryVerdictId,
            AlertSubjectRole = (PartyRole?)ruleEntity.AlertSubjectRoleId,
            DeduplicationWindow = ruleEntity.DeduplicationWindow.HasValue ? new DeduplicationWindow(ruleEntity.DeduplicationWindow.Value) : null,
            OrganizationId = new OrganizationId(ruleEntity.OrganizationId),
            Organization = ruleEntity.Organization.ToModel(),
            IsEnabled = new IsEnabled(ruleEntity.IsEnabled),
            RuleSetId = new RuleSetId(ruleEntity.RuleSetId),
            CreatedByUserId = new ApplicationUserId(ruleEntity.CreatedByUserId),
            CurrentRuleVersionId = new RuleVersionId(ruleEntity.CurrentRuleVersionId),
            CurrentVersionNumber = new VersionNumber(ruleEntity.CurrentVersionNumber),
            HasActiveDeployment = new HasActiveDeployment(ruleEntity.HasActiveDeployment),
            CreatedAt = new CreatedAt(ruleEntity.CreatedAt),
            UpdatedAt = new UpdatedAt(ruleEntity.UpdatedAt),
            IsDeleted = new IsDeleted(ruleEntity.IsDeleted),
            DeletedAt = ruleEntity.DeletedAt.HasValue ? new DeletedAt(ruleEntity.DeletedAt.Value) : null
        };
    }

    public static RuleEntity ToEntity(this Rule rule)
    {
        return new RuleEntity
        {
            ModeId = (int)rule.Mode,
            RuleId = rule.RuleId,
            FriendlyName = rule.FriendlyName.ToString(),
            RuleBody = rule.RuleBody.ToBase64String(),
            MatchPolarityId = (int)rule.MatchPolarity,
            ActionId = (int)rule.Action,
            SeverityId = (int)rule.Severity,
            RiskWeight = rule.RiskWeight,
            HoldWindow = rule.HoldWindow,
            HoldExpiryVerdictId = (int?)rule.HoldExpiryVerdict,
            AlertSubjectRoleId = (int?)rule.AlertSubjectRole,
            DeduplicationWindow = rule.DeduplicationWindow,
            OrganizationId = rule.OrganizationId,
            IsEnabled = rule.IsEnabled,
            RuleSetId = rule.RuleSetId,
            CreatedByUserId = rule.CreatedByUserId,
            CurrentRuleVersionId = rule.CurrentRuleVersionId,
            CurrentVersionNumber = rule.CurrentVersionNumber,
            HasActiveDeployment = rule.HasActiveDeployment,
            CreatedAt = rule.CreatedAt,
            UpdatedAt = rule.UpdatedAt,
            IsDeleted = rule.IsDeleted,
            DeletedAt = rule.DeletedAt
        };
    }
}