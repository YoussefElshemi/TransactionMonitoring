using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeRuleSetId
{
    public static RuleSetId CreateValid(IFixture fixture)
    {
        return new RuleSetId(fixture.Create<Guid>());
    }
}