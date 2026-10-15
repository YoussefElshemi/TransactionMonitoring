namespace Core.ValueObjects;

public readonly record struct ConcurrencyStamp
{
    public ConcurrencyStamp(string value)
    {
        ArgumentNullException.ThrowIfNull(value, nameof(ConcurrencyStamp));
        Value = value;
    }

    private string Value { get; }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator string(ConcurrencyStamp valueObject)
    {
        return valueObject.Value;
    }
}