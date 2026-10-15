using Core.Mappers;
using Core.ValueObjects;
using FluentAssertions;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Discounts;

namespace UnitTests.Tests.Core.Mappers;

public class CreateContractDiscountRequestMapperTests : BaseTestClass
{
    [Fact]
    public void ToModel_CreateContractDiscountRequest()
    {
        // Arrange
        var request = FakeCreateContractDiscountRequest.CreateValid(Fixture);
        var now = MockTimeProvider.GetUtcNow();

        // Act
        var contractDiscount = request.ToModel(MockTimeProvider);

        // Assert
        contractDiscount.ContractDiscountId.Should().NotBe(new ContractDiscountId(Guid.Empty));
        contractDiscount.OrganizationId.Should().Be(request.OrganizationId);
        contractDiscount.DiscountType.Should().Be(request.DiscountType);
        contractDiscount.Value.Should().Be(request.Value);
        contractDiscount.Description.Should().Be(request.Description);
        contractDiscount.CreatedByUserId.Should().Be(request.CreatedByUserId);
        contractDiscount.CreatedAt.Should().Be(new CreatedAt(now));
        contractDiscount.UpdatedAt.Should().Be(new UpdatedAt(now));
        contractDiscount.IsDeleted.Should().Be(new IsDeleted(false));
        contractDiscount.DeletedAt.Should().BeNull();
    }

    [Fact]
    public void ToModel_GivenNoStartDate_StartsItNow()
    {
        // Arrange
        var request = FakeCreateContractDiscountRequest.CreateValid(Fixture) with { EffectiveFrom = null };

        // Act
        var contractDiscount = request.ToModel(MockTimeProvider);

        // Assert
        contractDiscount.EffectiveFrom.Should().Be(new EffectiveFrom(MockTimeProvider.GetUtcNow()));
    }

    [Fact]
    public void ToModel_GivenAStartDate_KeepsIt()
    {
        // Arrange
        var effectiveFrom = new EffectiveFrom(MockTimeProvider.GetUtcNow().AddMonths(1));
        var request = FakeCreateContractDiscountRequest.CreateValid(Fixture) with { EffectiveFrom = effectiveFrom };

        // Act
        var contractDiscount = request.ToModel(MockTimeProvider);

        // Assert
        contractDiscount.EffectiveFrom.Should().Be(effectiveFrom);
    }

    [Fact]
    public void ToModel_GivenNoEndDate_LeavesItOpen()
    {
        // Arrange
        var request = FakeCreateContractDiscountRequest.CreateValid(Fixture) with { EffectiveTo = null };

        // Act
        var contractDiscount = request.ToModel(MockTimeProvider);

        // Assert
        contractDiscount.EffectiveTo.Should().BeNull();
    }
}
