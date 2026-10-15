using System.Globalization;
using Core.Enums;

namespace Core.ValueObjects;

public readonly record struct Locale
{
    public const int MaxLength = 16;

    public Locale(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(Locale));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value.Length, MaxLength, nameof(Locale));

        if (!TryResolve(value, out var culture))
        {
            throw new ArgumentException($"'{value}' is not a language tag this host knows.", nameof(Locale));
        }

        Value = culture.Name;
    }

    private string Value { get; }

    public Language Language =>
        Enum.TryParse<Language>(PrimarySubtag(Value), true, out var language) ? language : Language.En;

    public CultureInfo Culture()
    {
        TryResolve(Value, out var culture);

        return culture;
    }

    public static bool IsKnown(string value)
    {
        return !string.IsNullOrWhiteSpace(value) && value.Length <= MaxLength && TryResolve(value, out _);
    }

    public static implicit operator string(Locale valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }

    private static string PrimarySubtag(string value)
    {
        var separator = value.IndexOf('-', StringComparison.Ordinal);

        return separator < 0 ? value : value[..separator];
    }

    private static bool TryResolve(string value, out CultureInfo culture)
    {
        try
        {
            // Not the forgiving overload: with ICU predefined-cultures-only off it would manufacture a culture from an invented tag.
            culture = CultureInfo.GetCultureInfo(value, predefinedOnly: true);

            return !culture.Equals(CultureInfo.InvariantCulture);
        }
        catch (CultureNotFoundException)
        {
            culture = CultureInfo.InvariantCulture;

            return false;
        }
    }
}
