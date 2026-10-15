using Core.Enums;
using Core.Models.RuleSets;
using Core.ValueObjects;

namespace Core.Mappers;

public static class CreateRuleSetRequestMapper
{
    public static RuleSet ToModel(this CreateRuleSetRequest createRuleSetRequest, TimeProvider timeProvider, Mode mode)
    {
        return new RuleSet
        {
            Mode = mode,
            RuleSetId = new RuleSetId(Guid.NewGuid()),
            FriendlyName = createRuleSetRequest.FriendlyName,
            OrganizationId = createRuleSetRequest.OrganizationId,
            SubOrganizationId = createRuleSetRequest.SubOrganizationId,
            IsEnabled = createRuleSetRequest.IsEnabled,
            ReviewThreshold = createRuleSetRequest.ReviewThreshold,
            RuleCount = new RuleCount(0),
            Rules = [],
            CreatedByUserId = createRuleSetRequest.CreatedByUserId,
            CreatedAt = new CreatedAt(timeProvider.GetUtcNow()),
            UpdatedAt = new UpdatedAt(timeProvider.GetUtcNow()),
            IsDeleted = new IsDeleted(false),
            DeletedAt = null
        };
    }
}