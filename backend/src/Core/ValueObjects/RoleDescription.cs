namespace Core.ValueObjects;

public readonly record struct RoleDescription
{
    public const int MaxLength = 512;

    public RoleDescription(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(RoleDescription));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value.Length, MaxLength, nameof(RoleDescription));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(RoleDescription valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
