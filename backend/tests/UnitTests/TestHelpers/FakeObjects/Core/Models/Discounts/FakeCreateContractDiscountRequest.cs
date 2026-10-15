using AutoFixture;
using Core.Enums;
using Core.Models.Discounts;
using Core.ValueObjects;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.Discounts;

public static class FakeCreateContractDiscountRequest
{
    public static CreateContractDiscountRequest CreateValid(IFixture fixture)
    {
        return new CreateContractDiscountRequest
        {
            OrganizationId = FakeOrganizationId.CreateValid(fixture),
            DiscountType = DiscountType.Percentage,
            Value = new DiscountValue(10),
            Description = FakeContractDiscountDescription.CreateValid(fixture),
            EffectiveFrom = null,
            EffectiveTo = null,
            CreatedByUserId = FakeApplicationUserId.CreateValid(fixture)
        };
    }
}
