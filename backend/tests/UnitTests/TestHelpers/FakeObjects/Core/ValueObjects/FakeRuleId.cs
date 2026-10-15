using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeRuleId
{
    public static RuleId CreateValid(IFixture fixture)
    {
        return new RuleId(fixture.Create<Guid>());
    }
}