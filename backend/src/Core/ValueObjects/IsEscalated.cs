namespace Core.ValueObjects;

public readonly record struct IsEscalated
{
    public IsEscalated(bool value)
    {
        Value = value;
    }

    private bool Value { get; }

    public static implicit operator bool(IsEscalated valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}
