namespace Core.ValueObjects;

public readonly record struct SubOrganizationName
{
    public const int MaxLength = 128;

    public SubOrganizationName(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(SubOrganizationName));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value.Length, MaxLength, nameof(SubOrganizationName));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(SubOrganizationName valueObject)
    {
        return valueObject.Value;
    }

    public bool Equals(SubOrganizationName other)
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
