namespace Core.ValueObjects;

public readonly record struct CostsComplete
{
    public CostsComplete(bool value)
    {
        Value = value;
    }

    private bool Value { get; }

    public static implicit operator bool(CostsComplete valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}
