namespace Core.ValueObjects;

public readonly record struct HasChanged
{
    public HasChanged(bool value)
    {
        Value = value;
    }

    private bool Value { get; }

    public static implicit operator bool(HasChanged valueObject)
    {
        return valueObject.Value;
    }
}