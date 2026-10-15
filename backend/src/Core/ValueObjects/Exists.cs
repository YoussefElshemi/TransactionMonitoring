namespace Core.ValueObjects;

public readonly record struct Exists
{
    public Exists(bool value)
    {
        Value = value;
    }

    private bool Value { get; }

    public static implicit operator string(Exists value)
    {
        return value.Value.ToString();
    }

    public static implicit operator bool(Exists valueObject)
    {
        return valueObject.Value;
    }
}