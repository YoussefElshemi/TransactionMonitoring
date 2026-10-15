namespace Core.ValueObjects;

public readonly record struct CaseReasonCodeDescription
{
    public const int MaxLength = 500;

    public CaseReasonCodeDescription(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(CaseReasonCodeDescription));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value.Length, MaxLength, nameof(CaseReasonCodeDescription));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(CaseReasonCodeDescription valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
