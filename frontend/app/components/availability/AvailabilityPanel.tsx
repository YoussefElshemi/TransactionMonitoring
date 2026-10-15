"use client";

import { useCallback, useEffect, useState } from "react";
import { toast } from "sonner";

import type { AvailabilityMeasurement } from "@/types";
import { API_BASE, reportApiError, unwrap } from "@/lib/api";
import { apiFetch } from "@/lib/http";
import { usePermissions } from "@/context/PermissionsContext";
import {
  DEFAULT_PERIOD_HINT,
  NO_TARGET_AGREED,
  PERCENTILE_IS_A_BOUND,
  VERDICT_LABELS,
  availabilityQuery,
  availabilitySentence,
  availabilityVerdict,
  failureSentence,
  formatTarget,
  formatUptime,
  hasAgreedTarget,
  isThinlyMeasured,
  latencySentence,
  latencyVerdict,
  percentileLabel,
  periodLabel,
  periodRefusal,
  thinTrafficNote,
  verdictTone,
  type TargetVerdict,
} from "@/lib/availability";
import { ToneBadge } from "@/components/status/StatusChips";
import { LocalTime } from "@/components/LocalTime";
import { Card, CardContent } from "@/components/ui/card";
import { DateRangeField } from "@/components/DateRangePicker";
import { Skeleton } from "@/components/ui/skeleton";

import { useLocalization, useTranslation } from "@/context/LocalizationContext";

