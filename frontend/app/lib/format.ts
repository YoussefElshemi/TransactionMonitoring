// From the leaf rather than from `lib/localization`: that module reaches
// `lib/api`, which imports `@/types` — a `.tsx` file the `node --test` harness
// refuses to load. Everything downstream of this module would go untestable
// with it, which is most of `lib`.
import { FALLBACK_LOCALE, FALLBACK_TIME_ZONE } from "@/lib/displayPreferences";

export const DOCUMENT_LOCALE = "en-GB";

let readerLocale = FALLBACK_LOCALE;
let readerTimeZone = FALLBACK_TIME_ZONE;

export const publishReaderPreferences = (
  locale: string,
  timeZone: string
): void => {
  readerLocale = locale;
  readerTimeZone = timeZone;
};

export const currentReaderPreferences = (): {
  locale: string;
  timeZone: string;
} => ({ locale: readerLocale, timeZone: readerTimeZone });

const dateTimeFormatters = new Map<string, Intl.DateTimeFormat>();
const numberFormatters = new Map<string, Intl.NumberFormat>();
const relativeFormatters = new Map<string, Intl.RelativeTimeFormat>();

const dateTimeFormatter = (
  locale: string,
  timeZone: string,
  options: Intl.DateTimeFormatOptions
): Intl.DateTimeFormat => {
  const key = `${locale}|${timeZone}|${JSON.stringify(options)}`;
  const cached = dateTimeFormatters.get(key);
  if (cached) return cached;

  let formatter: Intl.DateTimeFormat;
  try {
    formatter = new Intl.DateTimeFormat(locale, { ...options, timeZone });
  } catch {
    formatter = new Intl.DateTimeFormat(FALLBACK_LOCALE, {
      ...options,
      timeZone: FALLBACK_TIME_ZONE,
    });
  }

  dateTimeFormatters.set(key, formatter);
  return formatter;
};

const numberFormatter = (
  locale: string,
  options: Intl.NumberFormatOptions
): Intl.NumberFormat => {
  const key = `${locale}|${JSON.stringify(options)}`;
  const cached = numberFormatters.get(key);
  if (cached) return cached;

  let formatter: Intl.NumberFormat;
  try {
    formatter = new Intl.NumberFormat(locale, options);
  } catch {
    formatter = new Intl.NumberFormat(FALLBACK_LOCALE, options);
  }

  numberFormatters.set(key, formatter);
  return formatter;
};

const relativeFormatter = (locale: string): Intl.RelativeTimeFormat => {
  const cached = relativeFormatters.get(locale);
  if (cached) return cached;

  let formatter: Intl.RelativeTimeFormat;
  try {
    formatter = new Intl.RelativeTimeFormat(locale, { numeric: "auto" });
  } catch {
    formatter = new Intl.RelativeTimeFormat(FALLBACK_LOCALE, { numeric: "auto" });
  }

  relativeFormatters.set(locale, formatter);
  return formatter;
};

export type InstantStyle = "dateTime" | "date" | "time" | "precise" | "deadline";

const INSTANT_OPTIONS: Record<InstantStyle, Intl.DateTimeFormatOptions> = {
  dateTime: {
    day: "2-digit",
    month: "short",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  },
  date: { day: "2-digit", month: "short", year: "numeric" },
  time: { hour: "2-digit", minute: "2-digit" },
  precise: {
    day: "2-digit",
    month: "short",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
    second: "2-digit",
  },
  deadline: {
    day: "2-digit",
    month: "short",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
    timeZoneName: "shortGeneric",
  },
};

export const formatInstant = (
  value: string | Date,
  locale: string,
  timeZone: string,
  style: InstantStyle = "dateTime"
): string => {
  const parsed = value instanceof Date ? value : new Date(value);
  if (Number.isNaN(parsed.getTime())) {
    return value instanceof Date ? "—" : value;
  }

  return dateTimeFormatter(locale, timeZone, INSTANT_OPTIONS[style]).format(parsed);
};

export const utcLabel = (value: string | Date): string => {
  const parsed = value instanceof Date ? value : new Date(value);
  if (Number.isNaN(parsed.getTime())) return "";

  return `${parsed.toISOString().replace("T", " ").replace(/\.\d+Z$/, "Z")} · UTC`;
};

export const formatWallClock = (
  value: Date,
  locale: string,
  style: "date" | "time" | "dateTime" = "date"
): string => {
  if (Number.isNaN(value.getTime())) return "—";

  const options: Intl.DateTimeFormatOptions =
    style === "time"
      ? { hour: "2-digit", minute: "2-digit", hour12: false }
      : style === "dateTime"
        ? {
            day: "2-digit",
            month: "short",
            year: "numeric",
            hour: "2-digit",
            minute: "2-digit",
            hour12: false,
          }
        : { day: "2-digit", month: "short", year: "numeric" };

  const key = `wall|${locale}|${JSON.stringify(options)}`;
  let formatter = dateTimeFormatters.get(key);
  if (!formatter) {
    try {
      formatter = new Intl.DateTimeFormat(locale, options);
    } catch {
      formatter = new Intl.DateTimeFormat(FALLBACK_LOCALE, options);
    }
    dateTimeFormatters.set(key, formatter);
  }

  return formatter.format(value);
};

const RELATIVE_UNITS: [Intl.RelativeTimeFormatUnit, number][] = [
  ["year", 31_536_000_000],
  ["month", 2_592_000_000],
  ["week", 604_800_000],
  ["day", 86_400_000],
  ["hour", 3_600_000],
  ["minute", 60_000],
  ["second", 1_000],
];

export const formatRelative = (
  value: string | Date,
  locale: string,
  now: Date = new Date()
): string => {
  const parsed = value instanceof Date ? value : new Date(value);
  if (Number.isNaN(parsed.getTime())) return typeof value === "string" ? value : "—";

  const elapsed = parsed.getTime() - now.getTime();
  const formatter = relativeFormatter(locale);

  for (const [unit, milliseconds] of RELATIVE_UNITS) {
    if (Math.abs(elapsed) >= milliseconds || unit === "second") {
      return formatter.format(Math.trunc(elapsed / milliseconds), unit);
    }
  }

  return formatter.format(0, "second");
};

export const formatCount = (value: number, locale: string): string =>
  numberFormatter(locale, { maximumFractionDigits: 0 }).format(value);

export const formatDecimal = (
  value: number,
  locale: string,
  options: { minimumFractionDigits?: number; maximumFractionDigits?: number } = {}
): string =>
  numberFormatter(locale, {
    minimumFractionDigits: options.minimumFractionDigits ?? 0,
    maximumFractionDigits: options.maximumFractionDigits ?? 2,
  }).format(value);

export const formatPercent = (
  fraction: number,
  locale: string,
  maximumFractionDigits = 1
): string =>
  numberFormatter(locale, {
    style: "percent",
    maximumFractionDigits,
  }).format(fraction);

export const formatBytes = (bytes: number, locale: string): string => {
  const units = ["B", "KB", "MB", "GB"];
  let value = bytes;
  let unit = 0;

  while (value >= 1024 && unit < units.length - 1) {
    value /= 1024;
    unit += 1;
  }

  return `${formatDecimal(value, locale, {
    maximumFractionDigits: unit === 0 ? 0 : 1,
  })} ${units[unit]}`;
};
