"use client";

import { useCallback, useEffect, useState } from "react";
import { toast } from "sonner";
import { CalendarClock, CircleSlash, Plus, Receipt, TriangleAlert, X } from "lucide-react";

import { Plan, Subscription } from "@/types";
import { API_BASE, reportApiError, unwrap, unwrapPage } from "@/lib/api";
import {
  BACKDATED_MESSAGE,
  CANCEL_REOPENS_PREDECESSOR,
  CANCEL_TOO_LATE_MESSAGE,
  CANNOT_CANCEL_STARTED,
  NO_PLAN_DETAIL,
  NO_PLAN_HEADLINE,
  OWNERSHIP_LABELS,
  STARTS_LATER_HINT,
  STARTS_NOW_HINT,
  canCancel,
  effectiveFromRefusal,
  inForceSubscription,
  isValidEffectiveFrom,
  planOwnership,
  scheduledSubscriptions,
  successorOf,
  supersededSubscriptions,
  timelineSentence,
  versionLabel,
} from "@/lib/plans";
import { priceShapeSentence } from "@/lib/pricing";
import { canWriteBilling } from "@/lib/permissions";
import { usePermissions } from "@/context/PermissionsContext";
import { apiFetch } from "@/lib/http";
import { useIdempotencyKey } from "@/lib/useIdempotencyKey";
import { cn } from "@/lib/utils";
import { LocalTime } from "@/components/LocalTime";
import { PolicySwitch } from "@/components/PolicySwitch";
import { Button } from "@/components/ui/button";
import { DateTimePicker } from "@/components/DateTimePicker";
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
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { useTranslation } from "@/context/LocalizationContext";

const TIMELINE_PAGE_SIZE = 100;

const PLAN_PAGE_SIZE = 100;

type StartMode = "now" | "scheduled";

/** `2026-10-01T09:00` — what `<input type="datetime-local">` reads and writes.
 *  Built by hand rather than off `toISOString`, which would shift the displayed
 *  wall-clock time by the reader's offset. */
const toLocalInputValue = (date: Date): string => {
  const pad = (value: number) => String(value).padStart(2, "0");

  return (
    `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}` +
    `T${pad(date.getHours())}:${pad(date.getMinutes())}`
  );
};

