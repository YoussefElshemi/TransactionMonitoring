namespace Core.ValueObjects;

public readonly record struct Destination
{
    public Destination(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(Destination));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(Destination valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
