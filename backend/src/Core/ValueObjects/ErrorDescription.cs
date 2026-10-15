namespace Core.ValueObjects;

public readonly record struct ErrorDescription
{
    public ErrorDescription(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(ErrorDescription));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(ErrorDescription valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
