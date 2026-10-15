namespace Core.ValueObjects;

public readonly record struct CreateCase
{
    public CreateCase(bool value)
    {
        Value = value;
    }

    private bool Value { get; }

    public static implicit operator string(CreateCase value)
    {
        return value.Value.ToString();
    }

    public static implicit operator bool(CreateCase valueObject)
    {
        return valueObject.Value;
    }
}