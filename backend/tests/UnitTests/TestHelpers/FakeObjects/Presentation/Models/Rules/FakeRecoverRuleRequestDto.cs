using AutoFixture;
using Presentation.Models.Rules;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.Rules;

public static class FakeRecoverRuleRequestDto
{
    public static RecoverRuleRequestDto CreateValid(IFixture fixture)
    {
        return new RecoverRuleRequestDto
        {
            RuleId = fixture.Create<Guid>(),
            OrganizationId = fixture.Create<Guid>()
        };
    }
}