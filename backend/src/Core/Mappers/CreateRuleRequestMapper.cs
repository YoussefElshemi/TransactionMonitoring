using Core.Enums;
using Core.Models.Rules;
using Core.ValueObjects;

namespace Core.Mappers;

public static class CreateRuleRequestMapper
{
    public static Rule ToModel(this CreateRuleRequest createRuleRequest, RuleVersionId currentRuleVersionId, TimeProvider timeProvider, Mode mode)
    {
        return new Rule
        {
            Mode = mode,
            RuleId = new RuleId(Guid.NewGuid()),
            FriendlyName = createRuleRequest.FriendlyName,
            RuleBody = createRuleRequest.RuleBody,
            MatchPolarity = createRuleRequest.MatchPolarity,
            Action = createRuleRequest.Action,
            Severity = createRuleRequest.Severity,
            RiskWeight = createRuleRequest.RiskWeight,
            HoldWindow = createRuleRequest.HoldWindow,
            HoldExpiryVerdict = createRuleRequest.HoldExpiryVerdict,
            AlertSubjectRole = createRuleRequest.AlertSubjectRole,
            DeduplicationWindow = createRuleRequest.DeduplicationWindow,
            OrganizationId = createRuleRequest.OrganizationId,
            Organization = null,
            IsEnabled = createRuleRequest.IsEnabled,
            RuleSetId = createRuleRequest.RuleSetId,
            CreatedByUserId = createRuleRequest.CreatedByUserId,
            CurrentRuleVersionId = currentRuleVersionId,
            CurrentVersionNumber = new VersionNumber(1),
            HasActiveDeployment = new HasActiveDeployment(false),
            CreatedAt = new CreatedAt(timeProvider.GetUtcNow()),
            UpdatedAt = new UpdatedAt(timeProvider.GetUtcNow()),
            IsDeleted = new IsDeleted(false),
            DeletedAt = null
        };
    }
}