export function SubscriptionsPanel({
  organizationId,
  onUnauthorized,
}: {
  organizationId: string;
  onUnauthorized: () => void;
}) {
  const t = useTranslation();
  const { grants } = usePermissions();
  const submission = useIdempotencyKey();

  const [timeline, setTimeline] = useState<Subscription[]>([]);
  const [history, setHistory] = useState<Subscription[]>([]);

  const [plans, setPlans] = useState<Plan[]>([]);
  const [plansLoading, setPlansLoading] = useState(false);

  const [includeSuperseded, setIncludeSuperseded] = useState(false);
  const [loading, setLoading] = useState(true);

  const [subscribeOpen, setSubscribeOpen] = useState(false);
  const [saving, setSaving] = useState(false);
  const [cancelTarget, setCancelTarget] = useState<Subscription | null>(null);
  const [cancelling, setCancelling] = useState(false);

  const [newPlanId, setNewPlanId] = useState<string>("");
  const [startMode, setStartMode] = useState<StartMode>("now");
  const [startsAt, setStartsAt] = useState<string>("");

  const canWrite = canWriteBilling(grants, "subscriptions:write");

  const fetchTimeline = useCallback(async () => {
    setLoading(true);

    try {
      const live = await apiFetch(
        `${API_BASE}/organizations/${organizationId}/subscriptions?PageNumber=1&PageSize=${TIMELINE_PAGE_SIZE}`,
        {
          method: "GET",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
        }
      );

      if (live.ok) {
        const page = await unwrapPage<Subscription>(live);
        setTimeline(page.data);
      } else {
        await reportApiError(live, {
          fallback: t("billing.errorOccurredFetchingSubscriptionTimeline"),
          onUnauthorized,
          toastId: "error-fetching-subscriptions",
        });
      }
    } catch {
      toast.error(t("billing.errorOccurredFetchingSubscriptionTimeline"), {
        id: "error-fetching-subscriptions",
      });
    } finally {
      setLoading(false);
    }
  }, [organizationId, onUnauthorized]);

  const fetchHistory = useCallback(async () => {
    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationId}/subscriptions?IncludeSuperseded=true&PageNumber=1&PageSize=${TIMELINE_PAGE_SIZE}`,
        {
          method: "GET",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
        }
      );

      if (response.ok) {
        const page = await unwrapPage<Subscription>(response);
        setHistory(page.data);
      } else {
        setHistory([]);
      }
    } catch {
      setHistory([]);
    }
  }, [organizationId]);

  const fetchPlans = useCallback(async () => {
    setPlansLoading(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/plans?OwnerOrganizationId=${organizationId}&PageNumber=1&PageSize=${PLAN_PAGE_SIZE}`,
        {
          method: "GET",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
        }
      );

      if (response.ok) {
        const page = await unwrapPage<Plan>(response);
        setPlans(page.data);
      } else {
        await reportApiError(response, {
          fallback: t("billing.errorOccurredFetchingPlanCatalogue"),
          onUnauthorized,
          toastId: "error-fetching-plans",
        });
      }
    } catch {
      toast.error(t("billing.errorOccurredFetchingPlanCatalogue"), {
        id: "error-fetching-plans",
      });
    } finally {
      setPlansLoading(false);
    }
  }, [organizationId, onUnauthorized]);

  useEffect(() => {
    fetchTimeline();
  }, [fetchTimeline]);

  useEffect(() => {
    if (includeSuperseded) fetchHistory();
  }, [includeSuperseded, fetchHistory]);

  const openSubscribe = () => {
    setNewPlanId("");
    setStartMode("now");
    // An hour out rather than now: the API refuses a stated time that is not
    // strictly in the future, and prefilling the current minute would produce
    // exactly that the moment the operator hesitated.
    setStartsAt(toLocalInputValue(new Date(Date.now() + 3_600_000)));
    setSubscribeOpen(true);
    fetchPlans();
  };

  const selectedPlan = plans.find((plan) => plan.planId === newPlanId) ?? null;

  const latestStart = timeline.reduce<string | null>(
    (latest, subscription) =>
      latest === null ||
      Date.parse(subscription.effectiveFrom) > Date.parse(latest)
        ? subscription.effectiveFrom
        : latest,
    null
  );

  const startsAtIso =
    startsAt.length > 0 ? new Date(startsAt).toISOString() : "";

  const startRefusal =
    startMode === "scheduled" && startsAt.length > 0
      ? effectiveFromRefusal(startsAtIso, new Date(), latestStart)
      : null;

  const subscribeBlocked =
    saving ||
    selectedPlan === null ||
    selectedPlan.currentVersion === null ||
    (startMode === "scheduled" &&
      !isValidEffectiveFrom(startsAtIso, new Date(), latestStart));

  const handleSubscribe = async () => {
    if (!selectedPlan?.currentVersion) return;

    const planVersionId = selectedPlan.currentVersion.planVersionId;
    const effectiveFrom =
      startMode === "scheduled" ? new Date(startsAt).toISOString() : null;

    setSaving(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationId}/subscriptions`,
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
          body: JSON.stringify({
            planVersionId,
            ...(effectiveFrom === null ? {} : { effectiveFrom }),
          }),
        },
        { idempotencyKey: submission.current("subscribe") }
      );

      if (response.ok) {
        const created = await unwrap<Subscription>(response);
        submission.completed("subscribe");

        toast.success(
          effectiveFrom === null
            ? t("billing.planNowInForce", selectedPlan.name)
            : t("billing.planScheduled", selectedPlan.name),
          {
            description: timelineSentence(created),
          }
        );

        setSubscribeOpen(false);
        await fetchTimeline();
        if (includeSuperseded) await fetchHistory();
      } else if (response.status === 409) {
        toast.error(t("billing.startDateRefused"), {
          description: BACKDATED_MESSAGE,
        });
      } else {
        await reportApiError(response, {
          fallback: t("billing.failedSubscribeOrganization"),
          onUnauthorized,
          fieldLabels: {
            "Body.PlanVersionId": "Plan",
            "Body.EffectiveFrom": "Starts",
            OrganizationId: "Organization",
          },
        });
      }
    } catch {
      toast.error(t("billing.failedSubscribeOrganization"));
    } finally {
      setSaving(false);
    }
  };

  const handleCancel = async () => {
    if (!cancelTarget) return;

    const { subscriptionId } = cancelTarget;
    setCancelling(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationId}/subscriptions/${subscriptionId}`,
        {
          method: "DELETE",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
        },
        // Keyed per row. The API binds a key to one method and path, so a key
        // shared across two rows would be a 409 rather than the protection it
        // looks like.
        { idempotencyKey: submission.current(`cancel:${subscriptionId}`) }
      );

      if (response.ok) {
        submission.completed(`cancel:${subscriptionId}`);

        toast.success(t("billing.scheduledChangeCancelled"), {
          description: CANCEL_REOPENS_PREDECESSOR,
        });

        setCancelTarget(null);
        await fetchTimeline();
      } else if (response.status === 409) {
        toast.error(t("billing.tooLateCancel"), {
          description: CANCEL_TOO_LATE_MESSAGE,
        });
        setCancelTarget(null);
        await fetchTimeline();
      } else {
        await reportApiError(response, {
          fallback: t("billing.failedCancelScheduledChange"),
          onUnauthorized,
        });
      }
    } catch {
      toast.error(t("billing.failedCancelScheduledChange"));
    } finally {
      setCancelling(false);
    }
  };

  const current = inForceSubscription(timeline);
  const scheduled = scheduledSubscriptions(timeline);
  const superseded = supersededSubscriptions(history);

  return (
    <div className="space-y-4">
      <div className="space-y-2">
        <p className="text-sm text-slate-500">
          What this tenant is charged, and since when. A price is a{" "}
          <span className="font-medium">plan version</span> they hold a
          subscription to over a stretch of time, not a number on their record —
          so repricing them is a new stretch with its own start date, and every
          invoice already issued goes on naming the version it was priced from.
        </p>
        <p className="text-sm text-slate-500">
          The timeline is appended to and never edited. A stretch that has begun
          is a period they demonstrably were on that plan; only a change that
          has not started yet can be called off.
        </p>
      </div>

      {loading ? (
        <Skeleton className="h-28 w-full rounded-xl" />
      ) : current ? (
        <div className="rounded-xl border border-elevated-200 bg-elevated-50 p-5">
          <div className="flex items-center justify-between gap-2">
            <span className="font-mono text-xs uppercase tracking-widest text-slate-500">
              {t("billing.inForce")}
            </span>
            <Receipt className="size-3.5 text-elevated-700" />
          </div>

          <p className="mt-2 text-2xl font-medium text-slate-900">
            {versionLabel(
              current.planName,
              current.planVersion?.versionNumber ?? null
            )}
          </p>

          <p className="mt-1 font-mono text-sm text-slate-700">
            {current.planVersion
              ?
                priceShapeSentence(current.planVersion)
              :
                "The API did not return the version this stretch points at, so its price cannot be shown."}
          </p>

          <p className="mt-2 text-xs text-elevated-700">
            {t("billing.since")} <LocalTime value={current.effectiveFrom} />
            {current.effectiveTo !== null && (
              <>
                {" · closes "}
                <LocalTime value={current.effectiveTo} />
              </>
            )}
          </p>
        </div>
      ) : (
        <div className="rounded-xl border border-attention-200 bg-attention-50 p-5">
          <div className="flex items-start gap-3">
            <CircleSlash className="mt-0.5 size-5 flex-shrink-0 text-attention-700" />
            <div className="space-y-2">
              <p className="text-lg font-medium text-attention-700">
                {NO_PLAN_HEADLINE}
              </p>
              <p className="max-w-2xl text-sm text-attention-700">
                {NO_PLAN_DETAIL}
              </p>
              {canWrite && (
                <Button size="sm" className="mt-1 gap-1.5" onClick={openSubscribe}>
                  <Plus className="size-4" />
                  {t("billing.subscribeToAPlan")}
                </Button>
              )}
            </div>
          </div>
        </div>
      )}

      {!loading && scheduled.length > 0 && (
        <div className="space-y-2 rounded-xl border border-slate-200 bg-card p-4">
          <div className="flex items-center gap-2">
            <CalendarClock className="size-3.5 text-slate-500" />
            <span className="font-mono text-xs uppercase tracking-widest text-slate-500">
              {t("billing.scheduled")}
            </span>
          </div>

          {scheduled.map((subscription) => (
            <div
              key={subscription.subscriptionId}
              className="flex flex-wrap items-start justify-between gap-3 rounded-lg border border-slate-200 p-3"
            >
              <div className="space-y-1">
                <p className="text-sm font-medium text-slate-900">
                  {versionLabel(
                    subscription.planName,
                    subscription.planVersion?.versionNumber ?? null
                  )}
                </p>
                {subscription.planVersion && (
                  <p className="font-mono text-xs text-slate-700">
                    {priceShapeSentence(subscription.planVersion)}
                  </p>
                )}
                <p className="text-xs text-slate-500">
                  {t("billing.takesOver")} <LocalTime value={subscription.effectiveFrom} />
                  {current !== null &&
                    ", when the stretch above closes at exactly that instant"}
                </p>
              </div>

              {canWrite && (
                <Button
                  variant="ghost"
                  size="sm"
                  className="gap-1.5 text-slate-500 hover:text-slate-900"
                  onClick={() => setCancelTarget(subscription)}
                >
                  <X className="size-4" />
                  {t("common.cancel")}
                </Button>
              )}
            </div>
          ))}
        </div>
      )}

      {!loading && current !== null && canWrite && (
        <div className="flex justify-end">
          <Button size="sm" className="gap-1.5" onClick={openSubscribe}>
            <Plus className="size-4" />
            {scheduled.length > 0 ? "Schedule another change" : "Change plan"}
          </Button>
        </div>
      )}

      <PolicySwitch
        label={t("billing.showSupersededStretches")}
        checked={includeSuperseded}
        onChange={setIncludeSuperseded}
      >
        Off by default, because a stretch that is over is not deciding anything.
        Turn it on to answer what this organisation was being charged in a given
        month. Nothing here can be changed.
      </PolicySwitch>

      {includeSuperseded && (
        <div className="overflow-hidden rounded-xl border border-slate-200 bg-card">
          <Table>
            <TableHeader>
              <TableRow className="bg-slate-50 hover:bg-slate-50">
                <TableHead className="w-56">{t("common.plan")}</TableHead>
                <TableHead className="w-40">{t("billing.rate")}</TableHead>
                <TableHead>{t("billing.theStretch")}</TableHead>
                <TableHead className="w-44">{t("billing.ended")}</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {superseded.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={4} className="py-10 text-center">
                    <p className="font-mono text-xs uppercase tracking-widest text-slate-400">
                      {t("billing.noSupersededStretches")}
                    </p>
                    <p className="mx-auto mt-3 max-w-md text-sm text-slate-500">
                      This organisation has not been moved between plans. What
                      it is on now is what it has always been on.
                    </p>
                  </TableCell>
                </TableRow>
              ) : (
                superseded.map((subscription) => (
                  <TableRow
                    key={subscription.subscriptionId}
                    className="text-slate-400 hover:bg-slate-50/50"
                  >
                    <TableCell className="text-slate-700">
                      {versionLabel(
                        subscription.planName,
                        subscription.planVersion?.versionNumber ?? null
                      )}
                    </TableCell>
                    <TableCell className="font-mono text-xs text-slate-700">
                      {subscription.planVersion
                        ? priceShapeSentence(subscription.planVersion)
                        : "—"}
                    </TableCell>
                    <TableCell className="max-w-md text-sm text-slate-500">
                      {timelineSentence(
                        subscription,
                        successorOf(subscription, history)
                      )}
                    </TableCell>
                    <TableCell className="text-xs text-slate-500">
                      {subscription.effectiveTo !== null && (
                        <LocalTime value={subscription.effectiveTo} />
                      )}
                    </TableCell>
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>
        </div>
      )}

      <Dialog open={subscribeOpen} onOpenChange={setSubscribeOpen}>
        <DialogContent className="sm:max-w-lg">
          <DialogHeader>
            <DialogTitle>
              {current ? "Change plan" : "Subscribe to a plan"}
            </DialogTitle>
            <DialogDescription>
              Sold at the plan&apos;s current version. Repricing the plan later
              does not move this organisation — that is another change, with its
              own date.
            </DialogDescription>
          </DialogHeader>

          <div className="max-h-[60vh] space-y-4 overflow-y-auto pr-1">
            <div className="space-y-1.5">
              <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
                {t("common.plan")}
              </label>
              <Select
                value={newPlanId}
                onValueChange={setNewPlanId}
                disabled={plansLoading}
              >
                <SelectTrigger>
                  <SelectValue
                    placeholder={
                      plansLoading ? t("billing.loadingPlans") : t("billing.choosePlan")
                    }
                  />
                </SelectTrigger>
                <SelectContent>
                  {plans
                    .filter((plan) => plan.currentVersion !== null)
                    .map((plan) => (
                      <SelectItem key={plan.planId} value={plan.planId}>
                        {plan.name} v{plan.currentVersionNumber} ·{" "}
                        {OWNERSHIP_LABELS[planOwnership(plan)]}
                      </SelectItem>
                    ))}
                </SelectContent>
              </Select>
              <p className="text-xs text-slate-500">
                The catalogue, plus any plan negotiated with this organisation
                alone. Somebody else&apos;s bespoke plan is never offered here.
              </p>
              {!plansLoading && plans.length === 0 && (
                <p className="text-xs text-critical-500">
                  There are no plans this organisation can be put on. Create one
                  in the plan catalogue first.
                </p>
              )}
            </div>

            {selectedPlan?.currentVersion && (
              <div className="rounded-lg border border-slate-200 p-3">
                <p className="font-mono text-sm text-slate-900">
                  {priceShapeSentence(selectedPlan.currentVersion)}
                </p>
                <p className="mt-1 text-xs text-slate-500">
                  {selectedPlan.name} v{selectedPlan.currentVersionNumber}
                  {selectedPlan.description
                    ? ` · ${selectedPlan.description}`
                    : ""}
                </p>
              </div>
            )}

            <div className="space-y-1.5">
              <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
                {t("common.starts")}
              </label>
              <div className="flex flex-wrap gap-1.5">
                {(
                  [
                    ["now", "Now"],
                    ["scheduled", "On a date"],
                  ] as [StartMode, string][]
                ).map(([mode, label]) => (
                  <button
                    key={mode}
                    type="button"
                    onClick={() => setStartMode(mode)}
                    className={cn(
                      "rounded-full border px-3 py-1 font-mono text-xs transition-colors",
                      startMode === mode
                        ? "border-brand-800 bg-brand-50 text-brand-900"
                        : "border-slate-200 bg-card text-slate-600 hover:bg-slate-50"
                    )}
                  >
                    {label}
                  </button>
                ))}
              </div>

              <p className="text-xs text-slate-500">
                {startMode === "now" ? STARTS_NOW_HINT : STARTS_LATER_HINT}
              </p>

              {startMode === "scheduled" && (
                <>
                  <DateTimePicker
                    value={startsAt}
                    onChange={setStartsAt}
                    placeholder={t("billing.selectStartDateAndTime")}
                    aria-label={t("billing.subscriptionStartDateAndTime")}
                  />
                  {startRefusal ? (
                    <p className="text-xs text-critical-500">{startRefusal}</p>
                  ) : (
                    startsAt.length > 0 && (
                      <p className="text-xs text-slate-400">
                        {t("billing.takesOver")} <LocalTime value={startsAtIso} />
                      </p>
                    )
                  )}
                </>
              )}
            </div>
          </div>

          <DialogFooter>
            <Button variant="outline" onClick={() => setSubscribeOpen(false)}>
              {t("common.cancel")}
            </Button>
            <Button onClick={handleSubscribe} disabled={subscribeBlocked}>
              {saving ? "Subscribing..." : "Subscribe"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <Dialog
        open={cancelTarget !== null}
        onOpenChange={(open) => !open && setCancelTarget(null)}
      >
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>{t("billing.cancelThisScheduledChange")}</DialogTitle>
            <DialogDescription>
              {cancelTarget &&
                `${versionLabel(
                  cancelTarget.planName,
                  cancelTarget.planVersion?.versionNumber ?? null
                )} will not take over.`}
            </DialogDescription>
          </DialogHeader>

          <p className="text-sm text-slate-500">
            {CANCEL_REOPENS_PREDECESSOR}
          </p>

          {cancelTarget && !canCancel(cancelTarget) && (
            <p className="flex items-start gap-2 rounded-lg border border-attention-200 bg-attention-50 p-3 text-xs text-attention-700">
              <TriangleAlert className="mt-0.5 size-3.5 flex-shrink-0" />
              <span>{CANNOT_CANCEL_STARTED}</span>
            </p>
          )}

          <DialogFooter>
            <Button variant="outline" onClick={() => setCancelTarget(null)}>
              {t("common.keepIt")}
            </Button>
            <Button
              variant="destructive"
              onClick={handleCancel}
              disabled={
                cancelling || (cancelTarget !== null && !canCancel(cancelTarget))
              }
            >
              {cancelling ? "Cancelling..." : "Cancel the change"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
