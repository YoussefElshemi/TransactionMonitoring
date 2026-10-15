using Core.Enums;
using Core.Models.RateLimitOverrides;
using Core.ValueObjects;
using Presentation.Models.RateLimitOverrides;

namespace Presentation.Mappers.RateLimitOverrides;

public static class CreateRateLimitOverrideRequestMapper
{
    public static CreateRateLimitOverrideRequest ToModel(this CreateRateLimitOverrideRequestDto dto)
    {
        return new CreateRateLimitOverrideRequest
        {
            OrganizationId = new OrganizationId(dto.OrganizationId),
            Pool = !string.IsNullOrWhiteSpace(dto.Body.Pool) ? Enum.Parse<RateLimitPool>(dto.Body.Pool, true) : null,
            PermitLimit = new PermitLimit(dto.Body.PermitLimit),
            TokensPerPeriod = new TokensPerPeriod(dto.Body.TokensPerPeriod),
            ReplenishmentPeriod = new ReplenishmentPeriod(dto.Body.ReplenishmentPeriodSeconds),
            Reason = new RateLimitOverrideReason(dto.Body.Reason),
            ExpiresAt = new ExpiresAt(dto.Body.ExpiresAt),
            CreatedByUserId = new ApplicationUserId(dto.CreatedByUserId)
        };
    }
}
