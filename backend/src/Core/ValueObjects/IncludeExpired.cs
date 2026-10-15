namespace Core.ValueObjects;

public readonly record struct IncludeExpired
{
    public IncludeExpired(bool value)
    {
        Value = value;
    }

    private bool Value { get; }

    public static implicit operator bool(IncludeExpired valueObject)
    {
        return valueObject.Value;
    }
}
