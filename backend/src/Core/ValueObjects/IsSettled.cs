namespace Core.ValueObjects;

public readonly record struct IsSettled
{
    public IsSettled(bool value)
    {
        Value = value;
    }

    private bool Value { get; }

    public static implicit operator bool(IsSettled valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}
