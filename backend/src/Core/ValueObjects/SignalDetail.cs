namespace Core.ValueObjects;

public readonly record struct SignalDetail
{
    public SignalDetail(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(SignalDetail));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(SignalDetail valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
