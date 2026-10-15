namespace Core.ValueObjects;

public readonly record struct ApiVersionSummary
{
    public ApiVersionSummary(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(ApiVersionSummary));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(ApiVersionSummary valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
