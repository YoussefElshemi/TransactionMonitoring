using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeSubscriptionId
{
    public static SubscriptionId CreateValid(IFixture fixture)
    {
        return new SubscriptionId(fixture.Create<Guid>());
    }
}
