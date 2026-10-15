using Core.ValueObjects;
using FluentAssertions;
using Presentation.Mappers.Subscriptions;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.Subscriptions;

namespace UnitTests.Tests.Presentation.Mappers.Subscriptions;

public class CreateSubscriptionRequestMapperTests : BaseTestClass
{
    [Fact]
    public void ToModel_GivenCreateSubscriptionRequestDto_MapsToModel()
    {
        // Arrange
        var dto = FakeCreateSubscriptionRequestDto.CreateValid(Fixture);
        dto = dto with
        {
            Body = dto.Body with { EffectiveFrom = MockTimeProvider.GetUtcNow().AddMonths(1) }
        };

        // Act
        var model = dto.ToModel();

        // Assert
        model.OrganizationId.Should().Be(new OrganizationId(dto.OrganizationId));
        model.PlanVersionId.Should().Be(new PlanVersionId(dto.Body.PlanVersionId));
        model.EffectiveFrom.Should().Be(new EffectiveFrom(dto.Body.EffectiveFrom!.Value));
        model.CreatedByUserId.Should().Be(new ApplicationUserId(dto.CreatedByUserId!.Value));
    }

    [Fact]
    public void ToModel_GivenNoEffectiveFrom_MapsToNull()
    {
        // Arrange
        var dto = FakeCreateSubscriptionRequestDto.CreateValid(Fixture);
        dto = dto with { Body = dto.Body with { EffectiveFrom = null } };

        // Act
        var model = dto.ToModel();

        // Assert
        model.EffectiveFrom.Should().BeNull();
    }

    [Fact]
    public void ToModel_GivenNoCreatedByUserId_MapsToNull()
    {
        // Arrange
        var dto = FakeCreateSubscriptionRequestDto.CreateValid(Fixture) with { CreatedByUserId = null };

        // Act
        var model = dto.ToModel();

        // Assert
        model.CreatedByUserId.Should().BeNull();
    }
}
