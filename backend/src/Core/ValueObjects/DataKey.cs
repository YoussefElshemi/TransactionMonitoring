namespace Core.ValueObjects;

public readonly record struct DataKey
{
    public const int ByteCount = 32;

    public DataKey(byte[] value)
    {
        ArgumentNullException.ThrowIfNull(value, nameof(DataKey));

        if (value.Length != ByteCount)
        {
            throw new ArgumentOutOfRangeException(nameof(DataKey), $"A data key must be exactly {ByteCount} bytes.");
        }

        Value = value;
    }

    private byte[] Value { get; }

    public static implicit operator byte[](DataKey valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return "[redacted]";
    }
}
