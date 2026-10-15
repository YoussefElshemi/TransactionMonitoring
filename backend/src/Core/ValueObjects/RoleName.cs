namespace Core.ValueObjects;

public readonly record struct RoleName
{
    public const int MaxLength = 64;

    public RoleName(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(RoleName));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value.Length, MaxLength, nameof(RoleName));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(RoleName valueObject)
    {
        return valueObject.Value;
    }

    public bool Equals(RoleName other)
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
