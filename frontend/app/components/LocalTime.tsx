"use client";

import { useLocalization } from "@/context/LocalizationContext";
import { InstantStyle, formatInstant, utcLabel } from "@/lib/format";

export function LocalTime({
  value,
  dateOnly = false,
  deadline = false,
  precise = false,
  className,
}: {
  value: string;
  dateOnly?: boolean;
  deadline?: boolean;
  precise?: boolean;
  className?: string;
}) {
  const { locale, timeZone } = useLocalization();

  const parsed = new Date(value);

  if (Number.isNaN(parsed.getTime())) {
    return <span className={className}>{value}</span>;
  }

  const style: InstantStyle = deadline
    ? "deadline"
    : precise
      ? "precise"
      : dateOnly
        ? "date"
        : "dateTime";

  return (
    <time
      dateTime={parsed.toISOString()}
      title={`${utcLabel(parsed)} · shown in ${timeZone}`}
      className={className}
      suppressHydrationWarning
    >
      {formatInstant(parsed, locale, timeZone, style)}
    </time>
  );
}
