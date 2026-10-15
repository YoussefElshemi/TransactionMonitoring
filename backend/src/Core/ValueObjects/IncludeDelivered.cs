namespace Core.ValueObjects;

public readonly record struct IncludeDelivered
{
    public IncludeDelivered(bool value)
    {
        Value = value;
    }

    private bool Value { get; }

    public static implicit operator bool(IncludeDelivered valueObject)
    {
        return valueObject.Value;
    }
}
