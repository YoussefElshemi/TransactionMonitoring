using Core.Mappers;
using Core.ValueObjects;
using FluentAssertions;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Subscriptions;

namespace UnitTests.Tests.Core.Mappers;

public class CreateSubscriptionRequestMapperTests : BaseTestClass
{
    [Fact]
    public void ToModel_GivenCreateSubscriptionRequest_MapsToSubscription()
    {
        // Arrange
        var request = FakeCreateSubscriptionRequest.CreateValid(Fixture);

        // Act
        var subscription = request.ToModel(MockTimeProvider);

        // Assert
        subscription.OrganizationId.Should().Be(request.OrganizationId);
        subscription.PlanVersionId.Should().Be(request.PlanVersionId);
        subscription.EffectiveFrom.Should().Be(request.EffectiveFrom!.Value);
        subscription.CreatedByUserId.Should().Be(request.CreatedByUserId);
        subscription.CreatedAt.Should().Be(new CreatedAt(MockTimeProvider.GetUtcNow()));
        subscription.UpdatedAt.Should().Be(new UpdatedAt(MockTimeProvider.GetUtcNow()));
        subscription.IsDeleted.Should().Be(new IsDeleted(false));
        subscription.DeletedAt.Should().BeNull();
        ((Guid)subscription.SubscriptionId).Should().NotBeEmpty();
    }

    [Fact]
    public void ToModel_GivenCreateSubscriptionRequest_OpensAnEndlessStretch()
    {
        // Arrange
        var request = FakeCreateSubscriptionRequest.CreateValid(Fixture);

        // Act
        var subscription = request.ToModel(MockTimeProvider);

        // Assert
        subscription.EffectiveTo.Should().BeNull();
    }

    [Fact]
    public void ToModel_GivenNoEffectiveFrom_StartsNow()
    {
        // Arrange
        var request = FakeCreateSubscriptionRequest.CreateValid(Fixture) with { EffectiveFrom = null };

        // Act
        var subscription = request.ToModel(MockTimeProvider);

        // Assert
        ((DateTimeOffset)subscription.EffectiveFrom).Should().Be(MockTimeProvider.GetUtcNow());
    }

    [Fact]
    public void ToModel_GivenCreateSubscriptionRequest_CarriesNoNavigations()
    {
        // Arrange
        var request = FakeCreateSubscriptionRequest.CreateValid(Fixture);

        // Act
        var subscription = request.ToModel(MockTimeProvider);

        // Assert
        subscription.PlanVersion.Should().BeNull();
        subscription.Plan.Should().BeNull();
    }
}
