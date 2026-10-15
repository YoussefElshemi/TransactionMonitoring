namespace Core.ValueObjects;

public readonly record struct IsDelivered
{
    public IsDelivered(bool value)
    {
        Value = value;
    }

    private bool Value { get; }

    public static implicit operator bool(IsDelivered valueObject)
    {
        return valueObject.Value;
    }
}
