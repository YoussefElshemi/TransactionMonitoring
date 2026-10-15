using AutoFixture;
using Core.Enums;
using Infrastructure.Entities;

namespace UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

public static class FakeRuleDeploymentEntity
{
    public static RuleDeploymentEntity CreateValid(IFixture fixture)
    {
        return new RuleDeploymentEntity
        {
            ModeId = (int)Mode.Live,
            CreatedAt = fixture.Create<DateTimeOffset>(),
            UpdatedAt = fixture.Create<DateTimeOffset>(),
            RuleDeploymentId = fixture.Create<Guid>(),
            OrganizationId = fixture.Create<Guid>(),
            RuleId = fixture.Create<Guid>(),
            Rule = FakeRuleEntity.CreateValid(fixture),
            TargetRuleVersionId = fixture.Create<Guid>(),
            PreviousRuleVersionId = fixture.Create<Guid>(),
            StrategyId = (int)DeploymentStrategy.Gradual,
            StatusId = (int)RuleDeploymentStatus.InProgress,
            CurrentPercentage = 20,
            StepPercentage = 20,
            StepIntervalSeconds = 60,
            CreatedByUserId = fixture.Create<Guid>(),
            CreatedByUser = FakeApplicationUserEntity.CreateValid(fixture),
            CompletedAt = fixture.Create<DateTimeOffset>()
        };
    }
}
