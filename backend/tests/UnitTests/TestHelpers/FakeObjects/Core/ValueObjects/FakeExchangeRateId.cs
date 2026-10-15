using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeExchangeRateId
{
    public static ExchangeRateId CreateValid(IFixture fixture)
    {
        return new ExchangeRateId(fixture.Create<Guid>());
    }
}
