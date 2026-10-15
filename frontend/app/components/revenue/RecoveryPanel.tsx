"use client";

import { useCallback, useEffect, useState } from "react";
import { toast } from "sonner";
import { endOfDay, startOfDay, subDays } from "date-fns";
import type { DateRange } from "react-day-picker";
import { LifeBuoy } from "lucide-react";

import type { RecoveryStatistic } from "@/types";
import { API_BASE, reportApiError, unwrap } from "@/lib/api";
import { apiFetch } from "@/lib/http";
import {
  COHORT_IS_STILL_MATURING,
  RECOVERY_IS_A_COHORT,
  RESTRICTIONS_PROPOSED_EXPLAINED,
  answered,
  cohortIsStillMaturing,
  formatRevenue,
  recoveryStageRows,
} from "@/lib/revenue";
import { canReadRevenue } from "@/lib/permissions";
import { usePermissions } from "@/context/PermissionsContext";
import { useFormat } from "@/lib/useFormat";
import { useTranslation } from "@/context/LocalizationContext";
import { ToneBadge } from "@/components/status/StatusChips";
import { DateRangePicker } from "@/components/DateRangePicker";
import { Skeleton } from "@/components/ui/skeleton";
import { NeedsRevenuePermission } from "@/components/revenue/NeedsRevenuePermission";
import { TONE_BG_CLASS } from "@/components/revenue/Swatch";

