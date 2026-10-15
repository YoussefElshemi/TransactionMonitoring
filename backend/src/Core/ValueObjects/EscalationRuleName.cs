namespace Core.ValueObjects;

public readonly record struct EscalationRuleName
{
    public const int MaxLength = 64;

    public EscalationRuleName(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(EscalationRuleName));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value.Length, MaxLength, nameof(EscalationRuleName));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(EscalationRuleName valueObject)
    {
        return valueObject.Value;
    }

    public bool Equals(EscalationRuleName other)
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
