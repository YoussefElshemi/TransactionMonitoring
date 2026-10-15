"use client";

import { Fragment, useCallback, useEffect, useId, useMemo, useState } from "react";
import { toast } from "sonner";
import { TrendingUp, TriangleAlert } from "lucide-react";

import type { RecurringRevenueMonth } from "@/types";
import { API_BASE, reportApiError, unwrap } from "@/lib/api";
import { apiFetch } from "@/lib/http";
import {
  INVOLUNTARY_APPEARANCE,
  INVOLUNTARY_IS_INSIDE_CHURN,
  MOVEMENT_APPEARANCE,
  MOVEMENT_DOES_NOT_RECONCILE,
  MOVEMENT_LABELS,
  MOVEMENT_ORDER,
  NET_GROWTH_IS_NOT_THE_HEADLINE,
  type MonthPoint,
  type MovementKind,
  formatRevenue,
  formatSignedRevenue,
  involuntaryShareOfChurn,
  isForwardRange,
  movementExtent,
  movementReconciles,
  movementResidual,
  movementSteps,
} from "@/lib/revenue";
import { canReadRevenue } from "@/lib/permissions";
import { usePermissions } from "@/context/PermissionsContext";
import { useFormat } from "@/lib/useFormat";
import { useLocalization, useTranslation } from "@/context/LocalizationContext";
import { cn } from "@/lib/utils";
import { Skeleton } from "@/components/ui/skeleton";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import {
  MonthRangeFields,
  lastCompleteMonth,
  monthLabel,
  monthsBefore,
} from "@/components/revenue/MonthPicker";
import {
  RevenueHatchDefs,
  RevenueLegendItem,
  RevenueSwatch,
  hatchId,
  markFill,
} from "@/components/revenue/Swatch";
import { NeedsRevenuePermission } from "@/components/revenue/NeedsRevenuePermission";

