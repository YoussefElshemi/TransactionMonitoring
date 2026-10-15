namespace Core.Configs;

public record LocalizationConfig
{
    public string DefaultLocale { get; init; } = "en-GB";
    public string DefaultTimeZone { get; init; } = "Etc/UTC";
}
