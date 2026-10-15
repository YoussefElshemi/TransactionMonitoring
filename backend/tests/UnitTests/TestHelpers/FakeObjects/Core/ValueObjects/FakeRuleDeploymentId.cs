using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeRuleDeploymentId
{
    public static RuleDeploymentId CreateValid(IFixture fixture)
    {
        return new RuleDeploymentId(fixture.Create<Guid>());
    }
}
