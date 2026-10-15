using Core.Helpers;

namespace Core.ValueObjects;

public readonly record struct PayloadFieldPath
{
    public const int MaxLength = 512;

    public PayloadFieldPath(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(PayloadFieldPath));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value.Length, MaxLength, nameof(PayloadFieldPath));

        if (!PayloadPath.IsAddressable(value))
        {
            throw new ArgumentException($"'{value}' is not a path the engine can read.", nameof(PayloadFieldPath));
        }

        if (PayloadPath.HasWildcard(value))
        {
            throw new ArgumentException($"'{value}' resolves to many values, so it cannot name a field.", nameof(PayloadFieldPath));
        }

        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(PayloadFieldPath valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
