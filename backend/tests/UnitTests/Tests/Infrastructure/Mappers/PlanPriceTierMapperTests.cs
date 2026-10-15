using Core.ValueObjects;
using FluentAssertions;
using Infrastructure.Mappers;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

namespace UnitTests.Tests.Infrastructure.Mappers;

public class PlanPriceTierMapperTests : BaseTestClass
{
    [Fact]
    public void ToModel_PlanPriceTier()
    {
        // Arrange
        var planPriceTierEntity = FakePlanPriceTierEntity.CreateValid(Fixture);

        // Act
        var planPriceTier = planPriceTierEntity.ToModel();

        // Assert
        planPriceTier.PlanPriceTierId.Should().Be(new PlanPriceTierId(planPriceTierEntity.PlanPriceTierId));
        planPriceTier.PlanPriceId.Should().Be(new PlanPriceId(planPriceTierEntity.PlanPriceId));
        planPriceTier.FromQuantity.Should().Be(new FromQuantity(planPriceTierEntity.FromQuantity));
        planPriceTier.ToQuantity.Should().Be(new ToQuantity(planPriceTierEntity.ToQuantity!.Value));
        planPriceTier.UnitAmount.Should().Be(new UnitAmount(planPriceTierEntity.UnitAmount));
        planPriceTier.FlatAmount.Should().Be(new FlatAmount(planPriceTierEntity.FlatAmount));
        planPriceTier.CreatedAt.Should().Be(new CreatedAt(planPriceTierEntity.CreatedAt));
    }

    [Fact]
    public void ToModel_TheTopBand_KeepsItsEndNull()
    {
        // Arrange
        var planPriceTierEntity = FakePlanPriceTierEntity.CreateValid(Fixture) with { ToQuantity = null };

        // Act
        var planPriceTier = planPriceTierEntity.ToModel();

        // Assert
        planPriceTier.ToQuantity.Should().BeNull();
    }

    [Fact]
    public void ToEntity_PlanPriceTier()
    {
        // Arrange
        var planPriceTier = FakePlanPriceTierEntity.CreateValid(Fixture).ToModel();

        // Act
        var planPriceTierEntity = planPriceTier.ToEntity();

        // Assert
        planPriceTierEntity.PlanPriceTierId.Should().Be(planPriceTier.PlanPriceTierId);
        planPriceTierEntity.PlanPriceId.Should().Be(planPriceTier.PlanPriceId);
        planPriceTierEntity.FromQuantity.Should().Be(planPriceTier.FromQuantity);
        planPriceTierEntity.ToQuantity.Should().Be(planPriceTier.ToQuantity!.Value);
        planPriceTierEntity.UnitAmount.Should().Be(planPriceTier.UnitAmount);
        planPriceTierEntity.FlatAmount.Should().Be(planPriceTier.FlatAmount);
        planPriceTierEntity.CreatedAt.Should().Be(planPriceTier.CreatedAt);
    }

    [Fact]
    public void ToEntity_TheTopBand_KeepsItsEndNull()
    {
        // Arrange
        var planPriceTier = (FakePlanPriceTierEntity.CreateValid(Fixture) with { ToQuantity = null }).ToModel();

        // Act
        var planPriceTierEntity = planPriceTier.ToEntity();

        // Assert
        planPriceTierEntity.ToQuantity.Should().BeNull();
    }
}
