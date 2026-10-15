namespace Core.ValueObjects;

public readonly record struct IncludeSuperseded
{
    public IncludeSuperseded(bool value)
    {
        Value = value;
    }

    private bool Value { get; }

    public static implicit operator bool(IncludeSuperseded valueObject)
    {
        return valueObject.Value;
    }
}
