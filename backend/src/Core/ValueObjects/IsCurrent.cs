namespace Core.ValueObjects;

public readonly record struct IsCurrent
{
    public IsCurrent(bool value)
    {
        Value = value;
    }

    private bool Value { get; }

    public static implicit operator bool(IsCurrent valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}
