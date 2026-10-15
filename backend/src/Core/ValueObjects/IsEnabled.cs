namespace Core.ValueObjects;

public readonly record struct IsEnabled
{
    public IsEnabled(bool value)
    {
        Value = value;
    }

    private bool Value { get; }

    public static implicit operator string(IsEnabled value)
    {
        return value.Value.ToString();
    }

    public static implicit operator bool(IsEnabled valueObject)
    {
        return valueObject.Value;
    }
}