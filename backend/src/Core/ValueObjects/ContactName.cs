namespace Core.ValueObjects;

public readonly record struct ContactName
{
    public const int MaxLength = 200;

    public ContactName(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(ContactName));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value.Length, MaxLength, nameof(ContactName));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(ContactName valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
