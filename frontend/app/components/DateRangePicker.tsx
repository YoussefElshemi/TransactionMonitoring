"use client";

import { useState } from "react";
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@/components/ui/popover";
import { Button } from "@/components/ui/button";
import { Calendar } from "@/components/ui/calendar";
import type { DateRange } from "react-day-picker";
import {
  addMonths,
  endOfMonth,
  format,
  isValid,
  parse,
  startOfMonth,
  subDays,
} from "date-fns";
import { CalendarDays } from "lucide-react";
import { cn } from "@/lib/utils";

import { useLocalization, useTranslation } from "@/context/LocalizationContext";
import { formatWallClock } from "@/lib/format";

const presets = [
  {
    key: "dateRange.preset.today",
    range: (): DateRange => {
      const d = new Date();
      return { from: d, to: d };
    },
  },
  {
    key: "dateRange.preset.yesterday",
    range: (): DateRange => {
      const d = subDays(new Date(), 1);
      return { from: d, to: d };
    },
  },
  {
    key: "dateRange.preset.last7Days",
    range: (): DateRange => ({ from: subDays(new Date(), 6), to: new Date() }),
  },
  {
    key: "dateRange.preset.last30Days",
    range: (): DateRange => ({ from: subDays(new Date(), 29), to: new Date() }),
  },
  {
    key: "dateRange.preset.thisMonth",
    range: (): DateRange => ({ from: startOfMonth(new Date()), to: new Date() }),
  },
  {
    key: "dateRange.preset.lastMonth",
    range: (): DateRange => ({
      from: startOfMonth(addMonths(new Date(), -1)),
      to: endOfMonth(addMonths(new Date(), -1)),
    }),
  },
];

interface DateRangePickerProps {
  value: DateRange | undefined;
  onChange: (range: DateRange | undefined) => void;
  className?: string;
  disabled?: boolean;
  emptyLabel?: string;
  clearLabel?: string;
  align?: "start" | "center" | "end";
  "aria-label"?: string;
}

export function DateRangePicker({
  value,
  onChange,
  className,
  disabled = false,
  emptyLabel,
  clearLabel,
  align = "end",
  "aria-label": ariaLabel,
}: DateRangePickerProps) {
  const [open, setOpen] = useState(false);
  const { locale } = useLocalization();
  const t = useTranslation();

  const label =
    value?.from
      ? value.to
        ? `${formatWallClock(value.from, locale)} – ${formatWallClock(value.to, locale)}`
        : formatWallClock(value.from, locale)
      : (emptyLabel ?? t("dateRange.allTime"));

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <Button
          type="button"
          variant="outline"
          disabled={disabled}
          aria-label={ariaLabel}
          className={cn(
            "justify-start gap-2 font-normal text-slate-700",
            className
          )}
        >
          <CalendarDays className="text-slate-400" />
          <span className="truncate">{label}</span>
        </Button>
      </PopoverTrigger>
      <PopoverContent
        align={align}
        className="flex w-auto max-w-[calc(100vw-2rem)] flex-col gap-0 overflow-auto p-0 sm:flex-row"
      >
        <div className="flex gap-0.5 overflow-x-auto border-b border-slate-200 p-3 sm:w-36 sm:flex-col sm:border-b-0 sm:border-r">
          {presets.map((preset) => (
            <Button
              key={preset.key}
              type="button"
              variant="ghost"
              size="sm"
              className="shrink-0 justify-start text-slate-600 hover:text-slate-900"
              onClick={() => {
                onChange(preset.range());
                setOpen(false);
              }}
            >
              {t(preset.key)}
            </Button>
          ))}
          <Button
            type="button"
            variant="ghost"
            size="sm"
            className="shrink-0 justify-start text-slate-600 hover:text-slate-900"
            onClick={() => {
              onChange(undefined);
              setOpen(false);
            }}
          >
            {clearLabel ?? t("dateRange.clear")}
          </Button>
        </div>
        <div>
          <Calendar
            mode="range"
            selected={value}
            onSelect={onChange}
            numberOfMonths={1}
            disabled={{ after: new Date() }}
          />
        </div>
      </PopoverContent>
    </Popover>
  );
}

interface DateRangeFieldProps
  extends Omit<DateRangePickerProps, "value" | "onChange"> {
  from: string;
  to: string;
  onChange: (from: string, to: string) => void;
}

const parseDateField = (value: string): Date | undefined => {
  if (!value) return undefined;
  const parsed = parse(value, "yyyy-MM-dd", new Date());
  return isValid(parsed) ? parsed : undefined;
};

export function DateRangeField({
  from,
  to,
  onChange,
  ...props
}: DateRangeFieldProps) {
  const fromDate = parseDateField(from);
  const toDate = parseDateField(to);
  const value = fromDate || toDate ? { from: fromDate, to: toDate } : undefined;

  return (
    <DateRangePicker
      {...props}
      value={value}
      onChange={(next) =>
        onChange(
          next?.from ? format(next.from, "yyyy-MM-dd") : "",
          next?.to ? format(next.to, "yyyy-MM-dd") : ""
        )
      }
    />
  );
}
