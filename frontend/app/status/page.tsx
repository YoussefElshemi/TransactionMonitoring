"use client";

import { useCallback, useEffect, useState } from "react";
import { CheckCircle2, CircleAlert, Loader2, Wrench } from "lucide-react";

import type { Incident, MaintenanceWindow, ServiceStatus } from "@/types";
import {
  COMPONENT_DESCRIPTIONS,
  COMPONENT_STATUS_DESCRIPTIONS,
  STATUS_IS_DERIVED,
  STATUS_POLL_INTERVAL_MS,
  SUBSCRIBE_ACKNOWLEDGEMENT,
  SUBSCRIBE_HINT,
  componentLabel,
  componentsInOrder,
  componentsSentence,
  formatLatency,
  formatUptime,
  isPlausibleEmail,
  overallDetail,
  overallHeadline,
  uptimeCoverageNote,
} from "@/lib/status";
import { statusFetch } from "@/lib/statusClient";
import { durationSentence, timelineNewestFirst } from "@/lib/incidents";
import { soonestFirst, windowLength } from "@/lib/maintenanceWindows";
import { LocalTime } from "@/components/LocalTime";
import {
  ComponentStatusChip,
  IncidentImpactChip,
  IncidentStatusChip,
  MaintenanceStatusChip,
} from "@/components/status/StatusChips";
import { UptimeBar, UptimeLegend } from "@/components/status/UptimeBar";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import { cn } from "@/lib/utils";

