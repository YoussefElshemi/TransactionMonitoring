using AutoFixture;
using Core.Enums;
using Infrastructure.Entities;
using Infrastructure.Mappers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Rules;

namespace UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

public static class FakeRuleEntity
{
    public static RuleEntity CreateValid(IFixture fixture)
    {
        var ruleEntity = new RuleEntity
        {
            ModeId = (int)Mode.Live,
            CreatedAt = fixture.Create<DateTimeOffset>(),
            UpdatedAt = fixture.Create<DateTimeOffset>(),
            IsDeleted = fixture.Create<bool>(),
            DeletedAt = fixture.Create<DateTimeOffset>(),
            RuleId = fixture.Create<Guid>(),
            RuleSetId = fixture.Create<Guid>(),
            FriendlyName = fixture.Create<string>(),
            RuleBody = FakeRuleBody.CreateValid(fixture).ToBase64String(),
            MatchPolarityId = (int)RuleMatchPolarity.MatchesWhenTrue,
            ActionId = (int)RuleAction.Review,
            SeverityId = (int)RuleSeverity.Medium,
            RiskWeight = fixture.Create<int>() % 101,
            HoldWindow = null,
            HoldExpiryVerdictId = null,
            AlertSubjectRoleId = null,
            DeduplicationWindow = null,
            IsEnabled = fixture.Create<bool>(),
            OrganizationId = fixture.Create<Guid>(),
            Organization = FakeOrganizationEntity.CreateValid(fixture),
            CreatedByUserId = fixture.Create<Guid>(),
            CreatedByUser = FakeApplicationUserEntity.CreateValid(fixture),
            CurrentRuleVersionId = fixture.Create<Guid>(),
            CurrentVersionNumber = fixture.Create<int>(),
            HasActiveDeployment = fixture.Create<bool>()
        };

        return ruleEntity with
        {
            RuleVersions =
            [
                new RuleVersionEntity
                {
                    ModeId = (int)Mode.Live,
                    RuleVersionId = ruleEntity.CurrentRuleVersionId,
                    RuleId = ruleEntity.RuleId,
                    VersionNumber = ruleEntity.CurrentVersionNumber,
                    RuleBody = ruleEntity.RuleBody,
                    MatchPolarityId = ruleEntity.MatchPolarityId,
                    ActionId = ruleEntity.ActionId,
                    SeverityId = ruleEntity.SeverityId,
                    RiskWeight = ruleEntity.RiskWeight,
                    RuleSetId = ruleEntity.RuleSetId,
                    OrganizationId = ruleEntity.OrganizationId,
                    CreatedByUserId = ruleEntity.CreatedByUserId,
                    CreatedAt = ruleEntity.CreatedAt
                }
            ]
        };
    }
}
