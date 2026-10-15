namespace Core.ValueObjects;

public readonly record struct ClearCriteria
{
    public ClearCriteria(bool value)
    {
        Value = value;
    }

    private bool Value { get; }

    public static implicit operator bool(ClearCriteria valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}
