using AutoFixture;
using Presentation.Models.Rules;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.Rules;

public static class FakeUpdateRuleRequestDto
{
    public static UpdateRuleRequestDto CreateValid(IFixture fixture)
    {
        return new UpdateRuleRequestDto
        {
            RuleId = fixture.Create<Guid>(),
            OrganizationId = fixture.Create<Guid>(),
            Body = new UpdateRuleRequestBodyDto
            {
                FriendlyName = fixture.Create<string>(),
                IsEnabled = fixture.Create<bool>()
            }
        };
    }

    public static UpdateRuleRequestDto CreateInvalid(IFixture fixture)
    {
        return new UpdateRuleRequestDto
        {
            RuleId = fixture.Create<Guid>(),
            OrganizationId = fixture.Create<Guid>(),
            Body = new UpdateRuleRequestBodyDto
            {
                FriendlyName = new string('A', 51),
                IsEnabled = fixture.Create<bool>()
            }
        };
    }
}
