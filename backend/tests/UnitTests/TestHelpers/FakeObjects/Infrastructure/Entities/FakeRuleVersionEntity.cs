using AutoFixture;
using Core.Enums;
using Infrastructure.Entities;
using Infrastructure.Mappers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Rules;

namespace UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

public static class FakeRuleVersionEntity
{
    public static RuleVersionEntity CreateValid(IFixture fixture)
    {
        return new RuleVersionEntity
        {
            ModeId = (int)Mode.Live,
            RuleVersionId = fixture.Create<Guid>(),
            RuleId = fixture.Create<Guid>(),
            VersionNumber = fixture.Create<int>(),
            RuleBody = FakeRuleBody.CreateValid(fixture).ToBase64String(),
            MatchPolarityId = (int)RuleMatchPolarity.MatchesWhenTrue,
            ActionId = (int)RuleAction.Review,
            SeverityId = (int)RuleSeverity.Medium,
            RiskWeight = fixture.Create<int>() % 101,
            RuleSetId = fixture.Create<Guid>(),
            OrganizationId = fixture.Create<Guid>(),
            CreatedByUserId = fixture.Create<Guid>(),
            CreatedAt = fixture.Create<DateTimeOffset>()
        };
    }
}
