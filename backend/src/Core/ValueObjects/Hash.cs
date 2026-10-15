namespace Core.ValueObjects;

public readonly record struct Hash
{
    public const int Length = 64;

    public Hash(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(Hash));

        if (value.Length != Length || !value.All(character => char.IsAsciiDigit(character) || character is >= 'a' and <= 'f'))
        {
            throw new ArgumentOutOfRangeException(nameof(value), $"{nameof(Hash)} must be {Length} lowercase hexadecimal characters.");
        }

        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(Hash valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
