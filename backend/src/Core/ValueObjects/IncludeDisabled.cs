namespace Core.ValueObjects;

public readonly record struct IncludeDisabled
{
    public IncludeDisabled(bool value)
    {
        Value = value;
    }

    private bool Value { get; }

    public static implicit operator bool(IncludeDisabled valueObject)
    {
        return valueObject.Value;
    }
}
