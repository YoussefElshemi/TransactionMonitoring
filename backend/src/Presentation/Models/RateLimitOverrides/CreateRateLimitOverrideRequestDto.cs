using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Swashbuckle.AspNetCore.Annotations;

namespace Presentation.Models.RateLimitOverrides;

public record CreateRateLimitOverrideRequestDto
{
    [FromRoute]
    public required Guid OrganizationId { get; init; }

    [FromBody]
    public required CreateRateLimitOverrideRequestBodyDto Body { get; init; }

    [BindNever]
    [SwaggerSchema(ReadOnly = true)]
    public Guid CreatedByUserId { get; set; }
}

public record CreateRateLimitOverrideRequestBodyDto
{
    public string? Pool { get; init; }
    public required int PermitLimit { get; init; }
    public required int TokensPerPeriod { get; init; }
    public required int ReplenishmentPeriodSeconds { get; init; }
    public required string Reason { get; init; }
    public required DateTimeOffset ExpiresAt { get; init; }
}
