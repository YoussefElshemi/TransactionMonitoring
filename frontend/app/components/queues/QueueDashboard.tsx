"use client";

import { useCallback, useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { toast } from "sonner";
import { Inbox, RefreshCw, Settings2, TriangleAlert } from "lucide-react";

import { QueueStatistic } from "@/types";
import { API_BASE, reportApiError, unwrap } from "@/lib/api";
import { hasIdleRoutingFromStatistic, oldestWaitingLabel } from "@/lib/queues";
import { PageHeader } from "@/components/PageHeader";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { QueueStatisticCard } from "./QueueStatisticCard";

import { apiFetch } from "@/lib/http";
import { useTranslation } from "@/context/LocalizationContext";

export function QueueDashboard({
  organizationId,
  queueHref,
  manageHref,
  unroutedHref,
  onUnauthorized,
}: {
  organizationId: string;
  queueHref: (queueId: string) => string;
  manageHref: string;
  unroutedHref: string;
  onUnauthorized: () => void;
}) {
  const t = useTranslation();
  const router = useRouter();

  const [statistics, setStatistics] = useState<QueueStatistic[] | null>(null);
  const [loading, setLoading] = useState(true);

  const fetchStatistics = useCallback(async () => {
    setLoading(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationId}/queues/statistics`,
        {
          method: "GET",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
        }
      );

      if (response.ok) {
        setStatistics(await unwrap<QueueStatistic[]>(response));
      } else {
        setStatistics(null);
        await reportApiError(response, {
          fallback: t("queues.errorOccurredFetchingQueueStatistics"),
          onUnauthorized,
          toastId: "error-fetching-queue-statistics",
        });
      }
    } catch {
      setStatistics(null);
      toast.error(t("queues.errorOccurredFetchingQueueStatistics"), {
        id: "error-fetching-queue-statistics",
      });
    } finally {
      setLoading(false);
    }
  }, [organizationId, onUnauthorized]);

  useEffect(() => {
    fetchStatistics();
  }, [fetchStatistics]);

  const misconfigured = (statistics ?? []).filter(hasIdleRoutingFromStatistic);

  const waits = (statistics ?? [])
    .map((statistic) => statistic.oldestWaitingForSeconds)
    .filter((seconds): seconds is number => seconds !== null);
  const worstWait = waits.length > 0 ? Math.max(...waits) : null;

  const totalWaiting = (statistics ?? []).reduce(
    (running, statistic) => running + statistic.depth,
    0
  );

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow={t("queues.queues2")}
        title={t("queues.queueDashboard")}
        description={t("queues.everyDeskInTheOrder")}
      >
        <Button
          variant="outline"
          size="sm"
          className="gap-1.5"
          onClick={fetchStatistics}
          disabled={loading}
        >
          <RefreshCw className="size-4" />
          {t("queues.refresh")}
        </Button>
        <Button
          variant="outline"
          size="sm"
          className="gap-1.5"
          onClick={() => router.push(manageHref)}
        >
          <Settings2 className="size-4" />
          {t("queues.configure")}
        </Button>
      </PageHeader>

      {loading && statistics === null ? (
        <div className="grid gap-4 lg:grid-cols-2">
          {Array.from({ length: 4 }).map((_, index) => (
            <Skeleton key={index} className="h-64 w-full" />
          ))}
        </div>
      ) : (statistics?.length ?? 0) === 0 ? (
        <Card>
          <CardContent className="py-14 text-center">
            <p className="font-mono text-xs uppercase tracking-widest text-slate-400">
              {t("queues.noQueuesConfiguredYet")}
            </p>
            <p className="mx-auto mt-3 max-w-md text-sm text-slate-500">
              Nothing is being routed, so every case opens unrouted and waits for
              a supervisor to assign it — exactly as it did before queues
              existed.
            </p>
            <div className="mt-5 flex items-center justify-center gap-2">
              <Button size="sm" onClick={() => router.push(manageHref)}>
                {t("queues.setUpAQueue")}
              </Button>
              <Button
                variant="outline"
                size="sm"
                className="gap-1.5"
                onClick={() => router.push(unroutedHref)}
              >
                <Inbox className="size-4" />
                {t("queues.seeTheUntriagedPile")}
              </Button>
            </div>
          </CardContent>
        </Card>
      ) : (
        <>
          <div className="grid gap-4 sm:grid-cols-3">
            <Headline
              label={t("queues.waitingAcrossAllDesks")}
              value={String(totalWaiting)}
              hint={t("queues.openCasesNobodyHasPicked")}
            />
            <Headline
              label={t("queues.longestWait")}
              value={oldestWaitingLabel(worstWait)}
              hint={
                worstWait === null
                  ? t("queues.nothingWaitingAnyQueue")
                  : t("queues.oldestUnclaimedCaseAnyQueue")
              }
            />
            <button
              type="button"
              onClick={() => router.push(unroutedHref)}
              className="rounded-xl border border-slate-200 bg-card p-4 text-left transition-colors hover:bg-slate-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-600"
            >
              <p className="font-mono text-[11px] uppercase tracking-widest text-slate-400">
                {t("queues.untriaged")}
              </p>
              <p className="mt-1 flex items-center gap-1.5 font-mono text-lg text-brand-800">
                <Inbox className="size-4" />
                {t("queues.unroutedCases")}
              </p>
              <p className="mt-1 text-xs text-slate-500">
                Cases no queue&apos;s criteria matched. They appear on none of the
                cards below, so this is the only way to see them.
              </p>
            </button>
          </div>

          {misconfigured.length > 0 && (
            <div className="flex items-start gap-2.5 rounded-lg border border-attention-200 bg-attention-50 px-4 py-3">
              <TriangleAlert className="mt-0.5 size-4 flex-shrink-0 text-attention-700" />
              <div className="text-sm text-attention-700">
                <p className="font-medium">
                  {misconfigured.length} queue
                  {misconfigured.length === 1 ? " is" : "s are"} set to hand work
                  out with nobody on{" "}
                  {misconfigured.length === 1 ? "it" : "them"}.
                </p>
                <p className="mt-0.5">
                  {misconfigured.map((statistic) => statistic.name).join(", ")}.
                  Cases still arrive there and sit unassigned.
                </p>
              </div>
            </div>
          )}

          <div className="grid gap-4 lg:grid-cols-2">
            {statistics!.map((statistic) => (
              <QueueStatisticCard
                key={statistic.queueId}
                statistic={statistic}
                href={queueHref(statistic.queueId)}
                unroutedHref={unroutedHref}
                memberHref={(userId) =>
                  `/cases?status=&queueMemberUserId=${userId}`
                }
              />
            ))}
          </div>
        </>
      )}
    </div>
  );
}

function Headline({
  label,
  value,
  hint,
}: {
  label: string;
  value: string;
  hint: string;
}) {
  return (
    <div className="rounded-xl border border-slate-200 bg-card p-4">
      <p className="font-mono text-[11px] uppercase tracking-widest text-slate-400">
        {label}
      </p>
      <p className="mt-1 font-mono text-lg text-slate-900">{value}</p>
      <p className="mt-1 text-xs text-slate-500">{hint}</p>
    </div>
  );
}
