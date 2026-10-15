using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.Localization;

public record DisplayPreferences
{
    public required Locale Locale { get; init; }
    public required DisplayTimeZone TimeZone { get; init; }
    public Language Language => Locale.Language;
}
