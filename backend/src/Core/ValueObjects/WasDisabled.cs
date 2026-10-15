namespace Core.ValueObjects;

public readonly record struct WasDisabled
{
    public WasDisabled(bool value)
    {
        Value = value;
    }

    private bool Value { get; }

    public static implicit operator bool(WasDisabled valueObject)
    {
        return valueObject.Value;
    }
}