export function RecoveryPanel({
  onUnauthorized,
}: {
  onUnauthorized: () => void;
}) {
  const t = useTranslation();
  const format = useFormat();
  const { grants, resolved } = usePermissions();

  const mayRead = canReadRevenue(grants);

  const [range, setRange] = useState<DateRange | undefined>(() => ({
    from: subDays(new Date(), 90),
    to: new Date(),
  }));

  const [statistic, setStatistic] = useState<RecoveryStatistic | null>(null);
  const [loading, setLoading] = useState(true);

  const fetchRecovery = useCallback(async () => {
    if (!range?.from || !range.to) return;
    setLoading(true);

    try {
      const query = new URLSearchParams({
        periodStart: startOfDay(range.from).toISOString(),
        periodEnd: endOfDay(range.to).toISOString(),
      });

      const response = await apiFetch(`${API_BASE}/revenue/recovery?${query}`, {
        method: "GET",
        headers: { "Content-Type": "application/json" },
        credentials: "include",
      });

      if (response.ok) {
        setStatistic(await unwrap<RecoveryStatistic>(response));
      } else {
        await reportApiError(response, {
          fallback: t("revenue.recoveryCouldNotBeRead"),
          onUnauthorized,
          toastId: "error-fetching-revenue-recovery",
        });
      }
    } catch {
      toast.error(t("revenue.recoveryCouldNotBeRead"), {
        id: "error-fetching-revenue-recovery",
      });
    } finally {
      setLoading(false);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [range?.from?.getTime(), range?.to?.getTime(), onUnauthorized]);

  useEffect(() => {
    if (mayRead) fetchRecovery();
    else setLoading(false);
  }, [mayRead, fetchRecovery]);

  if (!resolved) return null;
  if (!mayRead) return <NeedsRevenuePermission permission="revenue:read" />;

  const maturing = statistic !== null && cohortIsStillMaturing(statistic);

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div className="flex items-start gap-2">
          <LifeBuoy className="mt-0.5 size-3.5 flex-shrink-0 text-slate-400" />
          <div>
            <p className="font-mono text-xs uppercase tracking-widest text-slate-500">
              {t("revenue.recovery")}
            </p>
            <p className="mt-1 max-w-2xl text-sm text-slate-500">
              {t("revenue.whetherChasingWorks")}
            </p>
          </div>
        </div>

        <DateRangePicker
          value={range}
          onChange={setRange}
          disabled={loading}
          aria-label={t("revenue.recoveryWindow")}
        />
      </div>

      <p className="rounded-lg border border-slate-200 bg-slate-50 p-3 text-xs text-slate-500">
        {t(RECOVERY_IS_A_COHORT)}
      </p>

      {loading || statistic === null ? (
        <Skeleton className="h-56 w-full rounded-xl" />
      ) : (
        <>
          <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
            <Tile
              label={t("revenue.laddersOpened")}
              value={format.count(statistic.laddersOpened)}
              hint={t("revenue.theCohortItself")}
            />
            <Tile
              label={t("revenue.laddersResolved")}
              value={format.count(statistic.laddersResolved)}
            />
            <Tile
              label={t("revenue.recoveryRate")}
              answer={answered(
                statistic.recoveryRate,
                (fraction) => format.percent(fraction),
                "noLaddersOpened"
              )}
              hint={maturing ? t(COHORT_IS_STILL_MATURING) : undefined}
              hintTone={maturing ? "attention" : undefined}
            />
            <Tile
              label={t("revenue.meanDaysToRecover")}
              answer={answered(
                statistic.meanDaysToRecover,
                (days) => format.decimal(days, { maximumFractionDigits: 1 }),
                "nothingResolved"
              )}
            />
          </div>

          <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
            <Tile
              label={t("revenue.recovered")}
              value={formatRevenue(
                statistic.recoveredRevenue,
                statistic.currency
              )}
            />
            <Tile
              label={t("revenue.stillOutstanding")}
              value={formatRevenue(
                statistic.outstandingRevenue,
                statistic.currency
              )}
            />
            <Tile
              label={t("revenue.decisionsHandedOver")}
              value={format.count(statistic.restrictionsProposed)}
              hint={t(RESTRICTIONS_PROPOSED_EXPLAINED)}
            />
            <Tile
              label={t("revenue.involuntaryChurnRate")}
              answer={answered(
                statistic.involuntaryChurnRate,
                (fraction) => format.percent(fraction),
                "noLaddersOpened"
              )}
              hint={t(
                "revenue.customersLostToFailedCollection",
                format.count(statistic.involuntarilyChurnedCustomers)
              )}
            />
          </div>

          <StageBreakdown statistic={statistic} />
        </>
      )}
    </div>
  );
}

function Tile({
  label,
  value,
  answer,
  hint,
  hintTone,
}: {
  label: string;
  value?: string;
  answer?: { known: boolean; text: string; reasonKey: string };
  hint?: string;
  hintTone?: "attention";
}) {
  const t = useTranslation();

  const known = answer ? answer.known : true;
  const text = answer ? answer.text : (value ?? "");

  return (
    <div className="rounded-xl border border-slate-200 bg-card p-4">
      <p className="font-mono text-[11px] uppercase tracking-widest text-slate-400">
        {label}
      </p>
      <p
        className={`mt-2 font-mono text-2xl tabular-nums ${
          known ? "text-slate-900" : "text-slate-400"
        }`}
        title={known ? undefined : t(answer!.reasonKey)}
      >
        {text}
      </p>
      {!known && (
        <p className="mt-1.5 text-xs text-slate-500">{t(answer!.reasonKey)}</p>
      )}
      {hint && (
        <p
          className={`mt-1.5 text-xs ${
            hintTone === "attention" ? "text-attention-700" : "text-slate-500"
          }`}
        >
          {hint}
        </p>
      )}
    </div>
  );
}

function StageBreakdown({ statistic }: { statistic: RecoveryStatistic }) {
  const t = useTranslation();
  const format = useFormat();

  const rows = recoveryStageRows(statistic);
  const most = Math.max(...rows.map((row) => row.resolved), 1);

  return (
    <div className="rounded-xl border border-slate-200 bg-card p-4">
      <p className="font-mono text-[11px] uppercase tracking-widest text-slate-400">
        {t("revenue.resolvedAtEachStage")}
      </p>
      <p className="mt-1 text-xs text-slate-500">
        {t("revenue.whereTheLaddersEnded")}
      </p>

      <ul className="mt-3 space-y-2">
        {rows.map((row) => (
          <li key={row.stage} className="flex items-center gap-3">
            <span className="w-40 flex-shrink-0">
              <ToneBadge tone={row.tone}>{t(row.labelKey)}</ToneBadge>
            </span>
            <span
              aria-hidden="true"
              className="h-2 flex-1 overflow-hidden rounded-full bg-slate-100"
            >
              <span
                className={`block h-full rounded-full ${TONE_BG_CLASS[row.tone]}`}
                style={{ width: `${Math.max(1, (row.resolved / most) * 100)}%` }}
              />
            </span>
            <span className="w-16 flex-shrink-0 text-right font-mono text-xs tabular-nums text-slate-700">
              {format.count(row.resolved)}
            </span>
            <span className="w-16 flex-shrink-0 text-right font-mono text-xs tabular-nums text-slate-500">
              {row.share === null ? (
                <span title={t("revenue.noAnswer.nothingResolved")}>—</span>
              ) : (
                format.percent(row.share)
              )}
            </span>
          </li>
        ))}
      </ul>
    </div>
  );
}
