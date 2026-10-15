using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeRuleValue
{
    public static RuleValue CreateValid(IFixture fixture)
    {
        return new RuleValue(fixture.Create<string>());
    }
}