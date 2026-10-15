namespace Core.ValueObjects;

public readonly record struct Remediation
{
    public const int MaxLength = 4000;

    public Remediation(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(Remediation));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value.Length, MaxLength, nameof(Remediation));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(Remediation valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
