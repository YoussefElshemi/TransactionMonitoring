namespace Core.ValueObjects;

public readonly record struct Password
{
    public const int MaxLength = 256;

    public Password(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(Password));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value.Length, MaxLength, nameof(Password));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(Password valueObject)
    {
        return valueObject.Value;
    }

    // Never the password: this ends up in logs and exception messages.
    public override string ToString()
    {
        return "********";
    }
}
