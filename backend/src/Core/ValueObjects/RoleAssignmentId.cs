using System.Globalization;

namespace Core.ValueObjects;

public readonly record struct RoleAssignmentId : IFormattable
{
    public RoleAssignmentId(Guid value)
    {
        ArgumentNullException.ThrowIfNull(value, nameof(RoleAssignmentId));
        Value = value;
    }

    private Guid Value { get; }

    public static implicit operator Guid(RoleAssignmentId valueObject)
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
