using AutoFixture;
using Presentation.Models.RuleSets;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.RuleSets;

public static class FakeUpdateRuleSetRequestDto
{
    public static UpdateRuleSetRequestDto CreateValid(IFixture fixture)
    {
        return new UpdateRuleSetRequestDto
        {
            RuleSetId = fixture.Create<Guid>(),
            OrganizationId = fixture.Create<Guid>(),
            Body = new UpdateRuleSetRequestBodyDto
            {
                FriendlyName = fixture.Create<string>(),
                IsEnabled = fixture.Create<bool>()
            }
        };
    }

    public static UpdateRuleSetRequestDto CreateInvalid(IFixture fixture)
    {
        return new UpdateRuleSetRequestDto
        {
            RuleSetId = fixture.Create<Guid>(),
            OrganizationId = fixture.Create<Guid>(),
            Body = new UpdateRuleSetRequestBodyDto
            {
                FriendlyName = new string('A', 51),
                IsEnabled = fixture.Create<bool>()
            }
        };
    }
}