namespace Core.ValueObjects;

public readonly record struct NormalizedUserName
{
    public NormalizedUserName(string value)
    {
        ArgumentNullException.ThrowIfNull(value, nameof(NormalizedUserName));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(NormalizedUserName valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}