using Core.Models.VelocityProfiles;
using Core.ValueObjects;
using Presentation.Models.VelocityProfiles;

namespace Presentation.Mappers.VelocityProfiles;

public static class VelocityProfileDtoMapper
{
    public static VelocityProfileResponseDto ToDto(this VelocityProfile velocityProfile)
    {
        return new VelocityProfileResponseDto
        {
            Mode = velocityProfile.Mode.ToString(),
            VelocityProfileId = velocityProfile.VelocityProfileId,
            OrganizationId = velocityProfile.OrganizationId,
            Metrics = velocityProfile.Metrics.ToDto(),
            Thresholds = velocityProfile.Thresholds.ToDto(),
            IsDeleted = velocityProfile.IsDeleted,
            DeletedAt = velocityProfile.DeletedAt,
            CreatedAt = velocityProfile.CreatedAt,
            UpdatedAt = velocityProfile.UpdatedAt
        };
    }

    public static VelocityProfileUsageResponseDto ToDto(this VelocityProfileUsage usage)
    {
        return new VelocityProfileUsageResponseDto
        {
            RuleId = usage.RuleId,
            FriendlyName = usage.FriendlyName,
            IsEnabled = usage.IsEnabled,
            Metrics = usage.Metrics.ConvertAll(x => x.ToString()).ToArray(),
            Thresholds = usage.Thresholds.ConvertAll(x => x.ToString()).ToArray()
        };
    }

    public static VelocityBackfillResponseDto ToDto(this VelocityBackfill velocityBackfill)
    {
        return new VelocityBackfillResponseDto
        {
            Mode = velocityBackfill.Mode.ToString(),
            VelocityBackfillId = velocityBackfill.VelocityBackfillId,
            OrganizationId = velocityBackfill.OrganizationId,
            Metrics = velocityBackfill.Metrics.ConvertAll(x => x.ToString()).ToArray(),
            Status = velocityBackfill.Status.ToString(),
            TotalTransactions = velocityBackfill.TotalTransactions,
            ProcessedCount = velocityBackfill.ProcessedCount,
            CompletedAt = velocityBackfill.CompletedAt,
            CreatedAt = velocityBackfill.CreatedAt,
            UpdatedAt = velocityBackfill.UpdatedAt
        };
    }

    public static List<VelocityMetricDto> ToDto(this List<VelocityMetric> metrics)
    {
        return metrics.ConvertAll(x => new VelocityMetricDto
        {
            Name = x.Name,
            Path = x.Path
        });
    }

    public static List<VelocityThresholdDto> ToDto(this List<VelocityThreshold> thresholds)
    {
        return thresholds.ConvertAll(x => new VelocityThresholdDto
        {
            Name = x.Name,
            Value = x.Value
        });
    }

    public static List<VelocityMetric> ToModel(this List<VelocityMetricDto> metrics)
    {
        return metrics.ConvertAll(x => new VelocityMetric
        {
            Name = new VelocityMetricName(x.Name),
            Path = new VelocityMetricPath(x.Path)
        });
    }

    public static List<VelocityThreshold> ToModel(this List<VelocityThresholdDto>? thresholds)
    {
        return (thresholds ?? []).ConvertAll(x => new VelocityThreshold
        {
            Name = new VelocityThresholdName(x.Name),
            Value = new VelocityThresholdValue(x.Value)
        });
    }

    public static ListVelocityBackfillRequest ToModel(this ListVelocityBackfillRequestDto listVelocityBackfillRequestDto)
    {
        return new ListVelocityBackfillRequest
        {
            OrganizationId = new OrganizationId(listVelocityBackfillRequestDto.OrganizationId),
            PageNumber = new PageNumber(listVelocityBackfillRequestDto.PageNumber),
            PageSize = new PageSize(listVelocityBackfillRequestDto.PageSize),
            FromDate = new DateRange(listVelocityBackfillRequestDto.FromDate),
            ToDate = new DateRange(listVelocityBackfillRequestDto.ToDate)
        };
    }

    public static ListVelocityBackfillRequest ToModel(this ListMyVelocityBackfillRequestDto listMyVelocityBackfillRequestDto)
    {
        return new ListVelocityBackfillRequest
        {
            OrganizationId = new OrganizationId(listMyVelocityBackfillRequestDto.OrganizationId),
            PageNumber = new PageNumber(listMyVelocityBackfillRequestDto.PageNumber),
            PageSize = new PageSize(listMyVelocityBackfillRequestDto.PageSize),
            FromDate = new DateRange(listMyVelocityBackfillRequestDto.FromDate),
            ToDate = new DateRange(listMyVelocityBackfillRequestDto.ToDate)
        };
    }
}
