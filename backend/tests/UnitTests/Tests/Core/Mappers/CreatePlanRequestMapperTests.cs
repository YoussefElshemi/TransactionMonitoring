using Core.Mappers;
using Core.ValueObjects;
using FluentAssertions;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Plans;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.Tests.Core.Mappers;

public class CreatePlanRequestMapperTests : BaseTestClass
{
    [Fact]
    public void ToModel_GivenCreatePlanRequest_MapsToPlan()
    {
        // Arrange
        var request = FakeCreatePlanRequest.CreateValid(Fixture);
        var planId = FakePlanId.CreateValid(Fixture);

        // Act
        var plan = request.ToModel(planId, MockTimeProvider);

        // Assert
        plan.PlanId.Should().Be(planId);
        plan.Name.Should().Be(request.Name);
        plan.Description.Should().Be(request.Description);
        plan.OrganizationId.Should().Be(request.OrganizationId);
        plan.CurrentVersion.Should().BeNull();
        plan.CreatedAt.Should().Be(new CreatedAt(MockTimeProvider.GetUtcNow()));
        plan.UpdatedAt.Should().Be(new UpdatedAt(MockTimeProvider.GetUtcNow()));
        plan.IsDeleted.Should().Be(new IsDeleted(false));
        plan.DeletedAt.Should().BeNull();
    }

    [Fact]
    public void ToModel_GivenCreatePlanRequest_IsEnabled()
    {
        // Arrange
        var request = FakeCreatePlanRequest.CreateValid(Fixture);

        // Act
        var plan = request.ToModel(FakePlanId.CreateValid(Fixture), MockTimeProvider);

        // Assert
        plan.IsEnabled.Should().Be(new IsEnabled(true));
    }

    [Fact]
    public void ToModel_GivenCreatePlanRequest_PointsAtVersionOne()
    {
        // Arrange
        var request = FakeCreatePlanRequest.CreateValid(Fixture);

        // Act
        var plan = request.ToModel(FakePlanId.CreateValid(Fixture), MockTimeProvider);

        // Assert
        plan.CurrentVersionNumber.Should().Be(new VersionNumber(1));
    }

    [Fact]
    public void ToVersionModel_GivenCreatePlanRequest_MapsToVersionOneOfThatPlan()
    {
        // Arrange
        var request = FakeCreatePlanRequest.CreateValid(Fixture);
        var planId = FakePlanId.CreateValid(Fixture);

        // Act
        var planVersion = request.ToVersionModel(planId, MockTimeProvider);

        // Assert
        planVersion.PlanId.Should().Be(planId);
        planVersion.VersionNumber.Should().Be(new VersionNumber(1));
        planVersion.CostPerRequest.Should().Be(request.CostPerRequest);
        planVersion.PublishedByUserId.Should().Be(request.CreatedByUserId);
        planVersion.CreatedAt.Should().Be(new CreatedAt(MockTimeProvider.GetUtcNow()));
        ((Guid)planVersion.PlanVersionId).Should().NotBeEmpty();
    }

    [Fact]
    public void ToModel_GivenNoOwner_MapsToACataloguePlan()
    {
        // Arrange
        var request = FakeCreatePlanRequest.CreateValid(Fixture) with { OrganizationId = null };

        // Act
        var plan = request.ToModel(FakePlanId.CreateValid(Fixture), MockTimeProvider);

        // Assert
        plan.OrganizationId.Should().BeNull();
    }

    [Fact]
    public void ToModel_GivenNoDescription_MapsToNull()
    {
        // Arrange
        var request = FakeCreatePlanRequest.CreateValid(Fixture) with { Description = null };

        // Act
        var plan = request.ToModel(FakePlanId.CreateValid(Fixture), MockTimeProvider);

        // Assert
        plan.Description.Should().BeNull();
    }
}
