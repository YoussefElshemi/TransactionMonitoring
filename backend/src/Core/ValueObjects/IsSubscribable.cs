namespace Core.ValueObjects;

public readonly record struct IsSubscribable
{
    public IsSubscribable(bool value)
    {
        Value = value;
    }

    private bool Value { get; }

    public static implicit operator bool(IsSubscribable valueObject)
    {
        return valueObject.Value;
    }
}
