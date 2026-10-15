import type { ComponentUptimeDay } from "@/types";
import {
  readUptimeDay,
  uptimeDayTitle,
  uptimeWindow,
  type UptimeDayReading,
} from "@/lib/status";
import { cn } from "@/lib/utils";

const READING_CLASSES: Record<UptimeDayReading, string> = {
  up: "bg-settled-500",
  degraded: "bg-attention-500",
  down: "bg-critical-solid",
  unknown:
    "bg-slate-200 bg-[repeating-linear-gradient(45deg,transparent,transparent_2px,hsl(var(--slate-400)/0.55)_2px,hsl(var(--slate-400)/0.55)_4px)]",
};

const PADDED_TITLE =
  "No data. This day falls before this component's recorded history, so nothing is known about it. It is not being reported as up.";

export function UptimeBar({
  history,
  days = 90,
  className,
}: {
  history: readonly ComponentUptimeDay[];
  days?: number;
  className?: string;
}) {
  const window = uptimeWindow(history, days);

  return (
    <div
      className={cn("flex h-8 w-full items-stretch gap-px", className)}
      role="img"
      aria-label={`Uptime for the last ${days} days`}
    >
      {window.map((day, index) => {
        const reading: UptimeDayReading =
          day === null ? "unknown" : readUptimeDay(day);

        return (
          <span
            key={day?.date ?? `pad-${index}`}
            title={day === null ? PADDED_TITLE : uptimeDayTitle(day)}
            className={cn(
              "min-w-0 flex-1 rounded-[1px] transition-opacity hover:opacity-70",
              READING_CLASSES[reading]
            )}
          />
        );
      })}
    </div>
  );
}

export function UptimeLegend({ className }: { className?: string }) {
  const entries: { reading: UptimeDayReading; label: string }[] = [
    { reading: "up", label: "Operational" },
    { reading: "degraded", label: "Degraded" },
    { reading: "down", label: "Outage" },
    { reading: "unknown", label: "No data — not probed" },
  ];

  return (
    <div className={cn("flex flex-wrap items-center gap-x-5 gap-y-2", className)}>
      {entries.map((entry) => (
        <span key={entry.reading} className="flex items-center gap-1.5">
          <span
            className={cn(
              "size-2.5 rounded-[1px]",
              READING_CLASSES[entry.reading]
            )}
          />
          <span className="font-mono text-[11px] uppercase tracking-widest text-slate-400">
            {entry.label}
          </span>
        </span>
      ))}
    </div>
  );
}
