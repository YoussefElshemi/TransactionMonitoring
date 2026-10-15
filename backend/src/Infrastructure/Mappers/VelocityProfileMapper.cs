using Core.Enums;
using Core.Models.VelocityProfiles;
using Core.ValueObjects;
using Infrastructure.Entities;

namespace Infrastructure.Mappers;

public static class VelocityProfileMapper
{
    public static VelocityProfile ToModel(this VelocityProfileEntity velocityProfileEntity)
    {
        return new VelocityProfile
        {
            Mode = (Mode)velocityProfileEntity.ModeId,
            VelocityProfileId = new VelocityProfileId(velocityProfileEntity.VelocityProfileId),
            OrganizationId = new OrganizationId(velocityProfileEntity.OrganizationId),
            Metrics = velocityProfileEntity.Metrics.MetricsToModel(),
            Thresholds = velocityProfileEntity.Thresholds.ThresholdsToModel(),
            CreatedByUserId = new ApplicationUserId(velocityProfileEntity.CreatedByUserId),
            CreatedAt = new CreatedAt(velocityProfileEntity.CreatedAt),
            UpdatedAt = new UpdatedAt(velocityProfileEntity.UpdatedAt),
            IsDeleted = new IsDeleted(velocityProfileEntity.IsDeleted),
            DeletedAt = velocityProfileEntity.DeletedAt.HasValue ? new DeletedAt(velocityProfileEntity.DeletedAt.Value) : null
        };
    }

    public static VelocityProfileEntity ToEntity(this VelocityProfile velocityProfile)
    {
        return new VelocityProfileEntity
        {
            ModeId = (int)velocityProfile.Mode,
            VelocityProfileId = velocityProfile.VelocityProfileId,
            OrganizationId = velocityProfile.OrganizationId,
            Metrics = velocityProfile.Metrics.ToBase64String(),
            Thresholds = velocityProfile.Thresholds.ToBase64String(),
            CreatedByUserId = velocityProfile.CreatedByUserId,
            CreatedAt = velocityProfile.CreatedAt,
            UpdatedAt = velocityProfile.UpdatedAt,
            IsDeleted = velocityProfile.IsDeleted,
            DeletedAt = velocityProfile.DeletedAt
        };
    }
}
