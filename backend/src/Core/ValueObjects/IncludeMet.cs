namespace Core.ValueObjects;

public readonly record struct IncludeMet
{
    public IncludeMet(bool value)
    {
        Value = value;
    }

    private bool Value { get; }

    public static implicit operator bool(IncludeMet valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}
