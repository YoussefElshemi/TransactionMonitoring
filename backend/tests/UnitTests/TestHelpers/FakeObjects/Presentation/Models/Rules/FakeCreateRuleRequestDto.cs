using AutoFixture;
using Presentation.Models.Rules;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.Rules;

public static class FakeCreateRuleRequestDto
{
    public static CreateRuleRequestDto CreateValid(IFixture fixture)
    {
        return new CreateRuleRequestDto
        {
            RuleSetId = fixture.Create<Guid>(),
            OrganizationId = fixture.Create<Guid>(),
            Body = new CreateRuleRequestBodyDto
            {
                FriendlyName = fixture.Create<string>(),
                RuleBody = FakeRuleBodyDto.CreateBasicRule(fixture),
                IsEnabled = fixture.Create<bool>()
            }
        };
    }

    public static CreateRuleRequestDto CreateInvalid(IFixture fixture)
    {
        return new CreateRuleRequestDto
        {
            RuleSetId = fixture.Create<Guid>(),
            OrganizationId = fixture.Create<Guid>(),
            Body = new CreateRuleRequestBodyDto
            {
                FriendlyName = new string('A', 51),
                RuleBody = FakeRuleBodyDto.CreateBasicRule(fixture),
                IsEnabled = fixture.Create<bool>()
            }
        };
    }
}