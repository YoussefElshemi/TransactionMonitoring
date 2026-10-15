namespace Core.ValueObjects;

public readonly record struct PhoneNumberConfirmed
{
    public PhoneNumberConfirmed(bool value)
    {
        Value = value;
    }

    private bool Value { get; }

    public static implicit operator string(PhoneNumberConfirmed value)
    {
        return value.Value.ToString();
    }

    public static implicit operator bool(PhoneNumberConfirmed valueObject)
    {
        return valueObject.Value;
    }
}