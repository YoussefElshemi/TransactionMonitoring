using Core.Models.Availability;
using Core.ValueObjects;
using Infrastructure.Entities;

namespace Infrastructure.Mappers;

public static class AvailabilityCommitmentMapper
{
    public static AvailabilityCommitment ToModel(this AvailabilityCommitmentEntity availabilityCommitmentEntity)
    {
        return new AvailabilityCommitment
        {
            AvailabilityCommitmentId = new AvailabilityCommitmentId(availabilityCommitmentEntity.AvailabilityCommitmentId),
            OrganizationId = new OrganizationId(availabilityCommitmentEntity.OrganizationId),
            AvailabilityTarget = new AvailabilityTarget(availabilityCommitmentEntity.AvailabilityTarget),
            LatencyTarget = new LatencyTarget(availabilityCommitmentEntity.LatencyTargetMilliseconds),
            LatencyPercentile = new LatencyPercentile(availabilityCommitmentEntity.LatencyPercentile),
            EffectiveFrom = new EffectiveFrom(availabilityCommitmentEntity.EffectiveFrom),
            EffectiveTo = availabilityCommitmentEntity.EffectiveTo.HasValue ? new EffectiveTo(availabilityCommitmentEntity.EffectiveTo.Value) : null,

            CreditBands = availabilityCommitmentEntity.CreditBands
                .OrderBy(x => x.BelowUptimePercentage)
                .Select(x => x.ToModel())
                .ToList(),

            CreatedByUserId = availabilityCommitmentEntity.CreatedByUserId.HasValue ? new ApplicationUserId(availabilityCommitmentEntity.CreatedByUserId.Value) : null,
            CreatedAt = new CreatedAt(availabilityCommitmentEntity.CreatedAt),
            UpdatedAt = new UpdatedAt(availabilityCommitmentEntity.UpdatedAt),
            IsDeleted = new IsDeleted(availabilityCommitmentEntity.IsDeleted),
            DeletedAt = availabilityCommitmentEntity.DeletedAt.HasValue ? new DeletedAt(availabilityCommitmentEntity.DeletedAt.Value) : null
        };
    }

    public static ServiceCreditBand ToModel(this ServiceCreditBandEntity serviceCreditBandEntity)
    {
        return new ServiceCreditBand
        {
            ServiceCreditBandId = new ServiceCreditBandId(serviceCreditBandEntity.ServiceCreditBandId),
            AvailabilityCommitmentId = new AvailabilityCommitmentId(serviceCreditBandEntity.AvailabilityCommitmentId),
            BelowUptimePercentage = new UptimePercentage(serviceCreditBandEntity.BelowUptimePercentage),
            CreditPercentage = new CreditPercentage(serviceCreditBandEntity.CreditPercentage),
            CreatedAt = new CreatedAt(serviceCreditBandEntity.CreatedAt),
            UpdatedAt = new UpdatedAt(serviceCreditBandEntity.UpdatedAt)
        };
    }

    public static AvailabilityCommitmentEntity ToEntity(this AvailabilityCommitment availabilityCommitment)
    {
        return new AvailabilityCommitmentEntity
        {
            AvailabilityCommitmentId = availabilityCommitment.AvailabilityCommitmentId,
            OrganizationId = availabilityCommitment.OrganizationId,
            AvailabilityTarget = availabilityCommitment.AvailabilityTarget,
            LatencyTargetMilliseconds = availabilityCommitment.LatencyTarget,
            LatencyPercentile = availabilityCommitment.LatencyPercentile,
            EffectiveFrom = availabilityCommitment.EffectiveFrom,
            EffectiveTo = availabilityCommitment.EffectiveTo.HasValue ? availabilityCommitment.EffectiveTo.Value : null,
            CreatedByUserId = availabilityCommitment.CreatedByUserId.HasValue ? availabilityCommitment.CreatedByUserId.Value : null,
            CreditBands = availabilityCommitment.CreditBands
                .Select(x => new ServiceCreditBandEntity
                {
                    ServiceCreditBandId = x.ServiceCreditBandId,
                    AvailabilityCommitmentId = availabilityCommitment.AvailabilityCommitmentId,
                    BelowUptimePercentage = x.BelowUptimePercentage,
                    CreditPercentage = x.CreditPercentage,
                    CreatedAt = x.CreatedAt,
                    UpdatedAt = x.UpdatedAt
                })
                .ToList(),
            CreatedAt = availabilityCommitment.CreatedAt,
            UpdatedAt = availabilityCommitment.UpdatedAt,
            IsDeleted = availabilityCommitment.IsDeleted,
            DeletedAt = availabilityCommitment.DeletedAt
        };
    }
}
