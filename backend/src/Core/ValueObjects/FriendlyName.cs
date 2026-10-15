namespace Core.ValueObjects;

public readonly record struct FriendlyName
{
    public FriendlyName(string value)
    {
        ArgumentNullException.ThrowIfNull(value, nameof(FriendlyName));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(FriendlyName valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}