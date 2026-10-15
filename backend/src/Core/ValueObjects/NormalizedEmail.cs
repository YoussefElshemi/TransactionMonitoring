namespace Core.ValueObjects;

public readonly record struct NormalizedEmail
{
    public NormalizedEmail(string value)
    {
        ArgumentNullException.ThrowIfNull(value, nameof(NormalizedEmail));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(NormalizedEmail valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}