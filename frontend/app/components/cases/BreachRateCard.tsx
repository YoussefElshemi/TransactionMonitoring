"use client";

import { ArrowDownRight, ArrowRight, ArrowUpRight } from "lucide-react";

import { CaseStatistic } from "@/types";
import { cn } from "@/lib/utils";
import { Card, CardContent } from "@/components/ui/card";
import { useTranslation } from "@/context/LocalizationContext";

const rateAccent = (breached: number): string =>
  breached === 0 ? "text-slate-900" : "text-critical-700";

const formatDelta = (points: number): string =>
  `${Math.abs(points).toFixed(1)}pp`;

export function BreachRateCard({
  statistic,
  previous,
  periodLabel,
  onShowBreached,
}: {
  statistic: CaseStatistic;
  previous?: CaseStatistic | null;
  periodLabel?: string;
  onShowBreached?: () => void;
}) {
  const t = useTranslation();
  const rate = Number.isFinite(statistic.breachRate) ? statistic.breachRate : 0;
  const breached = Number.isFinite(statistic.breached) ? statistic.breached : 0;

  const previousRate =
    previous && Number.isFinite(previous.breachRate) ? previous.breachRate : null;
  const delta = previousRate === null ? null : rate - previousRate;

  return (
    <Card>
      <CardContent className="flex flex-wrap items-start justify-between gap-6 pt-6">
        <div>
          <p className="text-xs font-medium uppercase tracking-widest text-slate-400">
            {t("cases.breachRate")}
          </p>
          <div className="mt-2 flex items-baseline gap-3">
            <p
              className={cn(
                "font-mono text-4xl font-medium tabular-nums tracking-tight",
                rateAccent(breached)
              )}
            >
              {/* Already a percentage on the wire. @see the note above. */}
              {rate.toFixed(2)}%
            </p>
            {delta !== null && (
              <span
                className={cn(
                  "flex items-center gap-1 font-mono text-xs",
                  delta > 0
                    ? "text-critical-700"
                    : delta < 0
                    ? "text-settled-700"
                    : "text-slate-400"
                )}
                title={`Against ${periodLabel ?? "the previous period"}. Percentage points, not a percentage change.`}
              >
                {delta > 0 ? (
                  <ArrowUpRight className="size-3.5" />
                ) : delta < 0 ? (
                  <ArrowDownRight className="size-3.5" />
                ) : (
                  <ArrowRight className="size-3.5" />
                )}
                {formatDelta(delta)}
              </span>
            )}
          </div>

          <p className="mt-2 text-sm text-slate-600">
            <span className="font-mono tabular-nums text-slate-900">
              {breached}
            </span>{" "}
            of{" "}
            <span className="font-mono tabular-nums text-slate-900">
              {statistic.total}
            </span>{" "}
            case{statistic.total === 1 ? "" : "s"} past their deadline
            {delta !== null && periodLabel ? `, against ${periodLabel}` : ""}.
          </p>

          <p className="mt-1.5 max-w-xl text-xs text-slate-500">
            Counts every case in the window, open ones included — a case past
            its deadline has breached whether or not anybody has closed it yet.
            A resolved case is frozen at the moment it resolved, so one worked
            inside its window never drifts into this figure afterwards.
          </p>
        </div>

        {onShowBreached && breached > 0 && (
          <button
            type="button"
            onClick={onShowBreached}
            className="font-mono text-xs text-brand-800 underline-offset-4 hover:underline"
          >
            Show the {breached} breached
          </button>
        )}
      </CardContent>
    </Card>
  );
}
