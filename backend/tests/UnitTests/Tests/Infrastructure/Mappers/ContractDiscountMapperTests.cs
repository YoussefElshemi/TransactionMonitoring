using Core.Enums;
using Core.ValueObjects;
using FluentAssertions;
using Infrastructure.Mappers;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Discounts;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

namespace UnitTests.Tests.Infrastructure.Mappers;

public class ContractDiscountMapperTests : BaseTestClass
{
    [Fact]
    public void ToModel_ContractDiscount()
    {
        // Arrange
        var contractDiscountEntity = FakeContractDiscountEntity.CreateValid(Fixture);

        // Act
        var contractDiscount = contractDiscountEntity.ToModel();

        // Assert
        contractDiscount.ContractDiscountId.Should().Be(new ContractDiscountId(contractDiscountEntity.ContractDiscountId));
        contractDiscount.OrganizationId.Should().Be(new OrganizationId(contractDiscountEntity.OrganizationId));
        contractDiscount.DiscountType.Should().Be((DiscountType)contractDiscountEntity.DiscountTypeId);
        contractDiscount.Value.Should().Be(new DiscountValue(contractDiscountEntity.Value));
        contractDiscount.Description.Should().Be(new ContractDiscountDescription(contractDiscountEntity.Description));
        contractDiscount.EffectiveFrom.Should().Be(new EffectiveFrom(contractDiscountEntity.EffectiveFrom));
        contractDiscount.EffectiveTo.Should().Be(new EffectiveTo(contractDiscountEntity.EffectiveTo!.Value));
        contractDiscount.CreatedByUserId.Should().Be(new ApplicationUserId(contractDiscountEntity.CreatedByUserId!.Value));
        contractDiscount.CreatedAt.Should().Be(new CreatedAt(contractDiscountEntity.CreatedAt));
        contractDiscount.UpdatedAt.Should().Be(new UpdatedAt(contractDiscountEntity.UpdatedAt));
        contractDiscount.IsDeleted.Should().Be(new IsDeleted(contractDiscountEntity.IsDeleted));
        contractDiscount.DeletedAt.Should().Be(new DeletedAt(contractDiscountEntity.DeletedAt!.Value));
    }

    [Fact]
    public void ToModel_OpenEndedDiscount_KeepsTheEndNull()
    {
        // Arrange
        var contractDiscountEntity = FakeContractDiscountEntity.CreateValid(Fixture) with { EffectiveTo = null };

        // Act
        var contractDiscount = contractDiscountEntity.ToModel();

        // Assert
        contractDiscount.EffectiveTo.Should().BeNull();
    }

    [Fact]
    public void ToEntity_ContractDiscount()
    {
        // Arrange
        var contractDiscount = FakeContractDiscount.CreateValid(Fixture);

        // Act
        var contractDiscountEntity = contractDiscount.ToEntity();

        // Assert
        contractDiscountEntity.ContractDiscountId.Should().Be(contractDiscount.ContractDiscountId);
        contractDiscountEntity.OrganizationId.Should().Be(contractDiscount.OrganizationId);
        contractDiscountEntity.DiscountTypeId.Should().Be((int)contractDiscount.DiscountType);
        contractDiscountEntity.Value.Should().Be(contractDiscount.Value);
        contractDiscountEntity.Description.Should().Be(contractDiscount.Description);
        contractDiscountEntity.EffectiveFrom.Should().Be(contractDiscount.EffectiveFrom);
        contractDiscountEntity.CreatedByUserId.Should().Be(contractDiscount.CreatedByUserId!.Value);
        contractDiscountEntity.CreatedAt.Should().Be(contractDiscount.CreatedAt);
        contractDiscountEntity.UpdatedAt.Should().Be(contractDiscount.UpdatedAt);
        contractDiscountEntity.IsDeleted.Should().Be(contractDiscount.IsDeleted);
    }
}
