namespace Core.ValueObjects;

public readonly record struct AllSuppressed
{
    public AllSuppressed(bool value)
    {
        Value = value;
    }

    private bool Value { get; }

    public static implicit operator string(AllSuppressed value)
    {
        return value.Value.ToString();
    }

    public static implicit operator bool(AllSuppressed valueObject)
    {
        return valueObject.Value;
    }
}
