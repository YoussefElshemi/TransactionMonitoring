using System.Net;

namespace Core.ValueObjects;

public readonly record struct SourceIpRange
{
    public SourceIpRange(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(SourceIpRange));

        if (!IPNetwork.TryParse(value, out _))
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Not a CIDR range.");
        }

        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(SourceIpRange valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
