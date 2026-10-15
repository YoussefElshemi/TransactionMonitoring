using AutoFixture;
using Presentation.Models.RuleSets;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.RuleSets;

public static class FakeCreateRuleSetRequestDto
{
    public static CreateRuleSetRequestDto CreateValid(IFixture fixture)
    {
        return new CreateRuleSetRequestDto
        {
            FriendlyName = fixture.Create<string>(),
            IsEnabled = fixture.Create<bool>()
        };
    }

    public static CreateRuleSetRequestDto CreateInvalid(IFixture fixture)
    {
        return new CreateRuleSetRequestDto
        {
            FriendlyName = new string('A', 51),
            IsEnabled = fixture.Create<bool>()
        };
    }
}