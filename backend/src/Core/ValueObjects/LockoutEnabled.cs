namespace Core.ValueObjects;

public readonly record struct LockoutEnabled
{
    public LockoutEnabled(bool value)
    {
        Value = value;
    }

    private bool Value { get; }

    public static implicit operator string(LockoutEnabled value)
    {
        return value.Value.ToString();
    }

    public static implicit operator bool(LockoutEnabled valueObject)
    {
        return valueObject.Value;
    }
}