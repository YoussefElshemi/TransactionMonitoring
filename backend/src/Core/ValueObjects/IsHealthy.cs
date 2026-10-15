namespace Core.ValueObjects;

public readonly record struct IsHealthy
{
    public IsHealthy(bool value)
    {
        Value = value;
    }

    private bool Value { get; }

    public static implicit operator bool(IsHealthy valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}
