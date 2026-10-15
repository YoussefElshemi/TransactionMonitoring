namespace Core.ValueObjects;

public readonly record struct IncludeResolved
{
    public IncludeResolved(bool value)
    {
        Value = value;
    }

    private bool Value { get; }

    public static implicit operator bool(IncludeResolved valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}
