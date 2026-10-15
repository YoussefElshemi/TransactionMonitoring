namespace Core.ValueObjects;

public readonly record struct TwoFactorEnabled
{
    public TwoFactorEnabled(bool value)
    {
        Value = value;
    }

    private bool Value { get; }

    public static implicit operator string(TwoFactorEnabled value)
    {
        return value.Value.ToString();
    }

    public static implicit operator bool(TwoFactorEnabled valueObject)
    {
        return valueObject.Value;
    }
}