using Core.Models.RateLimitOverrides;
using Core.ValueObjects;

namespace Core.Mappers;

public static class CreateRateLimitOverrideRequestMapper
{
    public static RateLimitOverride ToModel(this CreateRateLimitOverrideRequest request, TimeProvider timeProvider)
    {
        var now = timeProvider.GetUtcNow();

        return new RateLimitOverride
        {
            RateLimitOverrideId = new RateLimitOverrideId(Guid.NewGuid()),
            OrganizationId = request.OrganizationId,
            Pool = request.Pool,
            PermitLimit = request.PermitLimit,
            TokensPerPeriod = request.TokensPerPeriod,
            ReplenishmentPeriod = request.ReplenishmentPeriod,
            Reason = request.Reason,
            ExpiresAt = request.ExpiresAt,
            CreatedByUserId = request.CreatedByUserId,
            CreatedAt = new CreatedAt(now),
            UpdatedAt = new UpdatedAt(now),
            IsDeleted = new IsDeleted(false),
            DeletedAt = null
        };
    }
}
