using AutoFixture;
using Presentation.Models.Rules;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.Rules;

public static class FakeTestRuleRequestDto
{
    public static TestRuleRequestDto CreateValid(IFixture fixture)
    {
        return new TestRuleRequestDto
        {
            RuleBody = FakeRuleBodyDto.CreateBasicRule(fixture),
            Payload = FakeJsonObject.CreateValid(fixture)
        };
    }
}