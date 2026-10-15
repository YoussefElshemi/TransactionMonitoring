export type DisplayPreferencePatch = Record<string, string | null>;

export interface StatedPreferences {
  locale: string | null;
  timeZone: string | null;
}

export const preferencesPatch = (
  next: StatedPreferences,
  current: StatedPreferences
): DisplayPreferencePatch => {
  const patch: DisplayPreferencePatch = {};
  if (next.locale !== current.locale) patch.locale = next.locale;
  if (next.timeZone !== current.timeZone) patch.timeZone = next.timeZone;
  return patch;
};

export const FALLBACK_LOCALE = "en-GB";

export const FALLBACK_TIME_ZONE = "Etc/UTC";

export const FALLBACK_LANGUAGE = "en";

export const localeLabel = (tag: string, displayLocale: string): string => {
  try {
    const names = new Intl.DisplayNames([displayLocale], { type: "language" });
    const name = names.of(tag);
    return name && name !== tag ? `${name} — ${tag}` : tag;
  } catch {
    return tag;
  }
};

export const languageLabel = (language: string): string => {
  try {
    const names = new Intl.DisplayNames([language], { type: "language" });
    const name = names.of(language);
    if (!name || name === language) return language.toUpperCase();
    return name.charAt(0).toLocaleUpperCase(language) + name.slice(1);
  } catch {
    return language.toUpperCase();
  }
};

export const timeZoneLabel = (zone: string, displayLocale: string): string => {
  const pretty = zone.replace("_", " ").replace("/", " / ");

  try {
    const parts = new Intl.DateTimeFormat(displayLocale, {
      timeZone: zone,
      timeZoneName: "shortOffset",
    }).formatToParts(new Date());

    const offset = parts.find((part) => part.type === "timeZoneName")?.value;
    return offset ? `${pretty} (${offset})` : pretty;
  } catch {
    return pretty;
  }
};
