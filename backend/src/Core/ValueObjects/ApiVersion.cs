using System.Globalization;

namespace Core.ValueObjects;

public readonly record struct ApiVersion : IComparable<ApiVersion>
{
    public const string Format = "yyyy-MM-dd";

    public ApiVersion(string value)
    {
        if (!IsWellFormed(value))
        {
            throw new ArgumentException($"An API version is a date written as {Format}.", nameof(ApiVersion));
        }

        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(ApiVersion valueObject)
    {
        return valueObject.Value;
    }

    public static bool TryParse(string? value, out ApiVersion apiVersion)
    {
        if (!IsWellFormed(value))
        {
            apiVersion = default;

            return false;
        }

        apiVersion = new ApiVersion(value!);

        return true;
    }

    public int CompareTo(ApiVersion other)
    {
        return string.CompareOrdinal(Value, other.Value);
    }

    public override string ToString()
    {
        return Value;
    }

    private static bool IsWellFormed(string? value)
    {
        return DateOnly.TryParseExact(value, Format, CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
    }
}
