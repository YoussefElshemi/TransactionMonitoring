using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeBillingProfileId
{
    public static BillingProfileId CreateValid(IFixture fixture)
    {
        return new BillingProfileId(fixture.Create<Guid>());
    }
}
