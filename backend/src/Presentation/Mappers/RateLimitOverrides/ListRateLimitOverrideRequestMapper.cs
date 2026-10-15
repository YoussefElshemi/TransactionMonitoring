using Core.Enums;
using Core.Models.RateLimitOverrides;
using Core.ValueObjects;
using Presentation.Models.RateLimitOverrides;

namespace Presentation.Mappers.RateLimitOverrides;

public static class ListRateLimitOverrideRequestMapper
{
    public static ListRateLimitOverrideRequest ToModel(this ListRateLimitOverrideRequestDto dto)
    {
        return new ListRateLimitOverrideRequest
        {
            OrganizationId = new OrganizationId(dto.OrganizationId),
            Pool = !string.IsNullOrWhiteSpace(dto.Pool) ? Enum.Parse<RateLimitPool>(dto.Pool, true) : null,
            IncludeExpired = new IncludeExpired(dto.IncludeExpired ?? false),
            PageNumber = new PageNumber(dto.PageNumber),
            PageSize = new PageSize(dto.PageSize),
            FromDate = new DateRange(dto.FromDate),
            ToDate = new DateRange(dto.ToDate)
        };
    }
}
