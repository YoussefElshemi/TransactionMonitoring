namespace Presentation.Models.RateLimitOverrides;

// Separate type so it gets its own validator.
public record DeleteRateLimitOverrideRequestDto : OrganizationRateLimitOverrideRequestDto;
