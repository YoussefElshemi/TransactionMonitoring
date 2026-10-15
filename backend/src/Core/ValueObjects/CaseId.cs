using System.Globalization;

namespace Core.ValueObjects;

public readonly record struct CaseId : IFormattable
{
    public CaseId(Guid value)
    {
        ArgumentNullException.ThrowIfNull(value, nameof(CaseId));
        Value = value;
    }

    private Guid Value { get; }

    public static implicit operator Guid(CaseId valueObject)
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