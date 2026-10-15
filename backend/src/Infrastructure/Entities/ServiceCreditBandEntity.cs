using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record ServiceCreditBandEntity : BaseEntity
{
    public required Guid ServiceCreditBandId { get; init; }
    public required Guid AvailabilityCommitmentId { get; init; }
    public AvailabilityCommitmentEntity AvailabilityCommitment { get; init; } = null!;
    public required decimal BelowUptimePercentage { get; init; }
    public required decimal CreditPercentage { get; init; }
}
