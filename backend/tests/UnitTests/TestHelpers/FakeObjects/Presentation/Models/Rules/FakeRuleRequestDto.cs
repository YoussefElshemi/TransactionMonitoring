using AutoFixture;
using Presentation.Models.Rules;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.Rules;

public static class FakeRuleRequestDto
{
    public static RuleRequestDto CreateValid(IFixture fixture)
    {
        return new RuleRequestDto
        {
            RuleId = fixture.Create<Guid>(),
            OrganizationId = fixture.Create<Guid>()
        };
    }
}