"use client";

import { useId } from "react";

import type { MonthPoint } from "@/lib/revenue";
import { monthStart } from "@/lib/revenue";
import { useLocalization, useTranslation } from "@/context/LocalizationContext";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";

const yearsBack = (count: number): number[] => {
  const current = new Date().getFullYear();

  return Array.from({ length: count }, (_, index) => current - index);
};

export const monthName = (month: number, locale: string): string =>
  monthStart({ month, year: 2020 }).toLocaleDateString(locale, {
    month: "long",
  });

export const monthLabel = (point: MonthPoint, locale: string): string =>
  monthStart(point).toLocaleDateString(locale, {
    month: "long",
    year: "numeric",
  });

export const lastCompleteMonth = (now: Date = new Date()): MonthPoint => {
  const previous = new Date(now.getFullYear(), now.getMonth() - 1, 1);

  return { month: previous.getMonth() + 1, year: previous.getFullYear() };
};

export const monthsBefore = (point: MonthPoint, count: number): MonthPoint => {
  const start = new Date(point.year, point.month - 1 - (count - 1), 1);

  return { month: start.getMonth() + 1, year: start.getFullYear() };
};

export function MonthField({
  label,
  value,
  onChange,
  disabled = false,
}: {
  label: string;
  value: MonthPoint;
  onChange: (point: MonthPoint) => void;
  disabled?: boolean;
}) {
  const { locale } = useLocalization();
  const labelId = useId();

  return (
    <div className="flex flex-col gap-1">
      <span id={labelId} className="font-mono text-[11px] uppercase tracking-widest text-slate-500">
        {label}
      </span>
      <div className="flex items-center gap-1.5">
        <Select
          value={String(value.month)}
          disabled={disabled}
          onValueChange={(month) => onChange({ ...value, month: Number(month) })}
        >
          <SelectTrigger className="h-8 w-36 text-xs" aria-labelledby={labelId}>
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            {Array.from({ length: 12 }, (_, index) => index + 1).map((month) => (
              <SelectItem key={month} value={String(month)}>
                {monthName(month, locale)}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>

        <Select
          value={String(value.year)}
          disabled={disabled}
          onValueChange={(year) => onChange({ ...value, year: Number(year) })}
        >
          <SelectTrigger className="h-8 w-24 text-xs" aria-labelledby={labelId}>
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            {yearsBack(6).map((year) => (
              <SelectItem key={year} value={String(year)}>
                {year}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>
    </div>
  );
}

export function MonthRangeFields({
  from,
  to,
  onFrom,
  onTo,
  disabled = false,
  invalid = false,
}: {
  from: MonthPoint;
  to: MonthPoint;
  onFrom: (point: MonthPoint) => void;
  onTo: (point: MonthPoint) => void;
  disabled?: boolean;
  invalid?: boolean;
}) {
  const t = useTranslation();

  return (
    <div className="flex flex-wrap items-end gap-3">
      <MonthField
        label={t("revenue.from")}
        value={from}
        onChange={onFrom}
        disabled={disabled}
      />
      <MonthField
        label={t("revenue.to")}
        value={to}
        onChange={onTo}
        disabled={disabled}
      />
      {invalid && (
        <p role="alert" className="pb-1 text-xs text-critical-700">
          {t("revenue.rangeRunsBackwards")}
        </p>
      )}
    </div>
  );
}
