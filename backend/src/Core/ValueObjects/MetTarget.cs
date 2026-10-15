namespace Core.ValueObjects;

public readonly record struct MetTarget
{
    public MetTarget(bool value)
    {
        Value = value;
    }

    private bool Value { get; }

    public static implicit operator bool(MetTarget valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}
