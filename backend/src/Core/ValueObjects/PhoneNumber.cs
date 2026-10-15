namespace Core.ValueObjects;

public readonly record struct PhoneNumber
{
    public PhoneNumber(string value)
    {
        ArgumentNullException.ThrowIfNull(value, nameof(PhoneNumber));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(PhoneNumber valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}