namespace Core.ValueObjects;

public readonly record struct IsIntact
{
    public IsIntact(bool value)
    {
        Value = value;
    }

    private bool Value { get; }

    public static implicit operator bool(IsIntact valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}
