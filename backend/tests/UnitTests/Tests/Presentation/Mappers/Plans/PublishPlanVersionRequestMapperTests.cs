using Core.ValueObjects;
using FluentAssertions;
using Presentation.Mappers.Plans;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.Plans;

namespace UnitTests.Tests.Presentation.Mappers.Plans;

public class PublishPlanVersionRequestMapperTests : BaseTestClass
{
    [Fact]
    public void ToModel_GivenPublishPlanVersionRequestDto_MapsToModel()
    {
        // Arrange
        var dto = FakePublishPlanVersionRequestDto.CreateValid(Fixture);

        // Act
        var model = dto.ToModel();

        // Assert
        model.PlanId.Should().Be(new PlanId(dto.PlanId));
        model.CostPerRequest.Should().Be(new CostPerRequest(dto.Body.CostPerRequest));
        model.PublishedByUserId.Should().Be(new ApplicationUserId(dto.PublishedByUserId!.Value));
    }

    [Fact]
    public void ToModel_GivenNoPublishedByUserId_MapsToNull()
    {
        // Arrange
        var dto = FakePublishPlanVersionRequestDto.CreateValid(Fixture) with { PublishedByUserId = null };

        // Act
        var model = dto.ToModel();

        // Assert
        model.PublishedByUserId.Should().BeNull();
    }
}
