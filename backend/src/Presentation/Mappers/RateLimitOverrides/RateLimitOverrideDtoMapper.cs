using Core.Models.RateLimitOverrides;
using Presentation.Models.RateLimitOverrides;

namespace Presentation.Mappers.RateLimitOverrides;

public static class RateLimitOverrideDtoMapper
{
    public static RateLimitOverrideResponseDto ToDto(this RateLimitOverride rateLimitOverride)
    {
        return new RateLimitOverrideResponseDto
        {
            RateLimitOverrideId = rateLimitOverride.RateLimitOverrideId,
            OrganizationId = rateLimitOverride.OrganizationId,
            Pool = rateLimitOverride.Pool?.ToString(),
            PermitLimit = rateLimitOverride.PermitLimit,
            TokensPerPeriod = rateLimitOverride.TokensPerPeriod,
            ReplenishmentPeriodSeconds = rateLimitOverride.ReplenishmentPeriod,
            Reason = rateLimitOverride.Reason,
            ExpiresAt = rateLimitOverride.ExpiresAt,
            CreatedByUserId = rateLimitOverride.CreatedByUserId,
            CreatedAt = rateLimitOverride.CreatedAt,
            UpdatedAt = rateLimitOverride.UpdatedAt
        };
    }
}
