using Core.ValueObjects;
using FluentAssertions;
using Infrastructure.Mappers;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Plans;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

namespace UnitTests.Tests.Infrastructure.Mappers;

public class PlanVersionMapperTests : BaseTestClass
{
    [Fact]
    public void ToModel_PlanVersion()
    {
        // Arrange
        var planVersionEntity = FakePlanVersionEntity.CreateValid(Fixture);

        // Act
        var planVersion = planVersionEntity.ToModel();

        // Assert
        planVersion.PlanVersionId.Should().Be(new PlanVersionId(planVersionEntity.PlanVersionId));
        planVersion.PlanId.Should().Be(new PlanId(planVersionEntity.PlanId));
        planVersion.VersionNumber.Should().Be(new VersionNumber(planVersionEntity.VersionNumber));
        planVersion.CostPerRequest.Should().Be(new CostPerRequest(planVersionEntity.CostPerRequest));
        planVersion.PublishedByUserId.Should().Be(new ApplicationUserId(planVersionEntity.PublishedByUserId!.Value));
        planVersion.CreatedAt.Should().Be(new CreatedAt(planVersionEntity.CreatedAt));
    }

    [Fact]
    public void ToEntity_PlanVersion()
    {
        // Arrange
        var planVersion = FakePlanVersion.CreateValid(Fixture);

        // Act
        var planVersionEntity = planVersion.ToEntity();

        // Assert
        planVersionEntity.PlanVersionId.Should().Be(planVersion.PlanVersionId);
        planVersionEntity.PlanId.Should().Be(planVersion.PlanId);
        planVersionEntity.VersionNumber.Should().Be(planVersion.VersionNumber);
        planVersionEntity.CostPerRequest.Should().Be(planVersion.CostPerRequest);
        planVersionEntity.PublishedByUserId.Should().Be(planVersion.PublishedByUserId!.Value);
        planVersionEntity.CreatedAt.Should().Be(planVersion.CreatedAt);
    }
}
