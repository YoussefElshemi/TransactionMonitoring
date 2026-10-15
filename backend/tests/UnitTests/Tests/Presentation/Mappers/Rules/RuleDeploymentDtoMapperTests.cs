using FluentAssertions;
using Presentation.Mappers.Rules;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.RuleDeployments;

namespace UnitTests.Tests.Presentation.Mappers.Rules;

public class RuleDeploymentDtoMapperTests : BaseTestClass
{
    [Fact]
    public void ToDto_RuleDeployment()
    {
        // Arrange
        var ruleDeployment = FakeRuleDeployment.CreateValid(Fixture);

        // Act
        var dto = ruleDeployment.ToDto();

        // Assert
        dto.RuleDeploymentId.Should().Be(ruleDeployment.RuleDeploymentId);
        dto.RuleId.Should().Be(ruleDeployment.RuleId);
        dto.TargetRuleVersionId.Should().Be(ruleDeployment.TargetRuleVersionId);
        dto.PreviousRuleVersionId.Should().Be(ruleDeployment.PreviousRuleVersionId!.Value);
        dto.Strategy.Should().Be(ruleDeployment.Strategy.ToString());
        dto.Status.Should().Be(ruleDeployment.Status.ToString());
        dto.CurrentPercentage.Should().Be(ruleDeployment.CurrentPercentage);
        dto.StepPercentage.Should().Be(ruleDeployment.StepPercentage!.Value);
        dto.StepIntervalSeconds.Should().Be(ruleDeployment.StepIntervalSeconds!.Value);
        dto.CreatedAt.Should().Be(ruleDeployment.CreatedAt);
        dto.UpdatedAt.Should().Be(ruleDeployment.UpdatedAt);
    }
}
