namespace Core.ValueObjects;

public readonly record struct QueueDescription
{
    public const int MaxLength = 512;

    public QueueDescription(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(QueueDescription));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value.Length, MaxLength, nameof(QueueDescription));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(QueueDescription valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
