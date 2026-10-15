namespace Core.ValueObjects;

public readonly record struct EntryDescription
{
    public EntryDescription(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(EntryDescription));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(EntryDescription valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
