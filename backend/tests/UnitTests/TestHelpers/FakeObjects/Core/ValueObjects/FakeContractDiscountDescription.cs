using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeContractDiscountDescription
{
    public static ContractDiscountDescription CreateValid(IFixture fixture)
    {
        return new ContractDiscountDescription(fixture.Create<string>());
    }
}
