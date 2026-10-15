namespace Core.ValueObjects;

public readonly record struct FileName
{
    public FileName(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(FileName));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(FileName valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
