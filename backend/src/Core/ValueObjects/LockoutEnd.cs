namespace Core.ValueObjects;

public readonly record struct LockoutEnd
{
    public LockoutEnd(DateTimeOffset value)
    {
        ArgumentNullException.ThrowIfNull(value, nameof(LockoutEnd));
        Value = value;
    }

    private DateTimeOffset Value { get; }

    public static implicit operator DateTimeOffset(LockoutEnd valueObject)
    {
        return valueObject.Value;
    }
}