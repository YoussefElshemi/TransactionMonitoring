using Core.Extensions;
using Core.Models.VelocityProfiles;
using Core.ValueObjects;

namespace Infrastructure.Mappers;

public static class VelocityMetricMapper
{
    public static List<VelocityMetric> MetricsToModel(this string metrics)
    {
        if (string.IsNullOrWhiteSpace(metrics))
        {
            return [];
        }

        return metrics.FromBase64String().TryDeserialize<List<VelocityMetricModel>>().ConvertAll(x => new VelocityMetric
        {
            Name = new VelocityMetricName(x.Name ?? string.Empty),
            Path = new VelocityMetricPath(x.Path ?? string.Empty)
        });
    }

    public static string ToBase64String(this List<VelocityMetric> metrics)
    {
        return metrics
            .ConvertAll(x => new VelocityMetricModel { Name = x.Name, Path = x.Path })
            .Serialize()
            .ToBase64String();
    }

    public static List<VelocityThreshold> ThresholdsToModel(this string thresholds)
    {
        if (string.IsNullOrWhiteSpace(thresholds))
        {
            return [];
        }

        return thresholds.FromBase64String().TryDeserialize<List<VelocityThresholdModel>>().ConvertAll(x => new VelocityThreshold
        {
            Name = new VelocityThresholdName(x.Name ?? string.Empty),
            Value = new VelocityThresholdValue(x.Value)
        });
    }

    public static string ToBase64String(this List<VelocityThreshold> thresholds)
    {
        return thresholds
            .ConvertAll(x => new VelocityThresholdModel { Name = x.Name, Value = x.Value })
            .Serialize()
            .ToBase64String();
    }

    public static List<VelocityMetricName> MetricNamesToModel(this string metrics)
    {
        if (string.IsNullOrWhiteSpace(metrics))
        {
            return [];
        }

        return metrics.FromBase64String().TryDeserialize<List<string>>().ConvertAll(x => new VelocityMetricName(x));
    }

    public static string ToBase64String(this List<VelocityMetricName> metrics)
    {
        return metrics.ConvertAll(x => (string)x).Serialize().ToBase64String();
    }
}
