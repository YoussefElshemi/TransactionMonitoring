namespace Core.ValueObjects;

public readonly record struct HasActiveDeployment
{
    public HasActiveDeployment(bool value)
    {
        Value = value;
    }

    private bool Value { get; }

    public static implicit operator string(HasActiveDeployment value)
    {
        return value.Value.ToString();
    }

    public static implicit operator bool(HasActiveDeployment valueObject)
    {
        return valueObject.Value;
    }
}
