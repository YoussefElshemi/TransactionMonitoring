using System.Globalization;

namespace Core.ValueObjects;

public readonly record struct ContractDiscountId : IFormattable
{
    public ContractDiscountId(Guid value)
    {
        ArgumentNullException.ThrowIfNull(value, nameof(ContractDiscountId));
        Value = value;
    }

    private Guid Value { get; }

    public static implicit operator Guid(ContractDiscountId valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value.ToString();
    }

    public string ToString(string? format, IFormatProvider? formatProvider = null)
    {
        formatProvider ??= CultureInfo.InvariantCulture;
        return Value.ToString(format, formatProvider);
    }
}
