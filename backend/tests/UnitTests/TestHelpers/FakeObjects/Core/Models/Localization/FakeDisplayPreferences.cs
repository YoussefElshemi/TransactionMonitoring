using Core.Constants;
using Core.Localization;
using Core.Models.Localization;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.Localization;

public static class FakeDisplayPreferences
{
    public static DisplayPreferences CreateValid()
    {
        return new DisplayPreferences
        {
            Locale = new Locale(LocalizationConstants.FallbackLocale),
            TimeZone = new DisplayTimeZone(LocalizationConstants.FallbackTimeZone)
        };
    }

    public static DisplayPreferences In(string locale, string timeZone)
    {
        return new DisplayPreferences
        {
            Locale = new Locale(locale),
            TimeZone = new DisplayTimeZone(timeZone)
        };
    }

    public static Translator Translator()
    {
        return new Translator(CreateValid());
    }
}
