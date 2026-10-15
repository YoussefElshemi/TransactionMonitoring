namespace Core.ValueObjects;

public readonly record struct IsDeleted
{
    public IsDeleted(bool value)
    {
        Value = value;
    }

    private bool Value { get; }

    public static implicit operator string(IsDeleted value)
    {
        return value.Value.ToString();
    }

    public static implicit operator bool(IsDeleted valueObject)
    {
        return valueObject.Value;
    }
}