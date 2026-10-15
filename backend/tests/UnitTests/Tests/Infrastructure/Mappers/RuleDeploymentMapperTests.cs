using Core.Enums;
using Core.ValueObjects;
using FluentAssertions;
using Infrastructure.Mappers;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.RuleDeployments;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

namespace UnitTests.Tests.Infrastructure.Mappers;

public class RuleDeploymentMapperTests : BaseTestClass
{
    [Fact]
    public void ToModel_RuleDeploymentEntity()
    {
        // Arrange
        var entity = FakeRuleDeploymentEntity.CreateValid(Fixture);

        // Act
        var ruleDeployment = entity.ToModel();

        // Assert
        ruleDeployment.RuleDeploymentId.Should().Be(new RuleDeploymentId(entity.RuleDeploymentId));
        ruleDeployment.OrganizationId.Should().Be(new OrganizationId(entity.OrganizationId));
        ruleDeployment.RuleId.Should().Be(new RuleId(entity.RuleId));
        ruleDeployment.TargetRuleVersionId.Should().Be(new RuleVersionId(entity.TargetRuleVersionId));
        ruleDeployment.PreviousRuleVersionId.Should().Be(new RuleVersionId(entity.PreviousRuleVersionId!.Value));
        ruleDeployment.Strategy.Should().Be((DeploymentStrategy)entity.StrategyId);
        ruleDeployment.Status.Should().Be((RuleDeploymentStatus)entity.StatusId);
        ruleDeployment.CurrentPercentage.Should().Be(new RolloutPercentage(entity.CurrentPercentage));
        ruleDeployment.StepPercentage.Should().Be(new StepPercentage(entity.StepPercentage!.Value));
        ruleDeployment.StepIntervalSeconds.Should().Be(new StepIntervalSeconds(entity.StepIntervalSeconds!.Value));
        ruleDeployment.CreatedByUserId.Should().Be(new ApplicationUserId(entity.CreatedByUserId));
        ruleDeployment.CompletedAt.Should().Be(new CompletedAt(entity.CompletedAt!.Value));
        ruleDeployment.CreatedAt.Should().Be(new CreatedAt(entity.CreatedAt));
        ruleDeployment.UpdatedAt.Should().Be(new UpdatedAt(entity.UpdatedAt));
    }

    [Fact]
    public void ToEntity_RuleDeployment()
    {
        // Arrange
        var ruleDeployment = FakeRuleDeployment.CreateValid(Fixture);

        // Act
        var entity = ruleDeployment.ToEntity();

        // Assert
        entity.RuleDeploymentId.Should().Be(ruleDeployment.RuleDeploymentId);
        entity.OrganizationId.Should().Be(ruleDeployment.OrganizationId);
        entity.RuleId.Should().Be(ruleDeployment.RuleId);
        entity.TargetRuleVersionId.Should().Be(ruleDeployment.TargetRuleVersionId);
        entity.PreviousRuleVersionId.Should().Be(ruleDeployment.PreviousRuleVersionId!.Value);
        entity.StrategyId.Should().Be((int)ruleDeployment.Strategy);
        entity.StatusId.Should().Be((int)ruleDeployment.Status);
        entity.CurrentPercentage.Should().Be(ruleDeployment.CurrentPercentage);
        entity.StepPercentage.Should().Be(ruleDeployment.StepPercentage!.Value);
        entity.StepIntervalSeconds.Should().Be(ruleDeployment.StepIntervalSeconds!.Value);
        entity.CreatedByUserId.Should().Be(ruleDeployment.CreatedByUserId);
        entity.CompletedAt.Should().BeNull();
        entity.CreatedAt.Should().Be(ruleDeployment.CreatedAt);
        entity.UpdatedAt.Should().Be(ruleDeployment.UpdatedAt);
    }
}
