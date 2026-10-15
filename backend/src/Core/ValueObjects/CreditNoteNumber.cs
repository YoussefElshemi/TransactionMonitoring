namespace Core.ValueObjects;

public readonly record struct CreditNoteNumber
{
    public const int MaxLength = 32;

    public CreditNoteNumber(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(CreditNoteNumber));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value.Length, MaxLength, nameof(CreditNoteNumber));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(CreditNoteNumber valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
