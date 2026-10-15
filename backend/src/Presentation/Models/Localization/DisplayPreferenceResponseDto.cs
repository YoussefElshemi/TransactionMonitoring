namespace Presentation.Models.Localization;

public record DisplayPreferenceResponseDto
{
    public required string? Locale { get; init; }
    public required string? TimeZone { get; init; }

    public required string ResolvedLocale { get; init; }
    public required string ResolvedTimeZone { get; init; }

    public required string ResolvedLanguage { get; init; }
}
