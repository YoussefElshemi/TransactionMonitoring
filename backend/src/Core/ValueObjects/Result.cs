namespace Core.ValueObjects;

public readonly record struct Result
{
    public Result(bool value)
    {
        Value = value;
    }

    private bool Value { get; }

    public static implicit operator string(Result value)
    {
        return value.Value.ToString();
    }

    public static implicit operator bool(Result valueObject)
    {
        return valueObject.Value;
    }
}