using AutoFixture;
using Core.Enums;
using Core.ValueObjects;
using FluentAssertions;
using Infrastructure.Mappers;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

namespace UnitTests.Tests.Infrastructure.Mappers;

public class PlanPriceMapperTests : BaseTestClass
{
    [Fact]
    public void ToModel_PlanPrice()
    {
        // Arrange
        var planPriceEntity = FakePlanPriceEntity.CreateValid(Fixture);

        // Act
        var planPrice = planPriceEntity.ToModel();

        // Assert
        planPrice.PlanPriceId.Should().Be(new PlanPriceId(planPriceEntity.PlanPriceId));
        planPrice.PlanVersionId.Should().Be(new PlanVersionId(planPriceEntity.PlanVersionId));
        planPrice.BillableResource.Should().Be((BillableResource)planPriceEntity.BillableResourceId!.Value);
        planPrice.PricingModel.Should().Be((PricingModel)planPriceEntity.PricingModelId);
        planPrice.FreeAllowance.Should().Be(new FreeAllowance(planPriceEntity.FreeAllowance));
        planPrice.CreatedAt.Should().Be(new CreatedAt(planPriceEntity.CreatedAt));
        planPrice.Tiers.Should().HaveCount(planPriceEntity.Tiers.Count);
    }

    [Fact]
    public void ToModel_TheCatchAllPrice_KeepsTheResourceNull()
    {
        // Arrange
        var planPriceEntity = FakePlanPriceEntity.CreateValid(Fixture) with { BillableResourceId = null };

        // Act
        var planPrice = planPriceEntity.ToModel();

        // Assert
        planPrice.BillableResource.Should().BeNull();
    }

    [Fact]
    public void ToModel_OrdersTheBandsFromTheBottomUp()
    {
        // Arrange
        var planPriceId = Fixture.Create<Guid>();

        var planPriceEntity = FakePlanPriceEntity.CreateValid(Fixture) with
        {
            PlanPriceId = planPriceId,
            Tiers =
            [
                FakePlanPriceTierEntity.CreateValid(Fixture) with { PlanPriceId = planPriceId, FromQuantity = 5000, ToQuantity = null },
                FakePlanPriceTierEntity.CreateValid(Fixture) with { PlanPriceId = planPriceId, FromQuantity = 0, ToQuantity = 1000 },
                FakePlanPriceTierEntity.CreateValid(Fixture) with { PlanPriceId = planPriceId, FromQuantity = 1000, ToQuantity = 5000 }
            ]
        };

        // Act
        var planPrice = planPriceEntity.ToModel();

        // Assert
        planPrice.Tiers.Select(x => (int)x.FromQuantity).Should().ContainInOrder(0, 1000, 5000);
    }

    [Fact]
    public void ToEntity_PlanPrice()
    {
        // Arrange
        var planPrice = FakePlanPriceEntity.CreateValid(Fixture).ToModel();

        // Act
        var planPriceEntity = planPrice.ToEntity();

        // Assert
        planPriceEntity.PlanPriceId.Should().Be(planPrice.PlanPriceId);
        planPriceEntity.PlanVersionId.Should().Be(planPrice.PlanVersionId);
        planPriceEntity.BillableResourceId.Should().Be((int)planPrice.BillableResource!.Value);
        planPriceEntity.PricingModelId.Should().Be((int)planPrice.PricingModel);
        planPriceEntity.FreeAllowance.Should().Be(planPrice.FreeAllowance);
        planPriceEntity.CreatedAt.Should().Be(planPrice.CreatedAt);
    }

    [Fact]
    public void ToEntity_TheCatchAllPrice_KeepsTheResourceNull()
    {
        // Arrange
        var planPrice = (FakePlanPriceEntity.CreateValid(Fixture) with { BillableResourceId = null }).ToModel();

        // Act
        var planPriceEntity = planPrice.ToEntity();

        // Assert
        planPriceEntity.BillableResourceId.Should().BeNull();
    }
}