export function AvailabilityPanel({
  organizationId,
  ownOrganization = false,
  onUnauthorized,
}: {
  organizationId: string;
  ownOrganization?: boolean;
  onUnauthorized: () => void;
}) {
  const t = useTranslation();
  const { holds } = usePermissions();
  const canRead = holds("availability:read");

  const [measurement, setMeasurement] = useState<AvailabilityMeasurement | null>(
    null
  );
  const [loading, setLoading] = useState(true);
  const [refused, setRefused] = useState(false);

  const [period, setPeriod] = useState({ fromDate: "", toDate: "" });

  const fetchMeasurement = useCallback(async () => {
    setLoading(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationId}/availability${availabilityQuery(
          period
        )}`,
        {
          method: "GET",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
        }
      );

      if (response.ok) {
        setMeasurement(await unwrap<AvailabilityMeasurement>(response));
        setRefused(false);
      } else if (response.status === 403) {
        setRefused(true);
      } else {
        await reportApiError(response, {
          fallback: t("availability.errorOccurredMeasuringAvailability"),
          onUnauthorized,
          toastId: "error-measuring-availability",
        });
      }
    } catch {
      toast.error(t("availability.errorOccurredMeasuringAvailability"), {
        id: "error-measuring-availability",
      });
    } finally {
      setLoading(false);
    }
  }, [organizationId, period, onUnauthorized]);

  const refusal = periodRefusal(period);

  useEffect(() => {
    if (canRead && refusal === null) fetchMeasurement();
  }, [canRead, refusal, fetchMeasurement]);

  if (!canRead) {
    return (
      <Card>
        <CardContent className="p-6">
          <p className="text-sm leading-relaxed text-slate-500">
            {t("availability.readingAvailabilityNeeds")} <code>availability:read</code>.
          </p>
        </CardContent>
      </Card>
    );
  }

  if (refused) {
    return (
      <Card>
        <CardContent className="p-6">
          <p className="text-sm leading-relaxed text-slate-500">
            The API refused this measurement. Reading another organisation&apos;s
            availability needs <code>platform:administer</code> as well as{" "}
            <code>availability:read</code>.
          </p>
        </CardContent>
      </Card>
    );
  }

  return (
    <div className="space-y-6">
      <Card>
        <CardContent className="space-y-3 p-6">
          <h3 className="font-mono text-xs uppercase tracking-widest text-slate-400">
            {t("availability.period")}
          </h3>
          <DateRangeField
            from={period.fromDate}
            to={period.toDate}
            onChange={(fromDate, toDate) => setPeriod({ fromDate, toDate })}
            emptyLabel={t("availability.allTime")}
            clearLabel={t("availability.clear")}
            align="start"
            aria-label={t("availability.availabilityMeasurementPeriod")}
          />

          {refusal === null ? (
            <p className="text-xs leading-relaxed text-slate-500">
              {DEFAULT_PERIOD_HINT}
            </p>
          ) : (
            <p className="text-sm leading-relaxed text-critical-700">
              {refusal}
            </p>
          )}
        </CardContent>
      </Card>

      {loading ? (
        <Skeleton className="h-72 w-full rounded-lg" />
      ) : measurement === null ? (
        <Card>
          <CardContent className="p-6">
            <p className="text-sm leading-relaxed text-slate-500">
              {t("availability.noMeasurementCouldBeRead")}
            </p>
          </CardContent>
        </Card>
      ) : (
        <Measurement
          measurement={measurement}
          ownOrganization={ownOrganization}
        />
      )}
    </div>
  );
}

function Measurement({
  measurement,
  ownOrganization,
}: {
  measurement: AvailabilityMeasurement;
  ownOrganization: boolean;
}) {
  const t = useTranslation();
  const { locale, timeZone } = useLocalization();
  const uptime = availabilityVerdict(measurement);
  const latency = latencyVerdict(measurement);
  const agreed = hasAgreedTarget(measurement);

  return (
    <div className="space-y-6">
      <Card>
        <CardContent className="space-y-5 p-6">
          <div className="flex flex-wrap items-baseline justify-between gap-x-4 gap-y-1">
            <h3 className="font-mono text-xs uppercase tracking-widest text-slate-400">
              {periodLabel(measurement, locale, timeZone)}
            </h3>
            <p className="font-mono text-[11px] uppercase tracking-widest text-slate-400">
              <LocalTime value={measurement.periodStart} dateOnly /> —{" "}
              <LocalTime value={measurement.periodEnd} dateOnly />
            </p>
          </div>

          <div className="grid gap-6 sm:grid-cols-2">
            <Metric
              label={t("availability.uptime")}
              value={formatUptime(measurement.uptimePercentage)}
              verdict={uptime}
              target={
                measurement.availabilityTarget === null
                  ? null
                  : `target ${formatTarget(measurement.availabilityTarget)}`
              }
              sentence={availabilitySentence(measurement)}
            />

            <Metric
              label={percentileLabel(measurement)}
              value={`under ${measurement.percentileLatencyMilliseconds} ms`}
              verdict={latency}
              target={
                measurement.latencyTargetMilliseconds === null
                  ? null
                  : `target ${measurement.latencyTargetMilliseconds} ms`
              }
              sentence={latencySentence(measurement)}
              footnote={PERCENTILE_IS_A_BOUND}
            />
          </div>

          {!agreed && (
            <p className="rounded-md border border-slate-200 bg-slate-50 px-4 py-3 text-sm leading-relaxed text-slate-600">
              {NO_TARGET_AGREED}
            </p>
          )}

          {isThinlyMeasured(measurement) && (
            <p className="rounded-md border border-attention-200 bg-attention-50 px-4 py-3 text-sm leading-relaxed text-attention-700">
              {thinTrafficNote(measurement)}
            </p>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardContent className="grid gap-x-8 gap-y-4 p-6 sm:grid-cols-2">
          <Readout
            label={t("availability.requests")}
            value={measurement.totalRequests.toLocaleString("en-GB")}
          />
          <Readout
            label={t("availability.failed")}
            value={measurement.failedRequests.toLocaleString("en-GB")}
          />
          <Readout
            label={t("availability.meanLatency")}
            value={`${measurement.meanLatencyMilliseconds} ms`}
          />
          <Readout
            label={t("availability.slowestRequest")}
            value={`${measurement.maxLatencyMilliseconds} ms`}
          />
          <p className="text-xs leading-relaxed text-slate-500 sm:col-span-2">
            {failureSentence(measurement)}{" "}
            {ownOrganization
              ? "Announced maintenance is excluded from both figures."
              : "Announced maintenance is excluded from both figures for this organisation."}
          </p>
        </CardContent>
      </Card>
    </div>
  );
}

function Metric({
  label,
  value,
  verdict,
  target,
  sentence,
  footnote,
}: {
  label: string;
  value: string;
  verdict: TargetVerdict;
  target: string | null;
  sentence: string;
  footnote?: string;
}) {
  const tone = verdictTone(verdict);

  return (
    <div className="space-y-2">
      <div className="flex flex-wrap items-center gap-2">
        <p className="font-mono text-[11px] uppercase tracking-widest text-slate-400">
          {label}
        </p>
        <ToneBadge tone={tone}>{VERDICT_LABELS[verdict]}</ToneBadge>
      </div>
      <p className="font-mono text-2xl tracking-tight text-slate-900">
        {value}
      </p>
      {target !== null && (
        <p className="font-mono text-xs text-slate-500">{target}</p>
      )}
      <p className="text-xs leading-relaxed text-slate-500">{sentence}</p>
      {footnote && (
        <p className="text-xs leading-relaxed text-slate-400">{footnote}</p>
      )}
    </div>
  );
}

function Readout({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex items-baseline justify-between gap-4">
      <span className="font-mono text-[11px] uppercase tracking-widest text-slate-400">
        {label}
      </span>
      <span className="font-mono text-sm text-slate-700">{value}</span>
    </div>
  );
}
