using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Swashbuckle.AspNetCore.Annotations;

namespace Presentation.Models.Availability;

public record CreateAvailabilityCommitmentRequestDto
{
    [FromRoute]
    public required Guid OrganizationId { get; init; }

    [FromBody]
    public required CreateAvailabilityCommitmentRequestBodyDto Body { get; init; }

    [BindNever]
    [SwaggerSchema(ReadOnly = true)]
    public Guid? CreatedByUserId { get; set; }
}

public record CreateAvailabilityCommitmentRequestBodyDto
{
    public required decimal AvailabilityTarget { get; init; }

    public required int LatencyTargetMilliseconds { get; init; }

    public required int LatencyPercentile { get; init; }

    public DateTimeOffset? EffectiveFrom { get; init; }

    public DateTimeOffset? EffectiveTo { get; init; }

    public required List<CreateServiceCreditBandRequestDto> CreditBands { get; init; }
}

public record CreateServiceCreditBandRequestDto
{
    public required decimal BelowUptimePercentage { get; init; }

    public required decimal CreditPercentage { get; init; }
}
