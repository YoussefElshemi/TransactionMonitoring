using Core.Configs;
using Core.Constants;
using Core.Models.Localization;
using Core.ValueObjects;

namespace Core.Helpers;

public static class DisplayPreferenceResolver
{
    public static DisplayPreferences Resolve(
        Locale? readerLocale,
        DisplayTimeZone? readerTimeZone,
        Locale? organizationLocale,
        DisplayTimeZone? organizationTimeZone,
        LocalizationConfig localizationConfig)
    {
        return new DisplayPreferences
        {
            Locale = readerLocale ?? organizationLocale ?? Fallback(localizationConfig),
            TimeZone = readerTimeZone ?? organizationTimeZone ?? FallbackTimeZone(localizationConfig)
        };
    }

    public static Locale Fallback(LocalizationConfig localizationConfig)
    {
        return Locale.IsKnown(localizationConfig.DefaultLocale)
            ? new Locale(localizationConfig.DefaultLocale)
            : new Locale(LocalizationConstants.FallbackLocale);
    }

    public static DisplayTimeZone FallbackTimeZone(LocalizationConfig localizationConfig)
    {
        return DisplayTimeZone.IsKnown(localizationConfig.DefaultTimeZone)
            ? new DisplayTimeZone(localizationConfig.DefaultTimeZone)
            : new DisplayTimeZone(LocalizationConstants.FallbackTimeZone);
    }
}
