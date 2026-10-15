namespace Core.ValueObjects;

public readonly record struct NotifyOnBreach
{
    public NotifyOnBreach(bool value)
    {
        Value = value;
    }

    private bool Value { get; }

    public static implicit operator bool(NotifyOnBreach valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}
