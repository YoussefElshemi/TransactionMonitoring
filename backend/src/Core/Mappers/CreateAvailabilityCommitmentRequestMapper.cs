using Core.Models.Availability;
using Core.ValueObjects;

namespace Core.Mappers;

public static class CreateAvailabilityCommitmentRequestMapper
{
    public static AvailabilityCommitment ToModel(this CreateAvailabilityCommitmentRequest request, TimeProvider timeProvider)
    {
        var now = timeProvider.GetUtcNow();
        var availabilityCommitmentId = new AvailabilityCommitmentId(Guid.NewGuid());

        return new AvailabilityCommitment
        {
            AvailabilityCommitmentId = availabilityCommitmentId,
            OrganizationId = request.OrganizationId,
            AvailabilityTarget = request.AvailabilityTarget,
            LatencyTarget = request.LatencyTarget,
            LatencyPercentile = request.LatencyPercentile,
            EffectiveFrom = request.EffectiveFrom ?? new EffectiveFrom(now),
            EffectiveTo = request.EffectiveTo,
            CreditBands = request.CreditBands
                .Select(x => new ServiceCreditBand
                {
                    ServiceCreditBandId = new ServiceCreditBandId(Guid.NewGuid()),
                    AvailabilityCommitmentId = availabilityCommitmentId,
                    BelowUptimePercentage = x.BelowUptimePercentage,
                    CreditPercentage = x.CreditPercentage,
                    CreatedAt = new CreatedAt(now),
                    UpdatedAt = new UpdatedAt(now)
                })
                .OrderBy(x => (decimal)x.BelowUptimePercentage)
                .ToList(),
            CreatedByUserId = request.CreatedByUserId,
            CreatedAt = new CreatedAt(now),
            UpdatedAt = new UpdatedAt(now),
            IsDeleted = new IsDeleted(false),
            DeletedAt = null
        };
    }
}
