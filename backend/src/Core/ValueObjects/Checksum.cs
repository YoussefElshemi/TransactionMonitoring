namespace Core.ValueObjects;

public readonly record struct Checksum
{
    public Checksum(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(Checksum));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(Checksum valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
