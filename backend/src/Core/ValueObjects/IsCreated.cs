namespace Core.ValueObjects;

public readonly record struct IsCreated
{
    public IsCreated(bool value)
    {
        Value = value;
    }

    private bool Value { get; }

    public static implicit operator bool(IsCreated valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}
