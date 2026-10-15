using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeContractDiscountId
{
    public static ContractDiscountId CreateValid(IFixture fixture)
    {
        return new ContractDiscountId(fixture.Create<Guid>());
    }
}
