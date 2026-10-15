namespace Core.ValueObjects;

public readonly record struct ReportingEntityId
{
    public const int MaxLength = 64;

    public ReportingEntityId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(ReportingEntityId));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value.Length, MaxLength, nameof(ReportingEntityId));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(ReportingEntityId valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