export default function StatusPage() {
  const [status, setStatus] = useState<ServiceStatus | null>(null);
  const [loading, setLoading] = useState(true);
  const [unreachable, setUnreachable] = useState(false);

  const fetchStatus = useCallback(async () => {
    try {
      const response = await statusFetch("/status", {
        method: "GET",
        headers: { "Content-Type": "application/json" },
      });

      if (!response.ok) {
        setUnreachable(true);
        return;
      }

      const { data } = (await response.json()) as { data: ServiceStatus };
      setStatus(data);
      setUnreachable(false);
    } catch {
      setUnreachable(true);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchStatus();

    const timer = setInterval(fetchStatus, STATUS_POLL_INTERVAL_MS);

    return () => clearInterval(timer);
  }, [fetchStatus]);

  if (loading) return <StatusSkeleton />;

  if (status === null) {
    return (
      <div className="space-y-8">
        <Card className="border-attention-200 bg-attention-50">
          <CardContent className="flex items-start gap-3 p-6">
            <CircleAlert className="mt-0.5 size-5 flex-shrink-0 text-attention-700" />
            <div>
              <h1 className="font-mono text-lg font-medium tracking-tight text-attention-700">
                This page cannot reach the status service
              </h1>
              <p className="mt-2 text-sm leading-relaxed text-slate-600">
                That is not the same as the service being down, and it is not
                the same as it being up — this page simply has no answer to give
                you right now. It is retrying every minute.
              </p>
            </div>
          </CardContent>
        </Card>
        <SubscribeBox />
      </div>
    );
  }

  const components = componentsInOrder(status.components);

  return (
    <div className="space-y-10">
      <OverallBanner status={status} />

      {unreachable && (
        <p className="rounded-md border border-attention-200 bg-attention-50 px-4 py-3 text-sm leading-relaxed text-attention-700">
          The last refresh did not get through. What is shown below was last
          read at <LocalTime value={status.observedAt} />, and may no longer be
          current.
        </p>
      )}

      {status.activeIncidents.length > 0 && (
        <section className="space-y-4">
          <SectionHeading
            title="Open incidents"
            count={status.activeIncidents.length}
          />
          {status.activeIncidents.map((incident) => (
            <IncidentCard key={incident.incidentId} incident={incident} />
          ))}
        </section>
      )}

      <section className="space-y-4">
        <SectionHeading title="Components" />
        <Card>
          <CardContent className="divide-y divide-slate-100 p-0">
            {components.map((component) => (
              <ComponentRow key={component.component} health={component} />
            ))}
          </CardContent>
        </Card>
        <div className="flex flex-wrap items-center justify-between gap-3">
          <UptimeLegend />
          <p className="font-mono text-[11px] uppercase tracking-widest text-slate-400">
            Last 90 days
          </p>
        </div>
        <p className="max-w-2xl text-xs leading-relaxed text-slate-500">
          {STATUS_IS_DERIVED}
        </p>
      </section>

      {status.upcomingMaintenance.length > 0 && (
        <section className="space-y-4">
          <SectionHeading
            title="Scheduled maintenance"
            count={status.upcomingMaintenance.length}
          />
          {soonestFirst(status.upcomingMaintenance).map((window) => (
            <MaintenanceCard
              key={window.maintenanceWindowId}
              window={window}
            />
          ))}
        </section>
      )}

      <SubscribeBox />

      <p className="text-xs text-slate-400">
        Observed at <LocalTime value={status.observedAt} />. This page refreshes
        itself every minute.
      </p>
    </div>
  );
}

function OverallBanner({ status }: { status: ServiceStatus }) {
  const operational = status.status === "Operational";
  const maintenance = status.status === "UnderMaintenance";

  const Icon = operational ? CheckCircle2 : maintenance ? Wrench : CircleAlert;

  return (
    <Card
      className={cn(
        operational
          ? "border-settled-200 bg-settled-50"
          : maintenance
            ? "border-inflight-200 bg-inflight-50"
            : "border-critical-200 bg-critical-50"
      )}
    >
      <CardContent className="flex items-start gap-4 p-6">
        <Icon
          className={cn(
            "mt-0.5 size-6 flex-shrink-0",
            operational
              ? "text-settled-700"
              : maintenance
                ? "text-inflight-700"
                : "text-critical-700"
          )}
        />
        <div className="min-w-0">
          <h1
            className={cn(
              "font-mono text-lg font-medium tracking-tight",
              operational
                ? "text-settled-700"
                : maintenance
                  ? "text-inflight-700"
                  : "text-critical-700"
            )}
          >
            {overallHeadline(status.status)}
          </h1>
          <p className="mt-1.5 text-sm leading-relaxed text-slate-600">
            {overallDetail(status)}
          </p>
        </div>
      </CardContent>
    </Card>
  );
}

function ComponentRow({
  health,
}: {
  health: ServiceStatus["components"][number];
}) {
  const coverage = uptimeCoverageNote(health.history);

  return (
    <div className="space-y-3 px-6 py-5">
      <div className="flex flex-wrap items-start justify-between gap-x-4 gap-y-2">
        <div className="min-w-0">
          <p className="font-mono text-sm font-medium text-slate-900">
            {componentLabel(health.component)}
          </p>
          <p className="mt-1 max-w-xl text-xs leading-relaxed text-slate-500">
            {COMPONENT_DESCRIPTIONS[health.component]}
          </p>
        </div>
        <div className="flex flex-shrink-0 items-center gap-2">
          <ComponentStatusChip status={health.status} />
        </div>
      </div>

      <UptimeBar history={health.history} />

      <div className="flex flex-wrap items-baseline justify-between gap-x-6 gap-y-1">
        <p className="text-xs leading-relaxed text-slate-500">
          {COMPONENT_STATUS_DESCRIPTIONS[health.status]}
        </p>
        <p
          className="font-mono text-xs text-slate-500"
          title="Uptime across the whole 90-day window, not over the last day."
        >
          {formatUptime(health.uptimePercentage)} over 90 days
        </p>
      </div>

      {coverage !== null && (
        <p className="text-xs leading-relaxed text-slate-500">{coverage}</p>
      )}

      <p className="font-mono text-[11px] uppercase tracking-widest text-slate-400">
        {health.lastProbedAt === null ? (
          "Never probed"
        ) : (
          <>
            Last probed <LocalTime value={health.lastProbedAt} /> ·{" "}
            {formatLatency(health.latencyMilliseconds)}
          </>
        )}
      </p>
    </div>
  );
}

function IncidentCard({ incident }: { incident: Incident }) {
  const updates = timelineNewestFirst(incident.updates);

  return (
    <Card>
      <CardContent className="space-y-4 p-6">
        <div className="flex flex-wrap items-start justify-between gap-x-4 gap-y-2">
          <div className="min-w-0">
            <p className="font-mono text-xs uppercase tracking-widest text-slate-400">
              {incident.incidentNumber}
            </p>
            <h3 className="mt-1 font-mono text-base font-medium tracking-tight text-slate-900">
              {incident.title}
            </h3>
          </div>
          <div className="flex flex-shrink-0 flex-wrap items-center gap-2">
            <IncidentStatusChip status={incident.status} />
            <IncidentImpactChip impact={incident.impact} />
          </div>
        </div>

        <p className="text-sm leading-relaxed text-slate-600">
          {incident.summary}
        </p>

        <div className="flex flex-wrap gap-x-6 gap-y-1 font-mono text-[11px] uppercase tracking-widest text-slate-400">
          <span>Affecting {componentsSentence(incident.components)}</span>
          <span>
            Started <LocalTime value={incident.startedAt} />
          </span>
        </div>

        <p className="text-xs leading-relaxed text-slate-500">
          {durationSentence(incident)}
        </p>

        {updates.length > 0 && (
          <ol className="space-y-3 border-l-2 border-slate-200 pl-4">
            {updates.map((update) => (
              <li key={update.incidentUpdateId}>
                <div className="flex flex-wrap items-center gap-2">
                  <IncidentStatusChip status={update.status} />
                  <span className="font-mono text-[11px] uppercase tracking-widest text-slate-400">
                    <LocalTime value={update.postedAt} />
                  </span>
                </div>
                <p className="mt-1.5 whitespace-pre-wrap text-sm leading-relaxed text-slate-600">
                  {update.body}
                </p>
              </li>
            ))}
          </ol>
        )}

        {incident.reviewPublishedAt !== null && (
          <div className="space-y-3 rounded-md border border-slate-200 bg-slate-50 p-4">
            <p className="font-mono text-[11px] uppercase tracking-widest text-slate-400">
              Post-incident review
            </p>
            {incident.rootCause && (
              <div>
                <p className="font-mono text-xs text-slate-500">Root cause</p>
                <p className="mt-1 whitespace-pre-wrap text-sm leading-relaxed text-slate-600">
                  {incident.rootCause}
                </p>
              </div>
            )}
            {incident.remediation && (
              <div>
                <p className="font-mono text-xs text-slate-500">Remediation</p>
                <p className="mt-1 whitespace-pre-wrap text-sm leading-relaxed text-slate-600">
                  {incident.remediation}
                </p>
              </div>
            )}
          </div>
        )}
      </CardContent>
    </Card>
  );
}

function MaintenanceCard({ window }: { window: MaintenanceWindow }) {
  return (
    <Card>
      <CardContent className="space-y-3 p-6">
        <div className="flex flex-wrap items-start justify-between gap-x-4 gap-y-2">
          <h3 className="min-w-0 font-mono text-base font-medium tracking-tight text-slate-900">
            {window.title}
          </h3>
          <MaintenanceStatusChip status={window.status} />
        </div>

        <p className="text-sm leading-relaxed text-slate-600">
          {window.description}
        </p>

        <div className="flex flex-wrap gap-x-6 gap-y-1 font-mono text-[11px] uppercase tracking-widest text-slate-400">
          <span>Affecting {componentsSentence(window.components)}</span>
          <span>
            <LocalTime value={window.scheduledStart} /> —{" "}
            <LocalTime value={window.scheduledEnd} />
          </span>
          <span>{windowLength(window)}</span>
        </div>
      </CardContent>
    </Card>
  );
}

function SubscribeBox() {
  const [email, setEmail] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [acknowledged, setAcknowledged] = useState(false);
  const [failed, setFailed] = useState(false);

  const valid = isPlausibleEmail(email);

  const subscribe = async () => {
    if (!valid) return;

    setSubmitting(true);
    setFailed(false);

    try {
      const response = await statusFetch("/status/subscriptions", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ email: email.trim() }),
      });

      if (response.ok) {
        setAcknowledged(true);
        setEmail("");
      } else {
        setFailed(true);
      }
    } catch {
      setFailed(true);
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Card>
      <CardContent className="space-y-3 p-6">
        <h2 className="font-mono text-sm font-medium tracking-tight text-slate-900">
          Get updates by email
        </h2>
        <p className="max-w-xl text-xs leading-relaxed text-slate-500">
          {SUBSCRIBE_HINT}
        </p>

        {acknowledged ? (
          <p className="rounded-md border border-settled-200 bg-settled-50 px-4 py-3 text-sm leading-relaxed text-settled-700">
            {SUBSCRIBE_ACKNOWLEDGEMENT}
          </p>
        ) : (
          <form
            className="flex flex-wrap items-center gap-2"
            onSubmit={(event) => {
              event.preventDefault();
              subscribe();
            }}
          >
            <Input
              type="email"
              value={email}
              onChange={(event) => setEmail(event.target.value)}
              placeholder="you@example.com"
              aria-label="Email address"
              className="w-full max-w-xs"
            />
            <Button type="submit" disabled={!valid || submitting}>
              {submitting && <Loader2 className="size-4 animate-spin" />}
              Subscribe
            </Button>
          </form>
        )}

        {failed && (
          <p className="text-sm leading-relaxed text-critical-700">
            That could not be submitted just now. This is a problem sending the
            request, not a problem with the address — try again in a moment.
          </p>
        )}
      </CardContent>
    </Card>
  );
}

function SectionHeading({ title, count }: { title: string; count?: number }) {
  return (
    <div className="flex items-baseline gap-2">
      <h2 className="font-mono text-xs uppercase tracking-widest text-slate-400">
        {title}
      </h2>
      {count !== undefined && (
        <span className="font-mono text-xs text-slate-400">({count})</span>
      )}
    </div>
  );
}

function StatusSkeleton() {
  return (
    <div className="space-y-10">
      <Skeleton className="h-28 w-full rounded-lg" />
      <div className="space-y-4">
        <Skeleton className="h-4 w-24" />
        <Skeleton className="h-80 w-full rounded-lg" />
      </div>
    </div>
  );
}
