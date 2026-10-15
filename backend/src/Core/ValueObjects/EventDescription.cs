namespace Core.ValueObjects;

public readonly record struct EventDescription
{
    public EventDescription(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(EventDescription));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(EventDescription valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
