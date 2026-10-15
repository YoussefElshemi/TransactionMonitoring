namespace Core.ValueObjects;

public readonly record struct ContractDiscountDescription
{
    public const int MaxLength = 500;

    public ContractDiscountDescription(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(ContractDiscountDescription));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value.Length, MaxLength, nameof(ContractDiscountDescription));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(ContractDiscountDescription valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
