using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeRuleVersionId
{
    public static RuleVersionId CreateValid(IFixture fixture)
    {
        return new RuleVersionId(fixture.Create<Guid>());
    }
}
