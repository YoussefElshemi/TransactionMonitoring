namespace Core.ValueObjects;

public readonly record struct RuleField
{
    public RuleField(string value)
    {
        ArgumentNullException.ThrowIfNull(value, nameof(RuleField));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(RuleField valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}