namespace Core.ValueObjects;

public readonly record struct HostedSessionUrl
{
    public HostedSessionUrl(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(HostedSessionUrl));

        if (!Uri.TryCreate(value, UriKind.Absolute, out _))
        {
            throw new ArgumentException("A hosted session URL must be absolute.", nameof(HostedSessionUrl));
        }

        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(HostedSessionUrl valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
