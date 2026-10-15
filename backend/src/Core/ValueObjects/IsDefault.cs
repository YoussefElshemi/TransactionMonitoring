namespace Core.ValueObjects;

public readonly record struct IsDefault
{
    public IsDefault(bool value)
    {
        Value = value;
    }

    private bool Value { get; }

    public static implicit operator bool(IsDefault valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}
