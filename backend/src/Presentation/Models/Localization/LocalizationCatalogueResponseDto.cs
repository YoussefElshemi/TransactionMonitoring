namespace Presentation.Models.Localization;

public record LocalizationCatalogueResponseDto
{
    public required IReadOnlyList<string> Languages { get; init; }

    public required IReadOnlyList<string> Locales { get; init; }
    public required IReadOnlyList<string> TimeZones { get; init; }

    public required string DefaultLocale { get; init; }
    public required string DefaultTimeZone { get; init; }
}
