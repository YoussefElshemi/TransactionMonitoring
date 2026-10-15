"use client";

import { useMemo } from "react";

import { useLocalization } from "@/context/LocalizationContext";
import {
  InstantStyle,
  formatBytes,
  formatCount,
  formatDecimal,
  formatInstant,
  formatPercent,
  formatRelative,
} from "@/lib/format";

export const useFormat = () => {
  const { locale, timeZone } = useLocalization();

  return useMemo(
    () => ({
      locale,
      timeZone,

      instant: (value: string | Date, style: InstantStyle = "dateTime") =>
        formatInstant(value, locale, timeZone, style),

      deadline: (value: string | Date) =>
        formatInstant(value, locale, timeZone, "deadline"),

      relative: (value: string | Date, now?: Date) =>
        formatRelative(value, locale, now),

      count: (value: number) => formatCount(value, locale),

      decimal: (
        value: number,
        options?: { minimumFractionDigits?: number; maximumFractionDigits?: number }
      ) => formatDecimal(value, locale, options),

      percent: (fraction: number, maximumFractionDigits?: number) =>
        formatPercent(fraction, locale, maximumFractionDigits),

      bytes: (value: number) => formatBytes(value, locale),
    }),
    [locale, timeZone]
  );
};
