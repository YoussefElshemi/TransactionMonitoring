namespace Core.ValueObjects;

public readonly record struct ProbeDetail
{
    public const int MaxLength = 500;

    public ProbeDetail(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(ProbeDetail));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value.Length, MaxLength, nameof(ProbeDetail));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(ProbeDetail valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
