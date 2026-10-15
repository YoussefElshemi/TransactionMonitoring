using Core.Models.Availability;
using Core.ValueObjects;
using Presentation.Models.Availability;

namespace Presentation.Mappers.Availability;

public static class AvailabilityMapper
{
    public static CreateAvailabilityCommitmentRequest ToModel(this CreateAvailabilityCommitmentRequestDto dto)
    {
        return new CreateAvailabilityCommitmentRequest
        {
            OrganizationId = new OrganizationId(dto.OrganizationId),
            AvailabilityTarget = new AvailabilityTarget(dto.Body.AvailabilityTarget),
            LatencyTarget = new LatencyTarget(dto.Body.LatencyTargetMilliseconds),
            LatencyPercentile = new LatencyPercentile(dto.Body.LatencyPercentile),
            EffectiveFrom = dto.Body.EffectiveFrom.HasValue ? new EffectiveFrom(dto.Body.EffectiveFrom.Value) : null,
            EffectiveTo = dto.Body.EffectiveTo.HasValue ? new EffectiveTo(dto.Body.EffectiveTo.Value) : null,
            CreditBands = dto.Body.CreditBands.ConvertAll(x => new CreateServiceCreditBandRequest
            {
                BelowUptimePercentage = new UptimePercentage(x.BelowUptimePercentage),
                CreditPercentage = new CreditPercentage(x.CreditPercentage)
            }),
            CreatedByUserId = dto.CreatedByUserId.HasValue ? new ApplicationUserId(dto.CreatedByUserId.Value) : null
        };
    }

    public static ListAvailabilityCommitmentRequest ToModel(this ListAvailabilityCommitmentRequestDto dto)
    {
        return new ListAvailabilityCommitmentRequest
        {
            OrganizationId = new OrganizationId(dto.OrganizationId),
            IncludeSuperseded = new IncludeSuperseded(dto.IncludeSuperseded ?? false),
            PageNumber = new PageNumber(dto.PageNumber),
            PageSize = new PageSize(dto.PageSize),
            FromDate = new DateRange(dto.FromDate),
            ToDate = new DateRange(dto.ToDate)
        };
    }

    public static MeasureAvailabilityRequest ToModel(this MeasureAvailabilityRequestDto dto)
    {
        var periodStart = new DateTimeOffset(dto.FromDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var periodEnd = dto.ToDate == DateOnly.MaxValue
            ? DateTimeOffset.MaxValue
            : new DateTimeOffset(dto.ToDate.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

        return new MeasureAvailabilityRequest
        {
            OrganizationId = new OrganizationId(dto.OrganizationId),
            PeriodStart = new PeriodStart(periodStart),
            PeriodEnd = new PeriodEnd(periodEnd)
        };
    }

    public static AvailabilityMeasurementResponseDto ToDto(this AvailabilityMeasurement availabilityMeasurement)
    {
        return new AvailabilityMeasurementResponseDto
        {
            OrganizationId = availabilityMeasurement.OrganizationId,
            PeriodStart = availabilityMeasurement.PeriodStart,
            PeriodEnd = availabilityMeasurement.PeriodEnd,
            TotalRequests = availabilityMeasurement.TotalRequests,
            FailedRequests = availabilityMeasurement.FailedRequests,
            UptimePercentage = availabilityMeasurement.UptimePercentage,
            MeanLatencyMilliseconds = availabilityMeasurement.MeanLatencyMilliseconds,
            PercentileLatencyMilliseconds = availabilityMeasurement.PercentileLatencyMilliseconds,
            Percentile = availabilityMeasurement.Percentile,
            MaxLatencyMilliseconds = availabilityMeasurement.MaxLatencyMilliseconds,
            AvailabilityTarget = availabilityMeasurement.AvailabilityTarget.HasValue ? availabilityMeasurement.AvailabilityTarget.Value : null,
            LatencyTargetMilliseconds = availabilityMeasurement.LatencyTarget.HasValue ? availabilityMeasurement.LatencyTarget.Value : null,
            MetAvailabilityTarget = availabilityMeasurement.MetAvailabilityTarget.HasValue ? availabilityMeasurement.MetAvailabilityTarget.Value : null,
            MetLatencyTarget = availabilityMeasurement.MetLatencyTarget.HasValue ? availabilityMeasurement.MetLatencyTarget.Value : null
        };
    }

    public static AvailabilityCommitmentResponseDto ToDto(this AvailabilityCommitment availabilityCommitment)
    {
        return new AvailabilityCommitmentResponseDto
        {
            AvailabilityCommitmentId = availabilityCommitment.AvailabilityCommitmentId,
            OrganizationId = availabilityCommitment.OrganizationId,
            AvailabilityTarget = availabilityCommitment.AvailabilityTarget,
            LatencyTargetMilliseconds = availabilityCommitment.LatencyTarget,
            LatencyPercentile = availabilityCommitment.LatencyPercentile,
            EffectiveFrom = availabilityCommitment.EffectiveFrom,
            EffectiveTo = availabilityCommitment.EffectiveTo.HasValue ? availabilityCommitment.EffectiveTo.Value : null,
            CreditBands = availabilityCommitment.CreditBands.ConvertAll(x => new ServiceCreditBandResponseDto
            {
                ServiceCreditBandId = x.ServiceCreditBandId,
                BelowUptimePercentage = x.BelowUptimePercentage,
                CreditPercentage = x.CreditPercentage
            }),
            CreatedByUserId = availabilityCommitment.CreatedByUserId.HasValue ? availabilityCommitment.CreatedByUserId.Value : null,
            CreatedAt = availabilityCommitment.CreatedAt,
            UpdatedAt = availabilityCommitment.UpdatedAt
        };
    }
}
