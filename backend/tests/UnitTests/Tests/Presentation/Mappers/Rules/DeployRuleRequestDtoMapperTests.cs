using Core.Enums;
using Core.ValueObjects;
using FluentAssertions;
using Presentation.Mappers.Rules;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.Rules;

namespace UnitTests.Tests.Presentation.Mappers.Rules;

public class DeployRuleRequestDtoMapperTests : BaseTestClass
{
    [Fact]
    public void ToModel_Gradual_MapsAllFields()
    {
        // Arrange
        var dto = FakeDeployRuleRequestDto.CreateValidGradual(Fixture);
        var deployedByUserId = FakeApplicationUserId.CreateValid(Fixture);

        // Act
        var request = dto.ToModel(deployedByUserId);

        // Assert
        request.RuleId.Should().Be(new RuleId(dto.RuleId));
        request.VersionNumber.Should().Be(new VersionNumber(dto.Body.VersionNumber));
        request.Strategy.Should().Be(DeploymentStrategy.Gradual);
        request.StepPercentage.Should().Be(new StepPercentage(dto.Body.StepPercentage!.Value));
        request.StepIntervalSeconds.Should().Be(new StepIntervalSeconds(dto.Body.StepIntervalSeconds!.Value));
        request.DeployedByUserId.Should().Be(deployedByUserId);
    }

    [Fact]
    public void ToModel_AllAtOnce_LeavesStepFieldsNull()
    {
        // Arrange
        var dto = FakeDeployRuleRequestDto.CreateValid(Fixture);
        var deployedByUserId = FakeApplicationUserId.CreateValid(Fixture);

        // Act
        var request = dto.ToModel(deployedByUserId);

        // Assert
        request.Strategy.Should().Be(DeploymentStrategy.AllAtOnce);
        request.StepPercentage.Should().BeNull();
        request.StepIntervalSeconds.Should().BeNull();
    }
}
