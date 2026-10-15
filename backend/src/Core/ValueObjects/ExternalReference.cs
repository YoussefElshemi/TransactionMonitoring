namespace Core.ValueObjects;

public readonly record struct ExternalReference
{
    public ExternalReference(string value)
    {
        ArgumentNullException.ThrowIfNull(value, nameof(ExternalReference));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(ExternalReference valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
