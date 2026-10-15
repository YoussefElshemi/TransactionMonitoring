using Core.Enums;
using Core.Models.VelocityProfiles;
using Core.ValueObjects;

namespace Core.Mappers;

public static class SetVelocityProfileRequestMapper
{
    public static VelocityProfile ToModel(this SetVelocityProfileRequest setVelocityProfileRequest, TimeProvider timeProvider, Mode mode)
    {
        return new VelocityProfile
        {
            Mode = mode,
            VelocityProfileId = new VelocityProfileId(Guid.NewGuid()),
            OrganizationId = setVelocityProfileRequest.OrganizationId,
            Metrics = setVelocityProfileRequest.Metrics,
            Thresholds = setVelocityProfileRequest.Thresholds,
            CreatedByUserId = setVelocityProfileRequest.CreatedByUserId,
            CreatedAt = new CreatedAt(timeProvider.GetUtcNow()),
            UpdatedAt = new UpdatedAt(timeProvider.GetUtcNow()),
            IsDeleted = new IsDeleted(false),
            DeletedAt = null
        };
    }
}
