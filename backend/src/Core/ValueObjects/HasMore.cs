namespace Core.ValueObjects;

public readonly record struct HasMore
{
    public HasMore(bool value)
    {
        Value = value;
    }

    private bool Value { get; }

    public static implicit operator string(HasMore value)
    {
        return value.Value.ToString();
    }

    public static implicit operator bool(HasMore valueObject)
    {
        return valueObject.Value;
    }
}