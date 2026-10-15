using Core.ValueObjects;
using FluentAssertions;
using Presentation.Mappers.Plans;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.Plans;

namespace UnitTests.Tests.Presentation.Mappers.Plans;

public class CreatePlanRequestMapperTests : BaseTestClass
{
    [Fact]
    public void ToModel_GivenCreatePlanRequestDto_MapsToModel()
    {
        // Arrange
        var dto = FakeCreatePlanRequestDto.CreateValid(Fixture);

        // Act
        var model = dto.ToModel();

        // Assert
        model.Name.Should().Be(new PlanName(dto.Body.Name));
        model.Description.Should().Be(new PlanDescription(dto.Body.Description!));
        model.OrganizationId.Should().Be(new OrganizationId(dto.Body.OrganizationId!.Value));
        model.CostPerRequest.Should().Be(new CostPerRequest(dto.Body.CostPerRequest));
        model.CreatedByUserId.Should().Be(new ApplicationUserId(dto.CreatedByUserId!.Value));
    }

    [Fact]
    public void ToModel_GivenNoOrganizationId_MapsToNull()
    {
        // Arrange
        var dto = FakeCreatePlanRequestDto.CreateValid(Fixture);
        dto = dto with { Body = dto.Body with { OrganizationId = null } };

        // Act
        var model = dto.ToModel();

        // Assert
        model.OrganizationId.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ToModel_GivenNoDescription_MapsToNull(string? description)
    {
        // Arrange
        var dto = FakeCreatePlanRequestDto.CreateValid(Fixture);
        dto = dto with { Body = dto.Body with { Description = description } };

        // Act
        var model = dto.ToModel();

        // Assert
        model.Description.Should().BeNull();
    }

    [Fact]
    public void ToModel_GivenNoCreatedByUserId_MapsToNull()
    {
        // Arrange
        var dto = FakeCreatePlanRequestDto.CreateValid(Fixture) with { CreatedByUserId = null };

        // Act
        var model = dto.ToModel();

        // Assert
        model.CreatedByUserId.Should().BeNull();
    }
}
