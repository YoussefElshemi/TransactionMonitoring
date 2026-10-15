namespace Core.ValueObjects;

public readonly record struct CaseReasonCodeName
{
    public const int MaxLength = 64;

    public CaseReasonCodeName(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(CaseReasonCodeName));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value.Length, MaxLength, nameof(CaseReasonCodeName));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(CaseReasonCodeName valueObject)
    {
        return valueObject.Value;
    }

    public bool Equals(CaseReasonCodeName other)
    {
        return string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);
    }

    public override int GetHashCode()
    {
        return Value is null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(Value);
    }

    public override string ToString()
    {
        return Value;
    }
}
