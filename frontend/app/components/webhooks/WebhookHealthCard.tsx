"use client";

import { useCallback, useEffect, useState } from "react";
import { Power, RotateCw, TriangleAlert } from "lucide-react";
import { toast } from "sonner";

import { WebhookEndpointHealth } from "@/types";
import { LocalTime } from "@/components/LocalTime";
import { ToneBadge } from "@/components/status/StatusChips";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { API_BASE, reportApiError, unwrap } from "@/lib/api";
import { apiFetch } from "@/lib/http";
import { cn } from "@/lib/utils";
import {
  DISABLE_STATE_LABELS,
  PENDING_MEANS_WAITING,
  RE_ENABLE_RECOVERS,
  disableState,
  healthSentence,
  healthTone,
  windowLength,
} from "@/lib/webhooks";
import { useTranslation } from "@/context/LocalizationContext";

export function WebhookHealthCard({
  organizationSegment,
  webhookConfigurationId,
  canWrite,
  refreshKey = 0,
  onEnable,
  enabling = false,
  onUnauthorized,
}: {
  organizationSegment: string;
  webhookConfigurationId: string;
  canWrite: boolean;
  refreshKey?: number;
  onEnable?: () => void;
  enabling?: boolean;
  onUnauthorized: () => void;
}) {
  const t = useTranslation();
  const [health, setHealth] = useState<WebhookEndpointHealth | null>(null);
  const [loading, setLoading] = useState<boolean>(true);

  const fetchHealth = useCallback(async () => {
    setLoading(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationSegment}/webhook-configurations/${webhookConfigurationId}/health`,
        { credentials: "include" }
      );

      if (response.ok) {
        setHealth(await unwrap<WebhookEndpointHealth>(response));
      } else {
        setHealth(null);
        await reportApiError(response, {
          fallback: t("webhooks.failedReadEndpointSHealth"),
          onUnauthorized,
          toastId: "error-webhook-health",
        });
      }
    } catch {
      setHealth(null);
      toast.error(t("webhooks.failedReadEndpointSHealth"), {
        id: "error-webhook-health",
      });
    } finally {
      setLoading(false);
    }
  }, [organizationSegment, webhookConfigurationId, onUnauthorized]);

  useEffect(() => {
    fetchHealth();
  }, [fetchHealth, refreshKey]);

  if (loading) {
    return (
      <Card>
        <CardContent className="space-y-3 pt-6">
          <Skeleton className="h-4 w-40" />
          <Skeleton className="h-8 w-full" />
          <Skeleton className="h-4 w-2/3" />
        </CardContent>
      </Card>
    );
  }

  if (health === null) {
    return (
      <Card>
        <CardContent className="space-y-3 pt-6">
          <h2 className="font-mono text-sm font-medium text-slate-900">
            {t("webhooks.endpointHealth")}
          </h2>
          <p className="max-w-prose text-sm text-slate-500">
            The delivery counters could not be read. Nothing about the endpoint
            itself has changed — this panel is a report, not a control.
          </p>
          <Button variant="outline" size="sm" onClick={() => fetchHealth()}>
            <RotateCw className="size-4" />
            {t("common.retry")}
          </Button>
        </CardContent>
      </Card>
    );
  }

  const state = disableState(health);
  const window = windowLength(health);

  return (
    <Card>
      <CardContent className="space-y-5 pt-6">
        <div className="flex flex-wrap items-start justify-between gap-3">
          <div className="space-y-1.5">
            <div className="flex flex-wrap items-center gap-2">
              <h2 className="font-mono text-sm font-medium text-slate-900">
                {t("webhooks.endpointHealth")}
              </h2>
              <ToneBadge tone={healthTone(health)}>
                {DISABLE_STATE_LABELS[state]}
              </ToneBadge>
            </div>
            <p className="max-w-prose text-sm text-slate-500">
              {healthSentence(health)}
            </p>
          </div>

          <div className="flex items-center gap-2">
            <Button
              variant="ghost"
              size="sm"
              className="gap-1.5 text-slate-500"
              onClick={() => fetchHealth()}
            >
              <RotateCw className="size-4" />
              {t("webhooks.refresh")}
            </Button>
            {canWrite && onEnable && !health.isEnabled && (
              <Button size="sm" onClick={onEnable} disabled={enabling}>
                <Power className="size-4" />
                {enabling ? "Enabling..." : "Enable"}
              </Button>
            )}
          </div>
        </div>

        {health.disabledAt !== null && (
          <div className="space-y-2 rounded-lg border border-critical-200 bg-critical-50 px-3 py-2.5">
            <div className="flex items-start gap-2.5">
              <TriangleAlert
                aria-hidden
                className="mt-0.5 size-4 flex-shrink-0 text-critical-700"
              />
              <div className="space-y-1">
                <p className="text-sm font-medium text-critical-700">
                  Switched off by the platform{" "}
                  <LocalTime
                    value={health.disabledAt}
                    className="font-normal"
                  />
                </p>
                {health.disabledReason && (
                  <p className="text-sm leading-relaxed text-critical-700">
                    {health.disabledReason}
                  </p>
                )}
                {canWrite && (
                  <p className="text-xs leading-relaxed text-critical-700/80">
                    {RE_ENABLE_RECOVERS}
                  </p>
                )}
              </div>
            </div>
          </div>
        )}

        <div className="space-y-2">
          <p className="font-mono text-[11px] uppercase tracking-widest text-slate-400">
            Last {window}
          </p>
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
            <Tally
              label={t("webhooks.delivered")}
              value={health.delivered}
              tone="settled"
              sentence="Accepted by the endpoint."
            />
            <Tally
              label={t("webhooks.failed")}
              value={health.failed}
              tone="critical"
              sentence="Out of retries. These are the ones to look at."
            />
            <Tally
              label={t("webhooks.pending")}
              value={health.pending}
              tone="inflight"
              sentence={PENDING_MEANS_WAITING}
            />
          </div>
          <p className="font-mono text-[11px] text-slate-400">
            <LocalTime value={health.from} /> — <LocalTime value={health.to} />
          </p>
        </div>

        <div className="grid grid-cols-1 gap-x-8 gap-y-3 border-t border-slate-100 pt-4 sm:grid-cols-3">
          <Readout
            label={t("webhooks.consecutiveFailures")}
            value={String(health.consecutiveFailures)}
            tone={health.consecutiveFailures > 0 ? "attention" : undefined}
          />
          <Readout
            label={t("webhooks.lastSucceeded")}
            value={health.lastSucceededAt}
            asTime
          />
          <Readout label={t("webhooks.lastFailed")} value={health.lastFailedAt} asTime />
        </div>
      </CardContent>
    </Card>
  );
}

function Tally({
  label,
  value,
  tone,
  sentence,
}: {
  label: string;
  value: number;
  tone: "settled" | "critical" | "inflight";
  sentence: string;
}) {
  const figure = {
    settled: "text-settled-700",
    critical: "text-critical-700",
    inflight: "text-inflight-700",
  }[tone];

  return (
    <div className="space-y-1">
      <p className="font-mono text-[11px] uppercase tracking-widest text-slate-400">
        {label}
      </p>
      <p className={cn("font-mono text-2xl tracking-tight", figure)}>{value}</p>
      <p className="text-xs leading-relaxed text-slate-500">{sentence}</p>
    </div>
  );
}

function Readout({
  label,
  value,
  asTime = false,
  tone,
}: {
  label: string;
  value: string | null;
  asTime?: boolean;
  tone?: "attention";
}) {
  const t = useTranslation();
  return (
    <div className="space-y-0.5">
      <p className="font-mono text-[11px] uppercase tracking-widest text-slate-400">
        {label}
      </p>
      {value === null ? (
        <p className="font-mono text-sm text-slate-400">{t("webhooks.never")}</p>
      ) : asTime ? (
        <LocalTime
          value={value}
          className="font-mono text-sm text-slate-700"
        />
      ) : (
        <p
          className={cn(
            "font-mono text-sm",
            tone === "attention" ? "text-attention-700" : "text-slate-700"
          )}
        >
          {value}
        </p>
      )}
    </div>
  );
}
