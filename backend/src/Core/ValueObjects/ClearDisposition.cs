namespace Core.ValueObjects;

public readonly record struct ClearDisposition
{
    public ClearDisposition(bool value)
    {
        Value = value;
    }

    private bool Value { get; }

    public static implicit operator bool(ClearDisposition valueObject)
    {
        return valueObject.Value;
    }
}
