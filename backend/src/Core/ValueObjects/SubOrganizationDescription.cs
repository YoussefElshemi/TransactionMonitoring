namespace Core.ValueObjects;

public readonly record struct SubOrganizationDescription
{
    public const int MaxLength = 512;

    public SubOrganizationDescription(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(SubOrganizationDescription));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value.Length, MaxLength, nameof(SubOrganizationDescription));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(SubOrganizationDescription valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
