using Core.Models.Plans;
using Core.ValueObjects;
using Infrastructure.Entities;

namespace Infrastructure.Mappers;

public static class PlanMapper
{
    public static Plan ToModel(this PlanEntity planEntity)
    {
        return new Plan
        {
            PlanId = new PlanId(planEntity.PlanId),
            Name = new PlanName(planEntity.Name),
            Description = !string.IsNullOrWhiteSpace(planEntity.Description) ? new PlanDescription(planEntity.Description) : null,
            OrganizationId = planEntity.OrganizationId.HasValue ? new OrganizationId(planEntity.OrganizationId.Value) : null,
            IsEnabled = new IsEnabled(planEntity.IsEnabled),
            CurrentVersionNumber = new VersionNumber(planEntity.CurrentVersionNumber),
            CurrentVersion = planEntity.PlanVersions
                .FirstOrDefault(x => x.VersionNumber == planEntity.CurrentVersionNumber)?.ToModel(),
            CreatedAt = new CreatedAt(planEntity.CreatedAt),
            UpdatedAt = new UpdatedAt(planEntity.UpdatedAt),
            IsDeleted = new IsDeleted(planEntity.IsDeleted),
            DeletedAt = planEntity.DeletedAt.HasValue ? new DeletedAt(planEntity.DeletedAt.Value) : null
        };
    }

    public static PlanEntity ToEntity(this Plan plan)
    {
        return new PlanEntity
        {
            PlanId = plan.PlanId,
            Name = plan.Name,
            Description = plan.Description?.ToString(),
            OrganizationId = plan.OrganizationId.HasValue ? plan.OrganizationId.Value : null,
            IsEnabled = plan.IsEnabled,
            CurrentVersionNumber = plan.CurrentVersionNumber,
            CreatedAt = plan.CreatedAt,
            UpdatedAt = plan.UpdatedAt,
            IsDeleted = plan.IsDeleted,
            DeletedAt = plan.DeletedAt
        };
    }
}
