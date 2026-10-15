namespace Core.ValueObjects;

public readonly record struct IsRequired
{
    public IsRequired(bool value)
    {
        Value = value;
    }

    private bool Value { get; }

    public static implicit operator bool(IsRequired valueObject)
    {
        return valueObject.Value;
    }
}
