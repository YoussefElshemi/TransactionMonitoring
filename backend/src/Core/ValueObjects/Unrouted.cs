namespace Core.ValueObjects;

public readonly record struct Unrouted
{
    public Unrouted(bool value)
    {
        Value = value;
    }

    private bool Value { get; }

    public static implicit operator bool(Unrouted valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}
