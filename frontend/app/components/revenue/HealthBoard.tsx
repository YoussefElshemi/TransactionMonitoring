"use client";

import { Fragment, useCallback, useEffect, useState } from "react";
import Link from "next/link";
import { toast } from "sonner";
import { HeartPulse } from "lucide-react";

import type { CustomerHealth, HealthSignal } from "@/types";
import { API_BASE, reportApiError, unwrap } from "@/lib/api";
import { apiFetch } from "@/lib/http";
import {
  DEFAULT_WINDOW_DAYS,
  HEALTH_BAND_HINTS,
  HEALTH_BAND_LABELS,
  PLATFORM_WROTE_THIS,
  SCORE_IS_A_SORT_NOT_A_VERDICT,
  SIGNAL_LABELS,
  arrearsChip,
  clampWindowDays,
  healthBandTone,
  isAbsenceOfEvidence,
  signalSentence,
  signalTone,
  signalsInReadingOrder,
  usageTrendAnswer,
} from "@/lib/revenue";
import { canReadRevenue } from "@/lib/permissions";
import { usePermissions } from "@/context/PermissionsContext";
import { useFormat } from "@/lib/useFormat";
import { useTranslation } from "@/context/LocalizationContext";
import { cn } from "@/lib/utils";
import { ToneBadge } from "@/components/status/StatusChips";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { NeedsRevenuePermission } from "@/components/revenue/NeedsRevenuePermission";

const WINDOWS = [7, DEFAULT_WINDOW_DAYS, 90, 365];

