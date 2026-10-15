using Core.ValueObjects;
using FluentAssertions;
using Presentation.Mappers.Plans;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.Plans;

namespace UnitTests.Tests.Presentation.Mappers.Plans;

public class UpdatePlanRequestMapperTests : BaseTestClass
{
    [Fact]
    public void ToModel_GivenUpdatePlanRequestDto_MapsToModel()
    {
        // Arrange
        var dto = FakeUpdatePlanRequestDto.CreateValid(Fixture);

        // Act
        var model = dto.ToModel();

        // Assert
        model.PlanId.Should().Be(new PlanId(dto.PlanId));
        model.Name.Should().Be(new PlanName(dto.Body.Name!));
        model.IsEnabled.Should().Be(new IsEnabled(dto.Body.IsEnabled!.Value));
        model.Description.IsSet.Should().BeTrue();
        model.Description.Value.Should().Be(new PlanDescription(dto.Body.Description!));
    }

    [Fact]
    public void ToModel_GivenOmittedFields_MapsToNull()
    {
        // Arrange
        var dto = FakeUpdatePlanRequestDto.CreateValid(Fixture);
        dto = dto with { Body = dto.Body with { Name = null, Description = null, IsEnabled = null } };

        // Act
        var model = dto.ToModel();

        // Assert
        model.Name.Should().BeNull();
        model.IsEnabled.Should().BeNull();
        model.Description.IsSet.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ToModel_GivenABlankDescription_ClearsIt(string description)
    {
        // Arrange
        var dto = FakeUpdatePlanRequestDto.CreateValid(Fixture);
        dto = dto with { Body = dto.Body with { Description = description } };

        // Act
        var model = dto.ToModel();

        // Assert
        model.Description.IsSet.Should().BeTrue();
        model.Description.Value.Should().BeNull();
    }

    [Fact]
    public void ToModel_GivenIsEnabledFalse_MapsIt()
    {
        // Arrange
        var dto = FakeUpdatePlanRequestDto.CreateValid(Fixture);
        dto = dto with { Body = dto.Body with { IsEnabled = false } };

        // Act
        var model = dto.ToModel();

        // Assert
        model.IsEnabled.Should().Be(new IsEnabled(false));
    }
}
