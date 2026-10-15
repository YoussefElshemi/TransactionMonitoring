namespace Core.ValueObjects;

public readonly record struct IncludeDeleted
{
    public IncludeDeleted(bool value)
    {
        Value = value;
    }

    private bool Value { get; }

    public static implicit operator string(IncludeDeleted value)
    {
        return value.Value.ToString();
    }

    public static implicit operator bool(IncludeDeleted valueObject)
    {
        return valueObject.Value;
    }
}