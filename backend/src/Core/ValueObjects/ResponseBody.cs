namespace Core.ValueObjects;

public readonly record struct ResponseBody
{
    public ResponseBody(string value)
    {
        ArgumentNullException.ThrowIfNull(value, nameof(ResponseBody));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(ResponseBody valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
