namespace Core.ValueObjects;

public readonly record struct CreditNoteReason
{
    public const int MaxLength = 500;

    public CreditNoteReason(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(CreditNoteReason));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value.Length, MaxLength, nameof(CreditNoteReason));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(CreditNoteReason valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
