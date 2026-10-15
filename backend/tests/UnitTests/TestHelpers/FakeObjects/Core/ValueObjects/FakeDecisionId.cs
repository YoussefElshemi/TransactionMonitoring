using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeDecisionId
{
    public static DecisionId CreateValid(IFixture fixture)
    {
        return new DecisionId(fixture.Create<Guid>());
    }
}
