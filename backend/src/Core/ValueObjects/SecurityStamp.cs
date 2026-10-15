namespace Core.ValueObjects;

public readonly record struct SecurityStamp
{
    public SecurityStamp(string value)
    {
        ArgumentNullException.ThrowIfNull(value, nameof(SecurityStamp));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(SecurityStamp valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}