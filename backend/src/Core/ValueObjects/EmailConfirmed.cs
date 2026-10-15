namespace Core.ValueObjects;

public readonly record struct EmailConfirmed
{
    public EmailConfirmed(bool value)
    {
        Value = value;
    }

    private bool Value { get; }

    public static implicit operator string(EmailConfirmed value)
    {
        return value.Value.ToString();
    }

    public static implicit operator bool(EmailConfirmed valueObject)
    {
        return valueObject.Value;
    }
}