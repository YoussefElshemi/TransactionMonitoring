using FluentAssertions;
using Presentation.Mappers.Plans;
using Presentation.Mappers.Subscriptions;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Subscriptions;

namespace UnitTests.Tests.Presentation.Mappers.Subscriptions;

public class SubscriptionDtoMapperTests : BaseTestClass
{
    [Fact]
    public void ToDto_Subscription()
    {
        // Arrange
        var subscription = FakeSubscription.CreateValid(Fixture);

        // Act
        var dto = subscription.ToDto();

        // Assert
        dto.SubscriptionId.Should().Be(subscription.SubscriptionId);
        dto.OrganizationId.Should().Be(subscription.OrganizationId);
        dto.PlanVersionId.Should().Be(subscription.PlanVersionId);
        dto.PlanVersion.Should().BeEquivalentTo(subscription.PlanVersion!.ToDto());
        dto.PlanName.Should().Be(subscription.Plan!.Name.ToString());
        dto.EffectiveFrom.Should().Be(subscription.EffectiveFrom);
        dto.EffectiveTo.Should().Be(subscription.EffectiveTo!.Value);
        dto.CreatedByUserId.Should().Be(subscription.CreatedByUserId!.Value);
        dto.CreatedAt.Should().Be(subscription.CreatedAt);
        dto.UpdatedAt.Should().Be(subscription.UpdatedAt);
    }

    [Fact]
    public void ToDto_OpenSubscription_KeepsTheEndNull()
    {
        // Arrange
        var subscription = FakeSubscription.CreateValid(Fixture) with { EffectiveTo = null };

        // Act
        var dto = subscription.ToDto();

        // Assert
        dto.EffectiveTo.Should().BeNull();
    }
}
