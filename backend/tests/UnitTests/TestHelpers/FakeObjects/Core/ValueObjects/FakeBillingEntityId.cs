using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeBillingEntityId
{
    public static BillingEntityId CreateValid(IFixture fixture)
    {
        return new BillingEntityId(fixture.Create<Guid>());
    }
}
