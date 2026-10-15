using AutoFixture;
using Core.Enums;
using Core.Models.Discounts;
using Core.ValueObjects;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.Discounts;

public static class FakeContractDiscount
{
    public static ContractDiscount CreateValid(IFixture fixture)
    {
        return new ContractDiscount
        {
            ContractDiscountId = FakeContractDiscountId.CreateValid(fixture),
            OrganizationId = FakeOrganizationId.CreateValid(fixture),
            DiscountType = DiscountType.Percentage,
            Value = new DiscountValue(10),
            Description = FakeContractDiscountDescription.CreateValid(fixture),
            EffectiveFrom = FakeEffectiveFrom.CreateValid(fixture),
            EffectiveTo = null,
            CreatedByUserId = FakeApplicationUserId.CreateValid(fixture),
            CreatedAt = FakeCreatedAt.CreateValid(fixture),
            UpdatedAt = FakeUpdatedAt.CreateValid(fixture),
            IsDeleted = new IsDeleted(false),
            DeletedAt = null
        };
    }
}
