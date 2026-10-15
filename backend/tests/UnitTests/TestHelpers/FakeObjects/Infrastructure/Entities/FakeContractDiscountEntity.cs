using AutoFixture;
using Core.Enums;
using Infrastructure.Entities;

namespace UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

public static class FakeContractDiscountEntity
{
    public static ContractDiscountEntity CreateValid(IFixture fixture)
    {
        var effectiveFrom = fixture.Create<DateTimeOffset>();

        return new ContractDiscountEntity
        {
            ContractDiscountId = fixture.Create<Guid>(),
            OrganizationId = fixture.Create<Guid>(),
            DiscountTypeId = (int)DiscountType.Percentage,
            Value = 10m,
            Description = fixture.Create<string>(),
            EffectiveFrom = effectiveFrom,
            EffectiveTo = effectiveFrom.AddYears(1),
            CreatedByUserId = fixture.Create<Guid>(),
            CreatedAt = fixture.Create<DateTimeOffset>(),
            UpdatedAt = fixture.Create<DateTimeOffset>(),
            IsDeleted = fixture.Create<bool>(),
            DeletedAt = fixture.Create<DateTimeOffset>()
        };
    }
}
