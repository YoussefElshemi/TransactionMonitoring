import {
  de,
  deAT,
  enAU,
  enCA,
  enGB,
  enIE,
  enIN,
  enNZ,
  enUS,
  enZA,
  es,
  fr,
  frCA,
  frCH,
  nl,
  nlBE,
} from "react-day-picker/locale";
import type { Locale } from "react-day-picker";

import { FALLBACK_LOCALE } from "@/lib/displayPreferences";

const LOCALES: Record<string, Locale> = {
  "en-au": enAU,
  "en-ca": enCA,
  "en-gb": enGB,
  "en-ie": enIE,
  "en-in": enIN,
  "en-nz": enNZ,
  "en-us": enUS,
  "en-za": enZA,
  en: enGB,
  "fr-ca": frCA,
  "fr-ch": frCH,
  fr,
  "de-at": deAT,
  de,
  es,
  "nl-be": nlBE,
  nl,
};

export const calendarLocale = (locale: string): Locale => {
  const tag = locale.trim().toLowerCase();

  return (
    LOCALES[tag] ??
    LOCALES[tag.split("-")[0]] ??
    LOCALES[FALLBACK_LOCALE.toLowerCase()]
  );
};
