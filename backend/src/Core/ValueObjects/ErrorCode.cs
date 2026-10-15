namespace Core.ValueObjects;

public readonly record struct ErrorCode
{
    public ErrorCode(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(ErrorCode));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(ErrorCode valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
