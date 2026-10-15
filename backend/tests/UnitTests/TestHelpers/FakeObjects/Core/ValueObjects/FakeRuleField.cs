using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeRuleField
{
    public static RuleField CreateValid(IFixture fixture)
    {
        return new RuleField(fixture.Create<string>());
    }
}