export function RevenueMovementPanel({
  onUnauthorized,
}: {
  onUnauthorized: () => void;
}) {
  const t = useTranslation();
  const { grants, resolved } = usePermissions();

  const mayRead = canReadRevenue(grants);

  const [to, setTo] = useState<MonthPoint>(() => lastCompleteMonth());
  const [from, setFrom] = useState<MonthPoint>(() =>
    monthsBefore(lastCompleteMonth(), 12)
  );

  const [months, setMonths] = useState<RecurringRevenueMonth[]>([]);
  const [loading, setLoading] = useState(true);
  const [selected, setSelected] = useState<string | null>(null);

  const rangeIsForward = isForwardRange(from, to);

  const fetchMovement = useCallback(async () => {
    if (!rangeIsForward) return;
    setLoading(true);

    try {
      const query = new URLSearchParams({
        fromMonth: String(from.month),
        fromYear: String(from.year),
        toMonth: String(to.month),
        toYear: String(to.year),
      });

      const response = await apiFetch(`${API_BASE}/revenue/recurring?${query}`, {
        method: "GET",
        headers: { "Content-Type": "application/json" },
        credentials: "include",
      });

      if (response.ok) {
        setMonths(await unwrap<RecurringRevenueMonth[]>(response));
      } else {
        await reportApiError(response, {
          fallback: t("revenue.movementCouldNotBeRead"),
          onUnauthorized,
          toastId: "error-fetching-revenue-movement",
        });
      }
    } catch {
      toast.error(t("revenue.movementCouldNotBeRead"), {
        id: "error-fetching-revenue-movement",
      });
    } finally {
      setLoading(false);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [from.month, from.year, to.month, to.year, rangeIsForward, onUnauthorized]);

  useEffect(() => {
    if (mayRead) fetchMovement();
    else setLoading(false);
  }, [mayRead, fetchMovement]);

  const shown = useMemo(() => {
    const found = months.find((month) => monthKey(month) === selected);

    return found ?? months[months.length - 1] ?? null;
  }, [months, selected]);

  if (!resolved) return null;
  if (!mayRead) return <NeedsRevenuePermission permission="revenue:read" />;

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div className="flex items-start gap-2">
          <TrendingUp className="mt-0.5 size-3.5 flex-shrink-0 text-slate-400" />
          <div>
            <p className="font-mono text-xs uppercase tracking-widest text-slate-500">
              {t("revenue.movement")}
            </p>
            <p className="mt-1 max-w-2xl text-sm text-slate-500">
              {t("revenue.whatTheMonthWasMadeOf")}
            </p>
          </div>
        </div>

        <MonthRangeFields
          from={from}
          to={to}
          onFrom={setFrom}
          onTo={setTo}
          disabled={loading}
          invalid={!rangeIsForward}
        />
      </div>

      <p className="rounded-lg border border-slate-200 bg-slate-50 p-3 text-xs text-slate-500">
        {t(NET_GROWTH_IS_NOT_THE_HEADLINE)}
      </p>

      {loading ? (
        <Skeleton className="h-64 w-full rounded-xl" />
      ) : shown === null ? (
        <div className="rounded-xl border border-slate-200 bg-card p-10 text-center">
          <p className="font-mono text-xs uppercase tracking-widest text-slate-400">
            {t("revenue.nothingInThisRange")}
          </p>
          <p className="mx-auto mt-2 max-w-md text-sm text-slate-500">
            {t("revenue.noMonthInThisRangeHasBeenBilled")}
          </p>
        </div>
      ) : (
        <>
          <MonthStrip
            months={months}
            selectedKey={monthKey(shown)}
            onSelect={setSelected}
          />
          <MovementWaterfall month={shown} />
          <MovementTable month={shown} />
          {months.length > 1 && (
            <SeriesTable
              months={months}
              selectedKey={monthKey(shown)}
              onSelect={setSelected}
            />
          )}
        </>
      )}
    </div>
  );
}

const monthKey = (month: { month: number; year: number }): string =>
  `${month.year}-${month.month}`;

function MonthStrip({
  months,
  selectedKey,
  onSelect,
}: {
  months: RecurringRevenueMonth[];
  selectedKey: string;
  onSelect: (key: string) => void;
}) {
  const t = useTranslation();
  const { locale } = useLocalization();

  return (
    <div
      className="flex flex-wrap gap-1"
      role="group"
      aria-label={t("revenue.chooseMonth")}
    >
      {months.map((month) => {
        const key = monthKey(month);
        const active = key === selectedKey;

        return (
          <button
            key={key}
            type="button"
            aria-pressed={active}
            onClick={() => onSelect(key)}
            className={cn(
              "rounded-md border px-2 py-1 font-mono text-[11px] transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-800",
              active
                ? "border-brand-800 bg-brand-50 text-brand-900"
                : "border-slate-200 text-slate-500 hover:text-slate-900"
            )}
          >
            {monthLabel(month, locale)}
          </button>
        );
      })}
    </div>
  );
}

function MovementWaterfall({ month }: { month: RecurringRevenueMonth }) {
  const t = useTranslation();
  const patterns = useId();

  const steps = movementSteps(month);
  const { min, max } = movementExtent(steps);

  const WIDTH = 760;
  const HEIGHT = 250;
  const TOP = 26;
  const BOTTOM = 34;
  const plot = HEIGHT - TOP - BOTTOM;

  // Never zero: a month in which nothing at all moved would otherwise divide by
  // it and put every bar on the same pixel.
  const span = max - min || 1;
  const y = (value: number) => TOP + ((max - value) / span) * plot;
  const heightOf = (magnitude: number) => (Math.abs(magnitude) / span) * plot;

  const column = WIDTH / steps.length;
  const barWidth = column * 0.5;
  const centre = (index: number) => index * column + column / 2;

  const involuntary = month.involuntaryChurnedRevenue;

  return (
    <div className="rounded-xl border border-slate-200 bg-card p-4">
      <div className="overflow-x-auto">
        <svg
          viewBox={`0 0 ${WIDTH} ${HEIGHT}`}
          className="h-auto w-full min-w-[640px]"
          aria-hidden="true"
          focusable="false"
        >
          <RevenueHatchDefs prefix={patterns} tones={["settled", "critical"]} />

          <line
            x1={0}
            x2={WIDTH}
            y1={y(0)}
            y2={y(0)}
            className="stroke-slate-200"
            strokeWidth={1}
          />

          {steps.map((step, index) => {
            const top = Math.min(y(step.from), y(step.to));
            const height = Math.abs(y(step.from) - y(step.to));
            const x = centre(index) - barWidth / 2;
            const mark = markFill(patterns, step.tone, step.fill);
            const drawn = Math.max(height, 2);

            return (
              <Fragment key={step.kind}>
                {index > 0 && !step.isTotal && (
                  <line
                    x1={centre(index - 1) + barWidth / 2}
                    x2={x}
                    y1={y(step.from)}
                    y2={y(step.from)}
                    className="stroke-slate-300"
                    strokeWidth={1}
                    strokeDasharray="3 3"
                  />
                )}

                <rect
                  x={x}
                  y={top}
                  width={barWidth}
                  height={drawn}
                  rx={2}
                  className={mark.className}
                  fill={mark.fill}
                />

                {step.kind === "churn" && involuntary > 0 && (
                  <rect
                    x={x}
                    y={y(step.to) - Math.min(heightOf(involuntary), drawn)}
                    width={barWidth}
                    height={Math.max(
                      Math.min(heightOf(involuntary), drawn),
                      2
                    )}
                    rx={2}
                    fill={`url(#${hatchId(patterns, "critical")})`}
                  />
                )}

                <text
                  x={centre(index)}
                  y={step.direction === "down" ? top + drawn + 13 : top - 6}
                  textAnchor="middle"
                  className="fill-slate-600 font-mono text-[10px] tabular-nums"
                >
                  {step.isTotal
                    ? formatRevenue(step.amount, month.currency)
                    : formatSignedRevenue(step.amount, month.currency)}
                </text>

                <text
                  x={centre(index)}
                  y={HEIGHT - 12}
                  textAnchor="middle"
                  className="fill-slate-500 text-[10px]"
                >
                  {t(step.labelKey)}
                </text>
              </Fragment>
            );
          })}
        </svg>
      </div>

      <div className="mt-3 flex flex-wrap gap-x-4 gap-y-1.5 border-t border-slate-100 pt-3">
        {steps
          .filter((step) => !step.isTotal)
          .map((step) => (
            <RevenueLegendItem
              key={step.kind}
              tone={step.tone}
              fill={step.fill}
              label={t(step.labelKey)}
              title={t(step.hintKey)}
            />
          ))}
        <RevenueLegendItem
          tone={INVOLUNTARY_APPEARANCE.tone}
          fill={INVOLUNTARY_APPEARANCE.fill}
          label={t("revenue.ofWhichInvoluntary")}
          title={t(INVOLUNTARY_IS_INSIDE_CHURN)}
        />
      </div>

      {!movementReconciles(month) && (
        <p
          role="alert"
          className="mt-3 flex items-start gap-2 rounded-lg border border-critical-200 bg-critical-50 p-3 text-xs text-critical-700"
        >
          <TriangleAlert className="mt-px size-3.5 flex-shrink-0" />
          <span>
            {t(
              MOVEMENT_DOES_NOT_RECONCILE,
              formatRevenue(movementResidual(month), month.currency)
            )}
          </span>
        </p>
      )}
    </div>
  );
}

function MovementTable({ month }: { month: RecurringRevenueMonth }) {
  const t = useTranslation();
  const { locale } = useLocalization();
  const format = useFormat();

  const steps = movementSteps(month);
  const involuntaryShare = involuntaryShareOfChurn(month);

  return (
    <div className="overflow-hidden rounded-xl border border-slate-200 bg-card">
      <Table>
        <caption className="sr-only">
          {t("revenue.movementTableCaption", monthLabel(month, locale))}
        </caption>
        <TableHeader>
          <TableRow className="bg-slate-50 hover:bg-slate-50">
            <TableHead className="w-56">{t("revenue.component")}</TableHead>
            <TableHead className="w-40 text-right">
              {t("revenue.amount")}
            </TableHead>
            <TableHead className="w-32 text-right">
              {t("revenue.customers")}
            </TableHead>
            <TableHead>{t("revenue.whatItMeans")}</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {steps.map((step) => (
            <Fragment key={step.kind}>
              <TableRow
                className={cn(
                  "hover:bg-slate-50/50",
                  step.isTotal && "bg-slate-50/60 font-medium"
                )}
              >
                <TableCell className="text-sm text-slate-900">
                  <span className="inline-flex items-center gap-2">
                    {!step.isTotal && (
                      <RevenueSwatch tone={step.tone} fill={step.fill} />
                    )}
                    {t(step.labelKey)}
                  </span>
                </TableCell>
                <TableCell className="text-right font-mono text-sm tabular-nums text-slate-900">
                  {step.isTotal
                    ? formatRevenue(step.amount, month.currency)
                    : formatSignedRevenue(step.amount, month.currency)}
                </TableCell>
                <TableCell className="text-right font-mono text-sm tabular-nums text-slate-600">
                  {step.customers === null ? (
                    <span
                      className="text-slate-400"
                      title={t("revenue.movesWithinExistingCustomers")}
                    >
                      —
                    </span>
                  ) : (
                    format.count(step.customers)
                  )}
                </TableCell>
                <TableCell className="text-xs text-slate-500">
                  {t(step.hintKey)}
                </TableCell>
              </TableRow>

              {step.kind === "churn" && (
                <TableRow className="hover:bg-transparent">
                  <TableCell className="py-2 pl-9 text-xs text-slate-600">
                    <span className="inline-flex items-center gap-2">
                      <RevenueSwatch
                        tone={INVOLUNTARY_APPEARANCE.tone}
                        fill={INVOLUNTARY_APPEARANCE.fill}
                      />
                      {t("revenue.ofWhichInvoluntary")}
                    </span>
                  </TableCell>
                  <TableCell className="py-2 text-right font-mono text-xs tabular-nums text-slate-600">
                    {formatRevenue(
                      month.involuntaryChurnedRevenue,
                      month.currency
                    )}
                  </TableCell>
                  <TableCell className="py-2 text-right font-mono text-xs tabular-nums text-slate-600">
                    {format.count(month.involuntarilyChurnedCustomers)}
                  </TableCell>
                  <TableCell className="py-2 text-xs text-slate-500">
                    {t(INVOLUNTARY_IS_INSIDE_CHURN)}{" "}
                    {involuntaryShare !== null && (
                      <span className="text-slate-400">
                        {t(
                          "revenue.shareOfChurn",
                          format.percent(involuntaryShare)
                        )}
                      </span>
                    )}
                  </TableCell>
                </TableRow>
              )}
            </Fragment>
          ))}
        </TableBody>
      </Table>
    </div>
  );
}

const SERIES_COLUMNS: {
  kind: MovementKind;
  value: (month: RecurringRevenueMonth) => number;
  signed: boolean;
}[] = MOVEMENT_ORDER.map((kind) => {
  switch (kind) {
    case "opening":
      return { kind, value: (m) => m.openingRevenue, signed: false };
    case "new":
      return { kind, value: (m) => m.newRevenue, signed: true };
    case "reactivation":
      return { kind, value: (m) => m.reactivationRevenue, signed: true };
    case "expansion":
      return { kind, value: (m) => m.expansionRevenue, signed: true };
    case "contraction":
      return { kind, value: (m) => -m.contractionRevenue, signed: true };
    case "churn":
      return { kind, value: (m) => -m.churnedRevenue, signed: true };
    default:
      return { kind, value: (m) => m.closingRevenue, signed: false };
  }
});

function SeriesTable({
  months,
  selectedKey,
  onSelect,
}: {
  months: RecurringRevenueMonth[];
  selectedKey: string;
  onSelect: (key: string) => void;
}) {
  const t = useTranslation();
  const { locale } = useLocalization();

  return (
    <div className="overflow-hidden rounded-xl border border-slate-200 bg-card">
      <div className="overflow-x-auto">
        <Table>
          <caption className="sr-only">{t("revenue.seriesTableCaption")}</caption>
          <TableHeader>
            <TableRow className="bg-slate-50 hover:bg-slate-50">
              <TableHead className="w-44">{t("revenue.month")}</TableHead>
              {SERIES_COLUMNS.map((column) => (
                <TableHead key={column.kind} className="text-right">
                  <span className="inline-flex items-center gap-1.5">
                    <RevenueSwatch
                      tone={MOVEMENT_APPEARANCE[column.kind].tone}
                      fill={MOVEMENT_APPEARANCE[column.kind].fill}
                    />
                    {t(MOVEMENT_LABELS[column.kind])}
                  </span>
                </TableHead>
              ))}
            </TableRow>
          </TableHeader>
          <TableBody>
            {months.map((month) => {
              const key = monthKey(month);

              return (
                <TableRow
                  key={key}
                  className={cn(
                    "hover:bg-slate-50/50",
                    key === selectedKey && "bg-brand-50/40"
                  )}
                >
                  <TableCell className="text-sm text-slate-900">
                    <button
                      type="button"
                      aria-pressed={key === selectedKey}
                      onClick={() => onSelect(key)}
                      className="underline-offset-2 hover:underline focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-800"
                    >
                      {monthLabel(month, locale)}
                    </button>
                  </TableCell>
                  {SERIES_COLUMNS.map((column) => (
                    <TableCell
                      key={column.kind}
                      className="text-right font-mono text-xs tabular-nums text-slate-700"
                    >
                      {column.signed
                        ? formatSignedRevenue(
                            column.value(month),
                            month.currency
                          )
                        : formatRevenue(column.value(month), month.currency)}
                    </TableCell>
                  ))}
                </TableRow>
              );
            })}
          </TableBody>
        </Table>
      </div>
    </div>
  );
}
