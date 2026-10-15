using Core.ValueObjects;
using FluentAssertions;
using Infrastructure.Mappers;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Subscriptions;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

namespace UnitTests.Tests.Infrastructure.Mappers;

public class SubscriptionMapperTests : BaseTestClass
{
    [Fact]
    public void ToModel_Subscription()
    {
        // Arrange
        var subscriptionEntity = FakeSubscriptionEntity.CreateValid(Fixture);

        // Act
        var subscription = subscriptionEntity.ToModel();

        // Assert
        subscription.SubscriptionId.Should().Be(new SubscriptionId(subscriptionEntity.SubscriptionId));
        subscription.OrganizationId.Should().Be(new OrganizationId(subscriptionEntity.OrganizationId));
        subscription.PlanVersionId.Should().Be(new PlanVersionId(subscriptionEntity.PlanVersionId));
        subscription.PlanVersion.Should().BeEquivalentTo(subscriptionEntity.PlanVersion.ToModel());
        subscription.EffectiveFrom.Should().Be(new EffectiveFrom(subscriptionEntity.EffectiveFrom));
        subscription.EffectiveTo.Should().Be(new EffectiveTo(subscriptionEntity.EffectiveTo!.Value));
        subscription.CreatedByUserId.Should().Be(new ApplicationUserId(subscriptionEntity.CreatedByUserId!.Value));
        subscription.CreatedAt.Should().Be(new CreatedAt(subscriptionEntity.CreatedAt));
        subscription.UpdatedAt.Should().Be(new UpdatedAt(subscriptionEntity.UpdatedAt));
        subscription.IsDeleted.Should().Be(new IsDeleted(subscriptionEntity.IsDeleted));
        subscription.DeletedAt.Should().Be(new DeletedAt(subscriptionEntity.DeletedAt!.Value));
    }

    [Fact]
    public void ToModel_OpenSubscription_KeepsTheEndNull()
    {
        // Arrange
        var subscriptionEntity = FakeSubscriptionEntity.CreateValid(Fixture) with { EffectiveTo = null };

        // Act
        var subscription = subscriptionEntity.ToModel();

        // Assert
        subscription.EffectiveTo.Should().BeNull();
    }

    [Fact]
    public void ToEntity_Subscription()
    {
        // Arrange
        var subscription = FakeSubscription.CreateValid(Fixture);

        // Act
        var subscriptionEntity = subscription.ToEntity();

        // Assert
        subscriptionEntity.SubscriptionId.Should().Be(subscription.SubscriptionId);
        subscriptionEntity.OrganizationId.Should().Be(subscription.OrganizationId);
        subscriptionEntity.PlanVersionId.Should().Be(subscription.PlanVersionId);
        subscriptionEntity.EffectiveFrom.Should().Be(subscription.EffectiveFrom);
        subscriptionEntity.EffectiveTo.Should().Be(subscription.EffectiveTo!.Value);
        subscriptionEntity.CreatedByUserId.Should().Be(subscription.CreatedByUserId!.Value);
        subscriptionEntity.CreatedAt.Should().Be(subscription.CreatedAt);
        subscriptionEntity.UpdatedAt.Should().Be(subscription.UpdatedAt);
        subscriptionEntity.IsDeleted.Should().Be(subscription.IsDeleted);
        subscriptionEntity.DeletedAt.Should().Be(subscription.DeletedAt!.Value);
    }
}
