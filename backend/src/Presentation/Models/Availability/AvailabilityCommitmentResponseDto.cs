namespace Presentation.Models.Availability;

public record AvailabilityCommitmentResponseDto
{
    public required Guid AvailabilityCommitmentId { get; init; }
    public required Guid OrganizationId { get; init; }
    public required decimal AvailabilityTarget { get; init; }
    public required int LatencyTargetMilliseconds { get; init; }
    public required int LatencyPercentile { get; init; }
    public required DateTimeOffset EffectiveFrom { get; init; }
    public required DateTimeOffset? EffectiveTo { get; init; }
    public required List<ServiceCreditBandResponseDto> CreditBands { get; init; }
    public required Guid? CreatedByUserId { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
}
