namespace Core.ValueObjects;

public readonly record struct ContractRule
{
    public ContractRule(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(ContractRule));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(ContractRule valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
