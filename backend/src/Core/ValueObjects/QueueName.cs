namespace Core.ValueObjects;

public readonly record struct QueueName
{
    public const int MaxLength = 64;

    public QueueName(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(QueueName));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value.Length, MaxLength, nameof(QueueName));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(QueueName valueObject)
    {
        return valueObject.Value;
    }

    public bool Equals(QueueName other)
    {
        return string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);
    }

    public override int GetHashCode()
    {
        return Value is null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(Value);
    }

    public override string ToString()
    {
        return Value;
    }
}
