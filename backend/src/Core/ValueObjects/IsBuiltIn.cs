namespace Core.ValueObjects;

public readonly record struct IsBuiltIn
{
    public IsBuiltIn(bool value)
    {
        Value = value;
    }

    private bool Value { get; }

    public static implicit operator bool(IsBuiltIn valueObject)
    {
        return valueObject.Value;
    }
}