export function HealthBoard({ onUnauthorized }: { onUnauthorized: () => void }) {
  const t = useTranslation();
  const { grants, resolved } = usePermissions();

  const mayRead = canReadRevenue(grants);

  const [windowDays, setWindowDays] = useState(DEFAULT_WINDOW_DAYS);
  const [rows, setRows] = useState<CustomerHealth[]>([]);
  const [loading, setLoading] = useState(true);
  const [opened, setOpened] = useState<string | null>(null);

  const fetchHealth = useCallback(async () => {
    setLoading(true);

    try {
      const query = new URLSearchParams({
        windowDays: String(clampWindowDays(windowDays)),
      });

      const response = await apiFetch(`${API_BASE}/revenue/health?${query}`, {
        method: "GET",
        headers: { "Content-Type": "application/json" },
        credentials: "include",
      });

      if (response.ok) {
        setRows(await unwrap<CustomerHealth[]>(response));
      } else {
        await reportApiError(response, {
          fallback: t("revenue.healthCouldNotBeRead"),
          onUnauthorized,
          toastId: "error-fetching-revenue-health",
        });
      }
    } catch {
      toast.error(t("revenue.healthCouldNotBeRead"), {
        id: "error-fetching-revenue-health",
      });
    } finally {
      setLoading(false);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [windowDays, onUnauthorized]);

  useEffect(() => {
    if (mayRead) fetchHealth();
    else setLoading(false);
  }, [mayRead, fetchHealth]);

  if (!resolved) return null;
  if (!mayRead) return <NeedsRevenuePermission permission="revenue:read" />;

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div className="flex items-start gap-2">
          <HeartPulse className="mt-0.5 size-3.5 flex-shrink-0 text-slate-400" />
          <div>
            <p className="font-mono text-xs uppercase tracking-widest text-slate-500">
              {t("revenue.health")}
            </p>
            <p className="mt-1 max-w-2xl text-sm text-slate-500">
              {t("revenue.whoLooksLikeTheyAreLeaving")}
            </p>
          </div>
        </div>

        <div className="flex flex-col gap-1">
          <span
            id="revenue-window-days"
            className="font-mono text-[11px] uppercase tracking-widest text-slate-500"
          >
            {t("revenue.window")}
          </span>
          <Select
            value={String(windowDays)}
            disabled={loading}
            onValueChange={(value) => setWindowDays(Number(value))}
          >
            <SelectTrigger
              className="h-8 w-40 text-xs"
              aria-labelledby="revenue-window-days"
            >
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {WINDOWS.map((days) => (
                <SelectItem key={days} value={String(days)}>
                  {t("revenue.lastDays", days)}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
      </div>

      <p className="rounded-lg border border-slate-200 bg-slate-50 p-3 text-xs text-slate-500">
        {t(SCORE_IS_A_SORT_NOT_A_VERDICT)}
      </p>

      <div className="overflow-hidden rounded-xl border border-slate-200 bg-card">
        <Table>
          <caption className="sr-only">
            {t("revenue.healthTableCaption", windowDays)}
          </caption>
          <TableHeader>
            <TableRow className="bg-slate-50 hover:bg-slate-50">
              <TableHead className="w-56">
                {t("reconciliation.customer")}
              </TableHead>
              <TableHead className="w-44">{t("revenue.score")}</TableHead>
              <TableHead>{t("revenue.signals")}</TableHead>
              <TableHead className="w-32 text-right">
                {t("revenue.usageTrend")}
              </TableHead>
              <TableHead className="w-24">{t("common.actions")}</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {loading ? (
              Array.from({ length: 4 }).map((_, row) => (
                <TableRow key={row}>
                  {Array.from({ length: 5 }).map((_, cell) => (
                    <TableCell key={cell}>
                      <Skeleton className="h-4 w-full" />
                    </TableCell>
                  ))}
                </TableRow>
              ))
            ) : rows.length === 0 ? (
              <TableRow>
                <TableCell colSpan={5} className="py-10 text-center">
                  <p className="font-mono text-xs uppercase tracking-widest text-slate-400">
                    {t("revenue.nothingToScore")}
                  </p>
                  <p className="mx-auto mt-2 max-w-md text-sm text-slate-500">
                    {t("revenue.noCustomerHasUsageInThisWindow")}
                  </p>
                </TableCell>
              </TableRow>
            ) : (
              rows.map((health) => (
                <HealthRow
                  key={health.organizationId}
                  health={health}
                  open={opened === health.organizationId}
                  onToggle={() =>
                    setOpened(
                      opened === health.organizationId
                        ? null
                        : health.organizationId
                    )
                  }
                />
              ))
            )}
          </TableBody>
        </Table>
      </div>
    </div>
  );
}

function HealthRow({
  health,
  open,
  onToggle,
}: {
  health: CustomerHealth;
  open: boolean;
  onToggle: () => void;
}) {
  const t = useTranslation();
  const format = useFormat();

  const signals = signalsInReadingOrder(health.signals);
  const arrears = arrearsChip(health);
  const trend = usageTrendAnswer(health, (fraction) =>
    format.percent(fraction)
  );

  return (
    <Fragment>
      <TableRow className="hover:bg-slate-50/50">
        <TableCell className="text-sm">
          <Link
            href={`/admin/organizations/${health.organizationId}`}
            className="text-slate-900 underline-offset-2 hover:underline"
          >
            {health.organizationName}
          </Link>
        </TableCell>

        <TableCell>
          <div className="flex flex-wrap items-center gap-2">
            <ToneBadge
              tone={healthBandTone(health.healthBand)}
              title={t(HEALTH_BAND_HINTS[health.healthBand])}
            >
              {t(HEALTH_BAND_LABELS[health.healthBand])}
            </ToneBadge>
            <span className="font-mono text-sm tabular-nums text-slate-700">
              {format.count(health.healthScore)}
            </span>
          </div>
        </TableCell>

        <TableCell>
          <div className="flex flex-wrap items-center gap-1.5">
            {signals.length === 0 ? (
              <span className="text-xs text-slate-400">
                {t("revenue.noSignals")}
              </span>
            ) : (
              signals.map((signal) => (
                <SignalChip key={signal.kind} signal={signal} />
              ))
            )}
            {arrears && (
              <ToneBadge tone={arrears.tone}>{t(arrears.labelKey)}</ToneBadge>
            )}
          </div>
        </TableCell>

        <TableCell className="text-right font-mono text-sm tabular-nums">
          <span
            className={trend.known ? "text-slate-900" : "text-slate-400"}
            title={trend.known ? undefined : t(trend.reasonKey)}
          >
            {trend.known && health.usageTrend !== null && health.usageTrend > 0
              ? `+${trend.text}`
              : trend.text}
          </span>
        </TableCell>

        <TableCell>
          <Button
            variant="ghost"
            size="sm"
            className="h-8 text-xs"
            onClick={onToggle}
          >
            {open ? t("reconciliation.hide") : t("revenue.why")}
          </Button>
        </TableCell>
      </TableRow>

      {open && (
        <TableRow className="hover:bg-transparent">
          <TableCell colSpan={5} className="bg-slate-50/60 p-4">
            <HealthDetail health={health} />
          </TableCell>
        </TableRow>
      )}
    </Fragment>
  );
}

function SignalChip({ signal }: { signal: HealthSignal }) {
  const t = useTranslation();
  const format = useFormat();

  const absence = isAbsenceOfEvidence(signal.kind);

  return (
    <ToneBadge
      tone={signalTone(signal)}
      className={cn(absence && "border-dashed")}
    >
      {t(SIGNAL_LABELS[signal.kind])}
      {!absence && (
        <span className="ml-1 tabular-nums opacity-80">
          −{format.count(signal.penalty)}
        </span>
      )}
    </ToneBadge>
  );
}

function HealthDetail({ health }: { health: CustomerHealth }) {
  const t = useTranslation();
  const format = useFormat();

  const signals = signalsInReadingOrder(health.signals);

  return (
    <div className="space-y-3">
      <dl className="grid gap-x-6 gap-y-2 sm:grid-cols-2 lg:grid-cols-4">
        <Fact
          label={t("revenue.recentUsage")}
          value={format.count(health.recentUsage)}
        />
        <Fact
          label={t("revenue.previousUsage")}
          value={format.count(health.previousUsage)}
        />
        <Fact
          label={t("revenue.errorRate")}
          value={t(
            "revenue.fromTo",
            format.percent(health.previousErrorRate),
            format.percent(health.recentErrorRate)
          )}
        />
        <Fact
          label={t("revenue.lastUsed")}
          value={
            health.daysSinceLastUsage === null
              ? t("revenue.noAnswer.neverUsed")
              : t("revenue.daysAgo", format.count(health.daysSinceLastUsage))
          }
        />
      </dl>

      <ul className="space-y-2">
        {signals.map((signal) => {
          const sentence = signalSentence(health, signal, {
            percent: (fraction) => format.percent(fraction),
            count: (value) => format.count(value),
          });

          return (
            <li
              key={signal.kind}
              className={cn(
                "rounded-lg border p-3",
                isAbsenceOfEvidence(signal.kind)
                  ? "border-dashed border-slate-200 bg-transparent"
                  : "border-slate-200 bg-card"
              )}
            >
              <div className="flex flex-wrap items-center gap-2">
                <SignalChip signal={signal} />
                <span className="text-xs text-slate-700">
                  {t(sentence.key, ...sentence.args)}
                </span>
              </div>
              <p className="mt-1.5 text-[11px] text-slate-400">
                {t(PLATFORM_WROTE_THIS)} {signal.detail}
              </p>
            </li>
          );
        })}
      </ul>
    </div>
  );
}

function Fact({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <dt className="font-mono text-[11px] uppercase tracking-widest text-slate-400">
        {label}
      </dt>
      <dd className="mt-0.5 font-mono text-sm tabular-nums text-slate-900">
        {value}
      </dd>
    </div>
  );
}
