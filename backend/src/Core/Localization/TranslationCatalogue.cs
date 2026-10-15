using Core.Enums;
using Core.Localization.Translations;

namespace Core.Localization;

public static class TranslationCatalogue
{
    private static readonly IReadOnlyDictionary<Language, IReadOnlyDictionary<string, string>> Languages =
        new Dictionary<Language, IReadOnlyDictionary<string, string>>
        {
            [Language.En] = EnglishTranslations.Values,
            [Language.Fr] = FrenchTranslations.Values,
            [Language.De] = GermanTranslations.Values,
            [Language.Es] = SpanishTranslations.Values,
            [Language.Nl] = DutchTranslations.Values
        };

    public static string Find(Language language, string key)
    {
        if (Languages.TryGetValue(language, out var translations) && translations.TryGetValue(key, out var translated))
        {
            return translated;
        }

        return EnglishTranslations.Values.TryGetValue(key, out var fallback) ? fallback : key;
    }

    public static IReadOnlyDictionary<string, string> For(Language language)
    {
        return Languages.TryGetValue(language, out var translations) ? translations : EnglishTranslations.Values;
    }

    public static IReadOnlyList<Language> Supported => [.. Languages.Keys];
}
