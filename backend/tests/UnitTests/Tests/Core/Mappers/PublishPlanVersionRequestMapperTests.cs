using Core.Mappers;
using Core.ValueObjects;
using FluentAssertions;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Plans;

namespace UnitTests.Tests.Core.Mappers;

public class PublishPlanVersionRequestMapperTests : BaseTestClass
{
    [Fact]
    public void ToModel_GivenPublishPlanVersionRequest_MapsToPlanVersion()
    {
        // Arrange
        var request = FakePublishPlanVersionRequest.CreateValid(Fixture);
        var versionNumber = new VersionNumber(4);

        // Act
        var planVersion = request.ToModel(versionNumber, MockTimeProvider);

        // Assert
        planVersion.PlanId.Should().Be(request.PlanId);
        planVersion.VersionNumber.Should().Be(versionNumber);
        planVersion.CostPerRequest.Should().Be(request.CostPerRequest);
        planVersion.PublishedByUserId.Should().Be(request.PublishedByUserId);
        planVersion.CreatedAt.Should().Be(new CreatedAt(MockTimeProvider.GetUtcNow()));
    }

    [Fact]
    public void ToModel_GivenTheSameRequestTwice_MintsADistinctVersionId()
    {
        // Arrange
        var request = FakePublishPlanVersionRequest.CreateValid(Fixture);

        // Act
        var first = request.ToModel(new VersionNumber(2), MockTimeProvider);
        var second = request.ToModel(new VersionNumber(3), MockTimeProvider);

        // Assert
        first.PlanVersionId.Should().NotBe(second.PlanVersionId);
        ((Guid)first.PlanVersionId).Should().NotBeEmpty();
    }
}
