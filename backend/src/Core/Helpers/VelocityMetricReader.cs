using System.Globalization;
using System.Text.Json.Nodes;
using Core.Models.VelocityProfiles;
using Core.ValueObjects;

namespace Core.Helpers;

public static class VelocityMetricReader
{
    public static List<(VelocityMetricName Name, MetricText Text, MetricNumber? Number)> Read(JsonObject payload, IReadOnlyList<VelocityMetric> metrics)
    {
        var values = new List<(VelocityMetricName, MetricText, MetricNumber?)>();

        foreach (var metric in metrics)
        {
            if (TryRead(payload, metric, out var text, out var number))
            {
                values.Add((metric.Name, text, number));
            }
        }

        return values;
    }

    private static bool TryRead(JsonObject payload, VelocityMetric metric, out MetricText text, out MetricNumber? number)
    {
        text = default;
        number = null;

        var value = RuleValueHelper.Extract(PayloadPath.ResolveSingle(payload, metric.Path));

        if (value is string stringValue)
        {
            if (string.IsNullOrWhiteSpace(stringValue) || stringValue.Length > MetricText.MaxLength)
            {
                return false;
            }

            text = new MetricText(stringValue.Trim());

            return true;
        }

        if (!RuleValueHelper.TryGetNumber(value, out var numericValue))
        {
            return false;
        }

        number = new MetricNumber(numericValue);
        text = new MetricText(numericValue.ToString(CultureInfo.InvariantCulture));

        return true;
    }
}
