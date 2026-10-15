using Core.Extensions;

namespace Core.ValueObjects;

public readonly record struct RuleValue
{
    public RuleValue(object value)
    {
        ArgumentNullException.ThrowIfNull(value, nameof(RuleValue));
        Value = value;
    }

    private object Value { get; }

    public static implicit operator string(RuleValue valueObject)
    {
        return valueObject.Value.Serialize();
    }

    public static object ToObject(RuleValue valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value.Serialize();
    }
}