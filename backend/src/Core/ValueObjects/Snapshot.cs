namespace Core.ValueObjects;

public readonly record struct Snapshot
{
    public Snapshot(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(Snapshot));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(Snapshot valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
