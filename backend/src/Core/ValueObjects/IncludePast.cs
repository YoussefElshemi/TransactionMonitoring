namespace Core.ValueObjects;

public readonly record struct IncludePast
{
    public IncludePast(bool value)
    {
        Value = value;
    }

    private bool Value { get; }

    public static implicit operator bool(IncludePast valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}
