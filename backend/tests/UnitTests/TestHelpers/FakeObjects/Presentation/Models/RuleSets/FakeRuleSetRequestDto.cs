using AutoFixture;
using Presentation.Models.RuleSets;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.RuleSets;

public static class FakeRuleSetRequestDto
{
    public static RuleSetRequestDto CreateValid(IFixture fixture)
    {
        return new RuleSetRequestDto
        {
            RuleSetId = fixture.Create<Guid>(),
            OrganizationId = fixture.Create<Guid>()
        };
    }
}