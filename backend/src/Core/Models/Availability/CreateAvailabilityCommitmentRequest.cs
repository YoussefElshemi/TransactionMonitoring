using Core.ValueObjects;

namespace Core.Models.Availability;

public record CreateAvailabilityCommitmentRequest
{
    public required OrganizationId OrganizationId { get; init; }
    public required AvailabilityTarget AvailabilityTarget { get; init; }
    public required LatencyTarget LatencyTarget { get; init; }
    public required LatencyPercentile LatencyPercentile { get; init; }

    public required EffectiveFrom? EffectiveFrom { get; init; }

    public required EffectiveTo? EffectiveTo { get; init; }
    public required List<CreateServiceCreditBandRequest> CreditBands { get; init; }
    public required ApplicationUserId? CreatedByUserId { get; init; }
}
