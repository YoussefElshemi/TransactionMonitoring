using AutoFixture;
using Core.Enums;
using Core.Models.RuleDeployments;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.RuleDeployments;

public static class FakeRuleDeployment
{
    public static RuleDeployment CreateValid(IFixture fixture)
    {
        return new RuleDeployment
        {
            Mode = Mode.Live,
            RuleDeploymentId = FakeRuleDeploymentId.CreateValid(fixture),
            OrganizationId = FakeOrganizationId.CreateValid(fixture),
            RuleId = FakeRuleId.CreateValid(fixture),
            TargetRuleVersionId = FakeRuleVersionId.CreateValid(fixture),
            PreviousRuleVersionId = FakeRuleVersionId.CreateValid(fixture),
            Strategy = DeploymentStrategy.Gradual,
            Status = RuleDeploymentStatus.InProgress,
            CurrentPercentage = FakeRolloutPercentage.CreateValid(fixture),
            StepPercentage = FakeStepPercentage.CreateValid(fixture),
            StepIntervalSeconds = FakeStepIntervalSeconds.CreateValid(fixture),
            CreatedByUserId = FakeApplicationUserId.CreateValid(fixture),
            CompletedAt = null,
            CreatedAt = FakeCreatedAt.CreateValid(fixture),
            UpdatedAt = FakeUpdatedAt.CreateValid(fixture)
        };
    }
}
