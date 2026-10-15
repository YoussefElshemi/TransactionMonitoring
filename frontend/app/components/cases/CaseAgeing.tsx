"use client";

import { useEffect, useState } from "react";
import { ArrowUpRight } from "lucide-react";

import { CaseAgeBucket, CaseAgeing } from "@/types";
import { cn } from "@/lib/utils";
import {
  BUCKET_HINTS,
  bucketLabel,
  deadlineTitle,
  duePhrase,
  durationLabel,
  tickAgeing,
} from "@/lib/ageing";

import { useLocalization, useTranslation } from "@/context/LocalizationContext";
import { useFormat } from "@/lib/useFormat";

const bucketStyles: Record<string, string> = {
  Fresh: "border-transparent bg-slate-100 text-slate-500",
  Ageing: "border-attention-200 bg-attention-50 text-attention-700",
  DueSoon:
    "border-elevated-200 bg-elevated-100 text-elevated-700 ring-1 ring-inset ring-elevated-200",
  Breached:
    "border-critical-solid bg-critical-solid text-critical-solid-foreground",
};

export function AgeBucketBadge({
  bucket,
  className,
}: {
  bucket: CaseAgeBucket | (string & {});
  className?: string;
}) {
  return (
    <span
      className={cn(
        "inline-flex items-center rounded-md border px-2 py-0.5 font-mono text-[11px] font-medium",
        bucketStyles[bucket] ?? bucketStyles.Fresh,
        className
      )}
      title={BUCKET_HINTS[bucket as CaseAgeBucket]}
    >
      {bucketLabel(bucket)}
    </span>
  );
}

export function useElapsedSince(anchor: unknown, intervalMs = 1000): number {
  const [elapsed, setElapsed] = useState(0);

  useEffect(() => {
    setElapsed(0);
    const startedAt = Date.now();
    const timer = setInterval(
      () => setElapsed(Date.now() - startedAt),
      intervalMs
    );
    return () => clearInterval(timer);
  }, [anchor, intervalMs]);

  return elapsed;
}

export function CaseAgeingCell({
  ageing,
  dueAt,
  frozen,
  elapsedMilliseconds = 0,
  className,
}: {
  ageing: CaseAgeing;
  dueAt: string;
  frozen: boolean;
  elapsedMilliseconds?: number;
  className?: string;
}) {
  const { locale, timeZone } = useLocalization();
  const live = tickAgeing(ageing, elapsedMilliseconds, frozen);
  const phrase = duePhrase(live.timeToDueInMilliseconds);

  return (
    <div className={cn("space-y-1", className)}>
      <AgeBucketBadge bucket={live.bucket} />
      <p
        className={cn(
          "font-mono text-xs tabular-nums",
          phrase.overdue ? "text-critical-700" : "text-slate-600"
        )}
        title={deadlineTitle(dueAt, locale, timeZone)}
      >
        {phrase.short}
      </p>
      {frozen && (
        <p className="font-mono text-[10px] uppercase tracking-widest text-slate-400">
          at close
        </p>
      )}
    </div>
  );
}

export function EscalatedMark({
  escalatedAt,
  className,
}: {
  escalatedAt: string;
  className?: string;
}) {
  const t = useTranslation();
  const format = useFormat();

  return (
    <span
      className={cn(
        "inline-flex items-center gap-1 font-mono text-[11px] uppercase tracking-widest text-elevated-700",
        className
      )}
      title={`A rule escalated this case on ${format.instant(
        escalatedAt
      )}. That is not the same as the Escalated status, which an analyst sets by hand — the sweep leaves the status exactly as it was.`}
    >
      <ArrowUpRight className="size-3.5" />
      {t("common.rule")}
    </span>
  );
}

export function CaseAgeLabel({
  ageing,
  frozen,
  elapsedMilliseconds = 0,
}: {
  ageing: CaseAgeing;
  frozen: boolean;
  elapsedMilliseconds?: number;
}) {
  const live = tickAgeing(ageing, elapsedMilliseconds, frozen);
  return <>{durationLabel(live.ageInMilliseconds)}</>;
}
