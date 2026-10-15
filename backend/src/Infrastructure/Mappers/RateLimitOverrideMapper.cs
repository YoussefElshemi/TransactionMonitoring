using Core.Enums;
using Core.Models.RateLimitOverrides;
using Core.ValueObjects;
using Infrastructure.Entities;

namespace Infrastructure.Mappers;

public static class RateLimitOverrideMapper
{
    public static RateLimitOverride ToModel(this RateLimitOverrideEntity rateLimitOverrideEntity)
    {
        return new RateLimitOverride
        {
            RateLimitOverrideId = new RateLimitOverrideId(rateLimitOverrideEntity.RateLimitOverrideId),
            OrganizationId = new OrganizationId(rateLimitOverrideEntity.OrganizationId),
            Pool = rateLimitOverrideEntity.PoolId.HasValue ? (RateLimitPool)rateLimitOverrideEntity.PoolId.Value : null,
            PermitLimit = new PermitLimit(rateLimitOverrideEntity.PermitLimit),
            TokensPerPeriod = new TokensPerPeriod(rateLimitOverrideEntity.TokensPerPeriod),
            ReplenishmentPeriod = new ReplenishmentPeriod(rateLimitOverrideEntity.ReplenishmentPeriodSeconds),
            Reason = new RateLimitOverrideReason(rateLimitOverrideEntity.Reason),
            ExpiresAt = new ExpiresAt(rateLimitOverrideEntity.ExpiresAt),
            CreatedByUserId = new ApplicationUserId(rateLimitOverrideEntity.CreatedByUserId),
            CreatedAt = new CreatedAt(rateLimitOverrideEntity.CreatedAt),
            UpdatedAt = new UpdatedAt(rateLimitOverrideEntity.UpdatedAt),
            IsDeleted = new IsDeleted(rateLimitOverrideEntity.IsDeleted),
            DeletedAt = rateLimitOverrideEntity.DeletedAt.HasValue ? new DeletedAt(rateLimitOverrideEntity.DeletedAt.Value) : null
        };
    }

    public static RateLimitOverrideEntity ToEntity(this RateLimitOverride rateLimitOverride)
    {
        return new RateLimitOverrideEntity
        {
            RateLimitOverrideId = rateLimitOverride.RateLimitOverrideId,
            OrganizationId = rateLimitOverride.OrganizationId,
            PoolId = rateLimitOverride.Pool.HasValue ? (int)rateLimitOverride.Pool.Value : null,
            PermitLimit = rateLimitOverride.PermitLimit,
            TokensPerPeriod = rateLimitOverride.TokensPerPeriod,
            ReplenishmentPeriodSeconds = rateLimitOverride.ReplenishmentPeriod,
            Reason = rateLimitOverride.Reason,
            ExpiresAt = rateLimitOverride.ExpiresAt,
            CreatedByUserId = rateLimitOverride.CreatedByUserId,
            CreatedAt = rateLimitOverride.CreatedAt,
            UpdatedAt = rateLimitOverride.UpdatedAt,
            IsDeleted = rateLimitOverride.IsDeleted,
            DeletedAt = rateLimitOverride.DeletedAt
        };
    }
}
