using Core.Models.Common;
using Core.ValueObjects;

namespace Presentation.Mappers;

public static class LocalizationPatchMapper
{
    public static Patch<Locale?> ToLocale(this Patch<string?> patch)
    {
        if (!patch.IsSet)
        {
            return default;
        }

        return Patch<Locale?>.Set(string.IsNullOrWhiteSpace(patch.Value) ? null : new Locale(patch.Value));
    }

    public static Patch<DisplayTimeZone?> ToDisplayTimeZone(this Patch<string?> patch)
    {
        if (!patch.IsSet)
        {
            return default;
        }

        return Patch<DisplayTimeZone?>.Set(string.IsNullOrWhiteSpace(patch.Value) ? null : new DisplayTimeZone(patch.Value));
    }
}
