using AutoFixture;
using Core.Enums;
using Presentation.Models.Rules;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.Rules;

public static class FakeCreateRuleVersionRequestDto
{
    public static CreateRuleVersionRequestDto CreateValid(IFixture fixture)
    {
        return new CreateRuleVersionRequestDto
        {
            RuleId = fixture.Create<Guid>(),
            OrganizationId = fixture.Create<Guid>(),
            Body = new CreateRuleVersionRequestBodyDto
            {
                RuleBody = FakeRuleBodyDto.CreateBasicRule(fixture)
            }
        };
    }

    public static CreateRuleVersionRequestDto CreateInvalid(IFixture fixture)
    {
        return new CreateRuleVersionRequestDto
        {
            RuleId = fixture.Create<Guid>(),
            OrganizationId = fixture.Create<Guid>(),
            Body = new CreateRuleVersionRequestBodyDto
            {
                RuleBody = FakeRuleBodyDto.CreateBasicRule(fixture) with
                {
                    Condition = RuleCondition.IsNull.ToString()
                }
            }
        };
    }
}
