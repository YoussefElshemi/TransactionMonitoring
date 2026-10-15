namespace Core.ValueObjects;

public readonly record struct Matched
{
    public Matched(bool value)
    {
        Value = value;
    }

    private bool Value { get; }

    public static implicit operator string(Matched value)
    {
        return value.Value.ToString();
    }

    public static implicit operator bool(Matched valueObject)
    {
        return valueObject.Value;
    }
}
