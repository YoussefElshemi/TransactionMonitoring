"use client";

import { useCallback, useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { toast } from "sonner";
import { CheckCheck, Coffee, Hand, Inbox } from "lucide-react";

import { CaseResource, Queue, QueueResource, RoutingPolicy } from "@/types";
import { API_BASE, reportApiError, unwrap, unwrapPage } from "@/lib/api";
import {
  ROUTING_POLICY_HINTS,
  criteriaChips,
  isCatchAll,
  routingPolicyLabel,
} from "@/lib/queues";
import { PageHeader } from "@/components/PageHeader";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";

import { apiFetch } from "@/lib/http";
import { useTranslation } from "@/context/LocalizationContext";

export function MyQueuesPanel({
  caseHref,
  myCasesHref,
  onUnauthorized,
}: {
  caseHref: (caseId: string) => string;
  myCasesHref: string;
  onUnauthorized: () => void;
}) {
  const t = useTranslation();
  const router = useRouter();

  const [queues, setQueues] = useState<Queue[] | null>(null);
  const [loading, setLoading] = useState(true);
  const [claiming, setClaiming] = useState<string | null>(null);
  const [emptyDesk, setEmptyDesk] = useState<{ queueName: string | null } | null>(
    null
  );

  const fetchQueues = useCallback(async () => {
    setLoading(true);

    try {
      const response = await apiFetch(`${API_BASE}/queues/my?PageNumber=1&PageSize=100`, {
        method: "GET",
        headers: { "Content-Type": "application/json" },
        credentials: "include",
      });

      if (response.ok) {
        const page = await unwrapPage<QueueResource>(response);
        setQueues(page.data.map(({ queue }) => queue));
      } else {
        setQueues(null);
        await reportApiError(response, {
          fallback: t("queues.errorOccurredFetchingYourQueues"),
          onUnauthorized,
          toastId: "error-fetching-my-queues",
        });
      }
    } catch {
      setQueues(null);
      toast.error(t("queues.errorOccurredFetchingYourQueues"), {
        id: "error-fetching-my-queues",
      });
    } finally {
      setLoading(false);
    }
  }, [onUnauthorized]);

  useEffect(() => {
    fetchQueues();
  }, [fetchQueues]);

  const claimNext = async (queue?: Queue) => {
    setClaiming(queue?.queueId ?? "all");
    setEmptyDesk(null);

    try {
      const url = queue
        ? `${API_BASE}/queues/${queue.queueId}/claim-next`
        : `${API_BASE}/queues/claim-next`;

      const response = await apiFetch(url, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        credentials: "include",
      });

      if (response.status === 204) {
        setEmptyDesk({ queueName: queue?.name ?? null });
        return;
      }

      if (response.ok) {
        const { case: claimed } = await unwrap<CaseResource>(response);
        toast.success(t("queues.caseIsYours", claimed.friendlyName), {
          description:
            t("queues.handedOutMostSevereFirst"),
        });
        router.push(caseHref(claimed.caseId));
        return;
      }

      if (response.status === 403) {
        toast.error(t("queues.noLongerOnQueue", queue?.name ?? t("queues.thisQueueLower")), {
          description: t("queues.onlyMembersCanClaimFrom"),
        });
        fetchQueues();
        return;
      }

      if (response.status === 404) {
        toast.error(t(
            "queues.queueNoLongerAvailable",
            queue?.name ?? t("queues.thisQueueUpper")
          ), {
          description: t("queues.hasBeenRetiredSoNothing"),
        });
        fetchQueues();
        return;
      }

      await reportApiError(response, {
        fallback: t("queues.failedClaimNextCase"),
        onUnauthorized,
      });
    } catch {
      toast.error(t("queues.failedClaimNextCase"));
    } finally {
      setClaiming(null);
    }
  };

  const busy = claiming !== null;

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow={t("queues.myDesk")}
        title={t("queues.myQueues")}
        description={t("queues.theDesksYouAreOn")}
      >
        <Button
          variant="outline"
          size="sm"
          className="gap-1.5"
          onClick={() => router.push(myCasesHref)}
        >
          <Inbox className="size-4" />
          {t("queues.myCases")}
        </Button>
      </PageHeader>

      {loading ? (
        <div className="space-y-4">
          <Skeleton className="h-28 w-full" />
          <Skeleton className="h-40 w-full" />
        </div>
      ) : (queues?.length ?? 0) === 0 ? (
        <Card>
          <CardContent className="py-14 text-center">
            <p className="font-mono text-xs uppercase tracking-widest text-slate-400">
              {t("queues.youAreNotOnAny")}
            </p>
            <p className="mx-auto mt-3 max-w-md text-sm text-slate-500">
              Membership is what makes a queue claimable, and a supervisor sets
              it. Your organization may also route nothing at all — in which case
              cases are assigned by hand, as they always were.
            </p>
            <Button
              variant="outline"
              size="sm"
              className="mt-5 gap-1.5"
              onClick={() => router.push(myCasesHref)}
            >
              <Inbox className="size-4" />
              {t("queues.goToMyCases")}
            </Button>
          </CardContent>
        </Card>
      ) : (
        <>
          <Card>
            <CardContent className="flex flex-wrap items-center justify-between gap-4 pt-6">
              <div className="min-w-0">
                <h2 className="font-mono text-base font-medium text-slate-900">
                  {t("queues.nextPieceOfWork")}
                </h2>
                <p className="mt-1 max-w-lg text-sm text-slate-500">
                  Pulls across all {queues!.length} of your queue
                  {queues!.length === 1 ? "" : "s"} at once, in priority order.
                  If someone else takes it first you are handed a different case,
                  never an error.
                </p>
              </div>
              <Button
                size="lg"
                className="gap-2"
                disabled={busy}
                onClick={() => claimNext()}
              >
                <Hand className="size-4" />
                {claiming === "all" ? "Claiming..." : "Claim next"}
              </Button>
            </CardContent>
          </Card>

          {emptyDesk && (
            <Card>
              <CardContent className="flex items-start gap-3 pt-6">
                <Coffee className="mt-0.5 size-5 flex-shrink-0 text-settled-700" />
                <div>
                  <p className="font-medium text-slate-900">
                    {emptyDesk.queueName
                      ? `Nothing waiting on ${emptyDesk.queueName}.`
                      : "Nothing waiting on any of your queues."}
                  </p>
                  <p className="mt-1 text-sm text-slate-500">
                    Everything routed to you has been picked up. Try again when
                    new work arrives.
                  </p>
                  <div className="mt-3 flex items-center gap-2">
                    <Button
                      variant="outline"
                      size="sm"
                      className="gap-1.5"
                      disabled={busy}
                      onClick={() => claimNext()}
                    >
                      <CheckCheck className="size-4" />
                      {t("queues.checkAgain")}
                    </Button>
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => setEmptyDesk(null)}
                    >
                      {t("queues.dismiss")}
                    </Button>
                  </div>
                </div>
              </CardContent>
            </Card>
          )}

          <div className="grid gap-4 lg:grid-cols-2">
            {queues!.map((queue) => {
              const chips = criteriaChips(queue);

              return (
                <Card key={queue.queueId}>
                  <CardContent className="space-y-3 pt-6">
                    <div className="flex items-start justify-between gap-3">
                      <div className="min-w-0">
                        <h3 className="truncate font-mono text-base font-medium text-slate-900">
                          {queue.name}
                        </h3>
                        {queue.description && (
                          <p className="mt-0.5 text-sm text-slate-500">
                            {queue.description}
                          </p>
                        )}
                      </div>
                      <span
                        className="flex-shrink-0 font-mono text-[11px] uppercase tracking-widest text-slate-400"
                        title={
                          ROUTING_POLICY_HINTS[queue.routingPolicy as RoutingPolicy]
                        }
                      >
                        {routingPolicyLabel(queue.routingPolicy)}
                      </span>
                    </div>

                    <div className="flex flex-wrap items-center gap-1">
                      {isCatchAll(queue) ? (
                        <span className="text-sm text-slate-500">
                          {t("queues.takesEverythingThatReachesIt")}
                        </span>
                      ) : (
                        chips.map((chip, position) => (
                          <span key={chip.key} className="flex items-center gap-1">
                            {position > 0 && (
                              <span className="font-mono text-[10px] uppercase tracking-widest text-slate-400">
                                and
                              </span>
                            )}
                            <span className="rounded-full border border-slate-200 bg-slate-50 px-2 py-0.5 text-xs text-slate-700">
                              {chip.label}
                            </span>
                          </span>
                        ))
                      )}
                    </div>

                    {!queue.isEnabled && (
                      <p className="rounded-md border border-slate-200 bg-slate-50 px-3 py-2 text-xs text-slate-600">
                        Paused — no new cases are being routed here. Anything
                        already waiting can still be claimed.
                      </p>
                    )}

                    <Button
                      variant="outline"
                      size="sm"
                      className="gap-1.5"
                      disabled={busy}
                      onClick={() => claimNext(queue)}
                    >
                      <Hand className="size-4" />
                      {claiming === queue.queueId
                        ? "Claiming..."
                        : "Claim from this queue"}
                    </Button>
                  </CardContent>
                </Card>
              );
            })}
          </div>
        </>
      )}
    </div>
  );
}
