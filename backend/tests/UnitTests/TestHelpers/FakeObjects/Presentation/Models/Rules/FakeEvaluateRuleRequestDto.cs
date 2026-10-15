using AutoFixture;
using Presentation.Models.Rules;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.Rules;

public static class FakeEvaluateRuleRequestDto
{
    public static EvaluateRuleRequestDto CreateValid(IFixture fixture)
    {
        return new EvaluateRuleRequestDto
        {
            RuleId = fixture.Create<Guid>(),
            OrganizationId = fixture.Create<Guid>(),
            Payload = FakeJsonObject.CreateValid(fixture),
            CreateCase = fixture.Create<bool>()
        };
    }
}