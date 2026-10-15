using AutoFixture;
using Presentation.Models.RuleSets;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.RuleSets;

public static class FakeRecoverRuleSetRequestDto
{
    public static RecoverRuleSetRequestDto CreateValid(IFixture fixture)
    {
        return new RecoverRuleSetRequestDto
        {
            RuleSetId = fixture.Create<Guid>(),
            OrganizationId = fixture.Create<Guid>()
        };
    }
}