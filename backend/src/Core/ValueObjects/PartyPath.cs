using Core.Helpers;

namespace Core.ValueObjects;

public readonly record struct PartyPath
{
    public PartyPath(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(PartyPath));

        if (!PayloadPath.IsAddressable(value))
        {
            throw new ArgumentException($"'{value}' is not a path the engine can read.", nameof(PartyPath));
        }

        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(PartyPath valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
