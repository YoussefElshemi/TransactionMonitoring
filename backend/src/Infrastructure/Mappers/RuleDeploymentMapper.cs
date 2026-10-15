using Core.Enums;
using Core.Models.RuleDeployments;
using Core.ValueObjects;
using Infrastructure.Entities;

namespace Infrastructure.Mappers;

public static class RuleDeploymentMapper
{
    public static RuleDeployment ToModel(this RuleDeploymentEntity entity)
    {
        return new RuleDeployment
        {
            Mode = (Mode)entity.ModeId,
            RuleDeploymentId = new RuleDeploymentId(entity.RuleDeploymentId),
            OrganizationId = new OrganizationId(entity.OrganizationId),
            RuleId = new RuleId(entity.RuleId),
            TargetRuleVersionId = new RuleVersionId(entity.TargetRuleVersionId),
            PreviousRuleVersionId = entity.PreviousRuleVersionId.HasValue
                ? new RuleVersionId(entity.PreviousRuleVersionId.Value)
                : null,
            Strategy = (DeploymentStrategy)entity.StrategyId,
            Status = (RuleDeploymentStatus)entity.StatusId,
            CurrentPercentage = new RolloutPercentage(entity.CurrentPercentage),
            StepPercentage = entity.StepPercentage.HasValue ? new StepPercentage(entity.StepPercentage.Value) : null,
            StepIntervalSeconds = entity.StepIntervalSeconds.HasValue ? new StepIntervalSeconds(entity.StepIntervalSeconds.Value) : null,
            CreatedByUserId = new ApplicationUserId(entity.CreatedByUserId),
            CompletedAt = entity.CompletedAt.HasValue ? new CompletedAt(entity.CompletedAt.Value) : null,
            CreatedAt = new CreatedAt(entity.CreatedAt),
            UpdatedAt = new UpdatedAt(entity.UpdatedAt)
        };
    }

    public static RuleDeploymentEntity ToEntity(this RuleDeployment deployment)
    {
        return new RuleDeploymentEntity
        {
            ModeId = (int)deployment.Mode,
            RuleDeploymentId = deployment.RuleDeploymentId,
            OrganizationId = deployment.OrganizationId,
            RuleId = deployment.RuleId,
            TargetRuleVersionId = deployment.TargetRuleVersionId,
            PreviousRuleVersionId = deployment.PreviousRuleVersionId,
            StrategyId = (int)deployment.Strategy,
            StatusId = (int)deployment.Status,
            CurrentPercentage = deployment.CurrentPercentage,
            StepPercentage = deployment.StepPercentage,
            StepIntervalSeconds = deployment.StepIntervalSeconds,
            CreatedByUserId = deployment.CreatedByUserId,
            CompletedAt = deployment.CompletedAt,
            CreatedAt = deployment.CreatedAt,
            UpdatedAt = deployment.UpdatedAt
        };
    }
}
