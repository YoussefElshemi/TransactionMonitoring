"use client";

import { useEffect, useId, useMemo, useState } from "react";
import { format, isValid, parse } from "date-fns";
import { CalendarDays } from "lucide-react";

import { Calendar } from "@/components/ui/calendar";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@/components/ui/popover";
import { cn } from "@/lib/utils";

import { useLocalization, useTranslation } from "@/context/LocalizationContext";
import { formatWallClock } from "@/lib/format";

const LOCAL_DATE_TIME = "yyyy-MM-dd'T'HH:mm";

const parseLocalDateTime = (value: string): Date | undefined => {
  if (!value) return undefined;

  const parsed = parse(value.slice(0, 16), LOCAL_DATE_TIME, new Date());
  return isValid(parsed) ? parsed : undefined;
};

interface DateTimePickerProps {
  value: string;
  onChange: (value: string) => void;
  className?: string;
  disabled?: boolean;
  placeholder?: string;
  align?: "start" | "center" | "end";
  "aria-label"?: string;
}

export function DateTimePicker({
  value,
  onChange,
  className,
  disabled = false,
  placeholder,
  align = "start",
  "aria-label": ariaLabel,
}: DateTimePickerProps) {
  const t = useTranslation();
  const timeInputId = useId();
  const { locale } = useLocalization();
  const selected = useMemo(() => parseLocalDateTime(value), [value]);
  const [open, setOpen] = useState(false);
  const [draftDate, setDraftDate] = useState<Date | undefined>(selected);
  const [draftTime, setDraftTime] = useState(
    selected ? format(selected, "HH:mm") : ""
  );

  useEffect(() => {
    if (open) {
      setDraftDate(selected);
      setDraftTime(selected ? format(selected, "HH:mm") : "");
    }
  }, [open, selected]);

  const label = selected
    ? formatWallClock(selected, locale, "dateTime")
    : (placeholder ?? t("dateTimePicker.placeholder"));

  const apply = () => {
    if (!draftDate || !draftTime) return;
    onChange(`${format(draftDate, "yyyy-MM-dd")}T${draftTime}`);
    setOpen(false);
  };

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <Button
          type="button"
          variant="outline"
          disabled={disabled}
          aria-label={ariaLabel}
          className={cn(
            "w-full justify-start gap-2 font-normal text-slate-700",
            !selected && "text-slate-500",
            className
          )}
        >
          <CalendarDays className="text-slate-400" />
          <span className="truncate">{label}</span>
        </Button>
      </PopoverTrigger>
      <PopoverContent
        align={align}
        className="w-auto max-w-[calc(100vw-2rem)] overflow-auto p-0"
      >
        <Calendar
          mode="single"
          selected={draftDate}
          onSelect={(date) => {
            setDraftDate(date);
            if (date && !draftTime) setDraftTime(format(new Date(), "HH:mm"));
          }}
        />
        <div className="space-y-3 border-t border-slate-200 p-3">
          <div className="flex items-center gap-3">
            <label
              htmlFor={timeInputId}
              className="font-mono text-xs text-slate-500"
            >
              {t("dateTimePicker.time")}
            </label>
            <Input
              id={timeInputId}
              type="time"
              value={draftTime}
              onChange={(event) => setDraftTime(event.target.value)}
              className="h-9 w-32"
            />
          </div>
          <div className="flex items-center justify-between gap-3">
            <Button
              type="button"
              variant="ghost"
              size="sm"
              className="text-slate-600"
              onClick={() => {
                onChange("");
                setOpen(false);
              }}
            >
              {t("dateTimePicker.clear")}
            </Button>
            <Button
              type="button"
              size="sm"
              disabled={!draftDate || !draftTime}
              onClick={apply}
            >
              {t("dateTimePicker.apply")}
            </Button>
          </div>
        </div>
      </PopoverContent>
    </Popover>
  );
}
