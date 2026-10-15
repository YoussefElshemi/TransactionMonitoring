using Core.Models.BaseModels;
using Core.ValueObjects;

namespace Core.Models.Availability;

public record ServiceCreditBand : BaseModel
{
    public required ServiceCreditBandId ServiceCreditBandId { get; init; }
    public required AvailabilityCommitmentId AvailabilityCommitmentId { get; init; }

    // Exclusive: a month measuring exactly at the bound is not in the band.
    public required UptimePercentage BelowUptimePercentage { get; init; }

    public required CreditPercentage CreditPercentage { get; init; }
}
