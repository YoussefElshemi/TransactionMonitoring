using AutoFixture;
using Presentation.Models.RuleSets;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.RuleSets;

public static class FakeEvaluateRuleSetRequestDto
{
    public static EvaluateRuleSetRequestDto CreateValid(IFixture fixture)
    {
        return new EvaluateRuleSetRequestDto
        {
            RuleSetId = fixture.Create<Guid>(),
            OrganizationId = fixture.Create<Guid>(),
            Payload = FakeJsonObject.CreateValid(fixture)
        };
    }
}