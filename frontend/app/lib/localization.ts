import { API_BASE } from "@/lib/api";

export interface DisplayPreferences {
  locale: string | null;
  timeZone: string | null;
  resolvedLocale: string;
  resolvedTimeZone: string;
  resolvedLanguage: string;
}

export interface LocalizationCatalogue {
  languages: string[];
  locales: string[];
  timeZones: string[];
  defaultLocale: string;
  defaultTimeZone: string;
}

export const fetchPreferences = async (
  signal?: AbortSignal
): Promise<DisplayPreferences> => {
  const response = await fetch(`${API_BASE}/users/me/preferences`, {
    method: "GET",
    headers: { "Content-Type": "application/json" },
    credentials: "include",
    signal,
  });

  if (!response.ok) throw new Error(`Preferences unavailable (${response.status})`);

  return (await response.json()).data as DisplayPreferences;
};

let cataloguePromise: Promise<LocalizationCatalogue> | null = null;

export const fetchLocalizationCatalogue = (): Promise<LocalizationCatalogue> => {
  cataloguePromise ??= (async () => {
    try {
      const response = await fetch(`${API_BASE}/localization/catalogue`, {
        method: "GET",
        headers: { "Content-Type": "application/json" },
        credentials: "include",
      });

      if (!response.ok) throw new Error(`Catalogue unavailable (${response.status})`);

      return (await response.json()).data as LocalizationCatalogue;
    } catch (error) {
      cataloguePromise = null;
      throw error;
    }
  })();

  return cataloguePromise;
};

export {
  FALLBACK_LANGUAGE,
  FALLBACK_LOCALE,
  FALLBACK_TIME_ZONE,
  languageLabel,
  localeLabel,
  preferencesPatch,
  timeZoneLabel,
} from "@/lib/displayPreferences";
export type {
  DisplayPreferencePatch,
  StatedPreferences,
} from "@/lib/displayPreferences";
