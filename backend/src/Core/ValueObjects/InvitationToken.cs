namespace Core.ValueObjects;

public readonly record struct InvitationToken
{
    public const int MaxLength = 128;

    public InvitationToken(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(InvitationToken));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value.Length, MaxLength, nameof(InvitationToken));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(InvitationToken valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
