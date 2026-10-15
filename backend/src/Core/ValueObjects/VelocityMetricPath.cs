using Core.Helpers;

namespace Core.ValueObjects;

public readonly record struct VelocityMetricPath
{
    public VelocityMetricPath(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(VelocityMetricPath));

        if (!PayloadPath.IsAddressable(value))
        {
            throw new ArgumentException($"'{value}' is not a path the engine can read.", nameof(VelocityMetricPath));
        }

        if (PayloadPath.HasWildcard(value))
        {
            throw new ArgumentException($"'{value}' resolves to many values, so it cannot be a metric.", nameof(VelocityMetricPath));
        }

        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(VelocityMetricPath valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
