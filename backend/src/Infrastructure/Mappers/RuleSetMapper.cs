using Core.Enums;
using Core.Models.RuleSets;
using Core.ValueObjects;
using Infrastructure.Entities;

namespace Infrastructure.Mappers;

public static class RuleSetMapper
{
    public static RuleSet ToModel(this RuleSetEntity ruleSetEntity, int ruleCount)
    {
        return new RuleSet
        {
            Mode = (Mode)ruleSetEntity.ModeId,
            RuleCount = new RuleCount(ruleCount),
            RuleSetId = new RuleSetId(ruleSetEntity.RuleSetId),
            FriendlyName = new FriendlyName(ruleSetEntity.FriendlyName),
            OrganizationId = new OrganizationId(ruleSetEntity.OrganizationId),
            SubOrganizationId = ruleSetEntity.SubOrganizationId.HasValue ? new SubOrganizationId(ruleSetEntity.SubOrganizationId.Value) : null,
            IsEnabled = new IsEnabled(ruleSetEntity.IsEnabled),
            ReviewThreshold = ruleSetEntity.ReviewThreshold.HasValue ? new ReviewThreshold(ruleSetEntity.ReviewThreshold.Value) : null,
            Rules = ruleSetEntity.Rules.Select(x => x.ToModel()).ToList(),
            CreatedByUserId = new ApplicationUserId(ruleSetEntity.CreatedByUserId),
            CreatedAt = new CreatedAt(ruleSetEntity.CreatedAt),
            UpdatedAt = new UpdatedAt(ruleSetEntity.UpdatedAt),
            IsDeleted = new IsDeleted(ruleSetEntity.IsDeleted),
            DeletedAt = ruleSetEntity.DeletedAt.HasValue ? new DeletedAt(ruleSetEntity.DeletedAt.Value) : null
        };
    }

    public static RuleSetEntity ToEntity(this RuleSet ruleSet)
    {
        return new RuleSetEntity
        {
            ModeId = (int)ruleSet.Mode,
            RuleSetId = ruleSet.RuleSetId,
            FriendlyName = ruleSet.FriendlyName.ToString(),
            OrganizationId = ruleSet.OrganizationId,
            SubOrganizationId = ruleSet.SubOrganizationId.HasValue ? (Guid)ruleSet.SubOrganizationId.Value : null,
            IsEnabled = ruleSet.IsEnabled,
            ReviewThreshold = ruleSet.ReviewThreshold,
            CreatedByUserId = ruleSet.CreatedByUserId,
            CreatedAt = ruleSet.CreatedAt,
            UpdatedAt = ruleSet.UpdatedAt,
            IsDeleted = ruleSet.IsDeleted,
            DeletedAt = ruleSet.DeletedAt
        };
    }
}