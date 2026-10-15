"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { toast } from "sonner";
import {
  Archive,
  History,
  Pause,
  Play,
  Save,
  TriangleAlert,
  X,
} from "lucide-react";

import {
  Queue,
  QueueResource,
  QueueStatistic,
  QueueUsage,
  ROUTING_POLICIES,
  RoutingPolicy,
  RuleBodyDto,
  RuleSeverity,
} from "@/types";
import { API_BASE, reportApiError, unwrap } from "@/lib/api";
import {
  QueueDraft,
  ROUTING_POLICY_HINTS,
  assignsOnArrival,
  draftFrom,
  hasVelocityCondition,
  isCatchAll,
  isDirty,
  isValidPriority,
  queuePatchBody,
  routingPolicyLabel,
} from "@/lib/queues";
import { SEVERITY_FILTERS } from "@/lib/severity";
import { HistoryDialog, historyPath } from "@/components/history";
import { NotFound } from "@/components/NotFound";
import { PageHeader } from "@/components/PageHeader";
import { StatusBadge } from "@/components/StatusBadge";
import RuleBuilder from "@/components/RuleBuilder";
import { SubOrganizationOwnerField } from "@/components/subOrganizations/SubOrganizationOwnerField";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Card, CardContent } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { QueueMembersField } from "./QueueMembersField";
import { QueueStatisticCard } from "./QueueStatisticCard";
import { RuleMultiSelect } from "./RuleMultiSelect";

import { apiFetch } from "@/lib/http";

import { LocalTime } from "@/components/LocalTime";
import { useTranslation } from "@/context/LocalizationContext";

const SEVERITY_ANY = "Any";

export function QueueDetail({
  organizationId,
  queueId,
  backHref,
  onUnauthorized,
}: {
  organizationId: string;
  queueId: string;
  backHref: string;
  onUnauthorized: () => void;
}) {
  const t = useTranslation();
  const router = useRouter();

  const [queue, setQueue] = useState<Queue | null>(null);
  const [statistic, setStatistic] = useState<QueueStatistic | null>(null);
  const [usage, setUsage] = useState<QueueUsage | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [historyOpen, setHistoryOpen] = useState(false);
  const [retireOpen, setRetireOpen] = useState(false);

  const [draft, setDraft] = useState<QueueDraft | null>(null);
  const [priorityText, setPriorityText] = useState("");
  const [memberIds, setMemberIds] = useState<string[]>([]);
  // Bumped whenever a saved matcher is loaded, to remount the rule builder —
  // it seeds its tree from `initialRules` once and does not watch the prop.
  const [matcherEpoch, setMatcherEpoch] = useState(0);

  const fetchQueue = useCallback(async () => {
    setLoading(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationId}/queues/${queueId}`,
        {
          method: "GET",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
        }
      );

      if (response.ok) {
        const { queue: loaded } = await unwrap<QueueResource>(response);
        setQueue(loaded);
        setDraft(draftFrom(loaded));
        setPriorityText(String(loaded.priority));
        setMemberIds(loaded.members.map((member) => member.userId));
        setMatcherEpoch((epoch) => epoch + 1);
      } else {
        setQueue(null);
        await reportApiError(response, {
          fallback: t("queues.errorOccurredFetchingQueue"),
          onUnauthorized,
        });
      }
    } catch {
      setQueue(null);
      toast.error(t("queues.errorOccurredFetchingQueue"));
    } finally {
      setLoading(false);
    }
  }, [organizationId, queueId, onUnauthorized]);

  const fetchStatistic = useCallback(async () => {
    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationId}/queues/${queueId}/statistics`,
        {
          method: "GET",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
        }
      );
      if (response.ok) setStatistic(await unwrap<QueueStatistic>(response));
    } catch {
    }
  }, [organizationId, queueId]);

  const fetchUsage = useCallback(async () => {
    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationId}/queues/${queueId}/usages`,
        {
          method: "GET",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
        }
      );
      if (response.ok) setUsage(await unwrap<QueueUsage>(response));
    } catch {
    }
  }, [organizationId, queueId]);

  useEffect(() => {
    fetchQueue();
    fetchStatistic();
    fetchUsage();
  }, [fetchQueue, fetchStatistic, fetchUsage]);

  const membersChanged = useMemo(() => {
    if (!queue) return false;
    const saved = new Set(queue.members.map((member) => member.userId));
    return (
      saved.size !== memberIds.length ||
      memberIds.some((userId) => !saved.has(userId))
    );
  }, [queue, memberIds]);

  const dirty = Boolean(
    queue && draft && (isDirty(draft, queue) || membersChanged)
  );

  const update = (patch: Partial<QueueDraft>) =>
    setDraft((current) => (current ? { ...current, ...patch } : current));

  const handleSave = async () => {
    if (!queue || !draft) return;

    if (hasVelocityCondition(draft.matcher)) {
      toast.error(t("queues.queueMatcherCannotAskAbout"), {
        description:
          t("queues.routingRunsAfterEngineHas"),
      });
      return;
    }

    setSaving(true);

    try {
      const body = queuePatchBody(draft, queue);

      if (Object.keys(body).length > 0) {
        const response = await apiFetch(
          `${API_BASE}/organizations/${organizationId}/queues/${queueId}`,
          {
            method: "PATCH",
            headers: { "Content-Type": "application/json" },
            credentials: "include",
            body: JSON.stringify(body),
          }
        );

        if (!response.ok) {
          await reportApiError(response, {
            fallback: t("queues.failedSaveQueue"),
            onUnauthorized,
          });
          return;
        }
      }

      if (membersChanged) {
        const response = await apiFetch(
          `${API_BASE}/organizations/${organizationId}/queues/${queueId}/members`,
          {
            method: "PUT",
            headers: { "Content-Type": "application/json" },
            credentials: "include",
            body: JSON.stringify({ userIds: memberIds }),
          }
        );

        if (!response.ok) {
          await reportApiError(response, {
            fallback: t("queues.queueSavedButMembershipNot"),
            onUnauthorized,
          });
          await fetchQueue();
          return;
        }
      }

      toast.success(t("queues.queueSaved"));
      await Promise.all([fetchQueue(), fetchStatistic()]);
    } catch {
      toast.error(t("queues.failedSaveQueue"));
    } finally {
      setSaving(false);
    }
  };

  const setEnabled = async (isEnabled: boolean) => {
    if (!queue) return;
    setSaving(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationId}/queues/${queueId}`,
        {
          method: "PATCH",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
          body: JSON.stringify({ isEnabled }),
        }
      );

      if (response.ok) {
        toast.success(
          isEnabled
            ? t("queues.namedTakingWorkAgain", queue.name)
            : t("common.namedPaused", queue.name),
          {
            description: isEnabled
              ? t("queues.routingTriesAgainUsualPlace")
              : t("queues.routingSkipsNextQueueDown"),
          }
        );
        await fetchQueue();
      } else {
        await reportApiError(response, {
          fallback: t("queues.failedUpdateQueue"),
          onUnauthorized,
        });
      }
    } catch {
      toast.error(t("queues.failedUpdateQueue"));
    } finally {
      setSaving(false);
    }
  };

  const handleRetire = async () => {
    if (!queue) return;

    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationId}/queues/${queueId}`,
        { method: "DELETE", credentials: "include" }
      );

      if (response.ok) {
        toast.success(t("common.namedRetired", queue.name), {
          description:
            t("queues.casesAlreadyKeepNameStay"),
        });
        setRetireOpen(false);
        router.push(backHref);
      } else {
        await reportApiError(response, {
          fallback: t("queues.failedRetireQueue"),
          onUnauthorized,
        });
      }
    } catch {
      toast.error(t("queues.failedRetireQueue"));
    }
  };

  if (loading) {
    return (
      <div className="space-y-6">
        <Skeleton className="h-8 w-56" />
        <Skeleton className="h-96 w-full max-w-3xl" />
      </div>
    );
  }

  if (!queue || !draft) {
    return (
      <NotFound
        title={t("queues.queueNotFound")}
        description={t("queues.itMayHaveBeenRetired")}
        backHref={backHref}
        backLabel={t("queues.allQueues")}
      />
    );
  }

  const priorityValid = isValidPriority(draft.priority);

  const idle =
    draft.isEnabled && assignsOnArrival(draft.routingPolicy) && memberIds.length === 0;

  return (
    <div className="space-y-6">
      <PageHeader eyebrow={t("queues.queue2")} title={queue.name}>
        <Button
          variant="outline"
          size="sm"
          className="gap-1.5"
          onClick={() => setHistoryOpen(true)}
        >
          <History className="size-4" />
          {t("common.history")}
        </Button>
        {queue.isEnabled ? (
          <Button
            variant="outline"
            size="sm"
            className="gap-1.5 text-slate-600"
            disabled={saving}
            onClick={() => setEnabled(false)}
          >
            <Pause className="size-4" />
            {t("queues.pause")}
          </Button>
        ) : (
          <Button
            variant="outline"
            size="sm"
            className="gap-1.5 text-settled-700 hover:text-settled-700"
            disabled={saving}
            onClick={() => setEnabled(true)}
          >
            <Play className="size-4" />
            {t("queues.resume")}
          </Button>
        )}
        <Button
          variant="outline"
          size="sm"
          className="gap-1.5 text-critical-500 hover:text-critical-700"
          disabled={saving}
          onClick={() => setRetireOpen(true)}
        >
          <Archive className="size-4" />
          {t("common.retire")}
        </Button>
      </PageHeader>

      {statistic && <QueueStatisticCard statistic={statistic} />}

      <Card>
        <CardContent className="space-y-5 pt-6">
          <div className="flex flex-wrap items-center gap-3">
            <StatusBadge
              status={queue.isEnabled ? "Enabled" : "Disabled"}
              label={queue.isEnabled ? t("queues.takingWork") : "Paused"}
            />
            <span className="font-mono text-[11px] uppercase tracking-widest text-slate-400">
              tried at priority {queue.priority}
            </span>
          </div>

          {!queue.isEnabled && (
            <p className="rounded-md border border-slate-200 bg-slate-50 px-3 py-2 text-sm text-slate-600">
              Paused. Routing skips this queue entirely and the next one down in
              priority order gets the chance instead. Cases already here are
              untouched, and analysts can still claim from it.
            </p>
          )}

          {idle && (
            <div className="flex items-start gap-2.5 rounded-md border border-attention-200 bg-attention-50 px-3 py-2.5">
              <TriangleAlert className="mt-0.5 size-4 flex-shrink-0 text-attention-700" />
              <p className="text-sm text-attention-700">
                This queue hands work out but has nobody to hand it to. Cases
                will still arrive and will sit unassigned until somebody is
                added below — nothing is lost, but nobody is told either.
              </p>
            </div>
          )}

          <div className="grid gap-5 sm:grid-cols-2">
            <div className="space-y-1.5">
              <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
                {t("common.name")}
              </label>
              <Input
                value={draft.name}
                onChange={(event) => update({ name: event.target.value })}
                maxLength={64}
              />
            </div>

            <div className="space-y-1.5">
              <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
                {t("queues.priority")}
              </label>
              <Input
                value={priorityText}
                onChange={(event) => {
                  const text = event.target.value;
                  setPriorityText(text);
                  // An empty box is NaN, not 0 — see `priorityText` above.
                  update({ priority: text.trim() === "" ? NaN : Number(text) });
                }}
                inputMode="numeric"
                className="font-mono"
              />
              <p className="text-xs text-slate-500">
                0–1000, lower tried first. The first queue that matches takes the
                case, so this is the order and not an importance.
              </p>
              {!priorityValid && (
                <p className="text-xs text-critical-500">
                  {t("queues.mustBeAWholeNumber")}
                </p>
              )}
            </div>
          </div>

          <div className="space-y-1.5">
            <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
              {t("common.description")}
            </label>
            <Input
              value={draft.description}
              onChange={(event) => update({ description: event.target.value })}
              maxLength={512}
            />
          </div>

          <div className="space-y-1.5">
            <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
              {t("queues.handsOut")}
            </label>
            <Select
              value={draft.routingPolicy}
              onValueChange={(next) =>
                update({ routingPolicy: next as RoutingPolicy })
              }
            >
              <SelectTrigger className="max-w-xs">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {ROUTING_POLICIES.map((option) => (
                  <SelectItem key={option} value={option}>
                    {routingPolicyLabel(option)}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
            <p className="text-xs text-slate-500">
              {ROUTING_POLICY_HINTS[draft.routingPolicy]}
            </p>
          </div>

          <SubOrganizationOwnerField
            organizationId={organizationId}
            value={draft.subOrganizationOwner}
            onChange={(subOrganizationOwner) => update({ subOrganizationOwner })}
            disabled={saving}
            label={t("queues.ownedBy")}
          />
        </CardContent>
      </Card>

      <Card>
        <CardContent className="space-y-5 pt-6">
          <div>
            <h2 className="font-mono text-sm font-medium text-slate-900">
              {t("queues.whatLandsHere")}
            </h2>
            <p className="mt-1 text-sm text-slate-500">
              All three are optional, and a queue that declares several matches
              only when <span className="font-medium">every</span> one of them is
              satisfied. Declare none and the queue takes everything that reaches
              it.
            </p>
            {isCatchAll(draft) && (
              <p className="mt-2 rounded-md border border-attention-200 bg-attention-50 px-3 py-2 text-sm text-attention-700">
                Nothing is declared, so this queue takes every case that gets as
                far as it — and no queue below it in the order will ever be
                tried.
              </p>
            )}
          </div>

          <div className="space-y-1.5">
            <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
              {t("queues.severityFloor")}
            </label>
            <Select
              value={draft.minimumSeverity ?? SEVERITY_ANY}
              onValueChange={(next) =>
                update({
                  minimumSeverity:
                    next === SEVERITY_ANY ? null : (next as RuleSeverity),
                })
              }
            >
              <SelectTrigger className="max-w-xs">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value={SEVERITY_ANY}>{t("filter.anySeverity")}</SelectItem>
                {SEVERITY_FILTERS.map((severity) => (
                  <SelectItem key={severity} value={severity}>
                    {severity} and above
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
            <p className="text-xs text-slate-500">
              Matches the case&apos;s severity and everything more severe. A case
              takes the severity of the worst rule that fired when it opened.
            </p>
          </div>

          <div className="space-y-1.5">
            <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
              {t("queues.rules")}
            </label>
            <RuleMultiSelect
              value={draft.ruleIds}
              onChange={(ruleIds) => update({ ruleIds })}
              disabled={saving}
            />
          </div>

          <div className="space-y-2">
            <div className="flex items-center justify-between gap-3">
              <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
                {t("queues.matcher")}
              </label>
              {draft.matcher && (
                <Button
                  variant="ghost"
                  size="sm"
                  className="gap-1.5 text-slate-500 hover:text-slate-900"
                  onClick={() => {
                    update({ matcher: null });
                    setMatcherEpoch((epoch) => epoch + 1);
                  }}
                >
                  <X className="size-4" />
                  {t("queues.removeMatcher")}
                </Button>
              )}
            </div>

            {draft.matcher ? (
              <div className="space-y-2 rounded-lg border border-slate-200 p-3">
                <p className="rounded-md border border-attention-200 bg-attention-50 px-3 py-2 text-xs text-attention-700">
                  Editing here is separate from the queue. Press{" "}
                  <span className="font-medium">{t("queues.useThisMatcher")}</span> to bring
                  changes back to the form, then Save the queue to keep them.
                </p>
                <RuleBuilder
                  key={matcherEpoch}
                  ruleSetId=""
                  initialRules={[draft.matcher]}
                  submitRuleText={t("queues.useThisMatcher")}
                  saveButtonText={t("organization.useThisMatcher")}
                  readOnly={false}
                  allowVelocity={false}
                  handleSaveBody={(rules) => {
                    update({ matcher: rules[0] ?? null });
                    toast.success(t("common.matcherUpdated"), {
                      description: t("queues.saveQueueKeep"),
                    });
                  }}
                />
              </div>
            ) : (
              <div className="rounded-lg border border-dashed border-slate-200 px-4 py-6 text-center">
                <p className="text-sm text-slate-500">
                  No matcher. Add one to route on the payload itself — an amount
                  band, a debtor country, a party&apos;s risk score.
                </p>
                <Button
                  variant="outline"
                  size="sm"
                  className="mt-3"
                  onClick={() => {
                    update({
                      matcher: {
                        condition: null,
                        field: null,
                        value: null,
                      } as RuleBodyDto,
                    });
                    setMatcherEpoch((epoch) => epoch + 1);
                  }}
                >
                  {t("queues.addAMatcher")}
                </Button>
              </div>
            )}
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardContent className="space-y-4 pt-6">
          <div>
            <h2 className="font-mono text-sm font-medium text-slate-900">
              {t("queues.whoWorksIt")}
            </h2>
            <p className="mt-1 text-sm text-slate-500">
              Membership drives the rotation, the load-aware pick, and who may
              claim from this queue. Anyone not on this list gets a 403 from its
              claim-next.
            </p>
          </div>

          <QueueMembersField
            value={memberIds}
            onChange={setMemberIds}
            disabled={saving}
          />
        </CardContent>
      </Card>

      <div className="flex items-center gap-3">
        <Button
          className="gap-1.5"
          onClick={handleSave}
          disabled={
            saving || !dirty || !priorityValid || draft.name.trim().length === 0
          }
        >
          <Save className="size-4" />
          {saving ? "Saving..." : "Save"}
        </Button>
        <Button
          variant="outline"
          onClick={() => router.push(backHref)}
          disabled={saving}
        >
          {t("queues.back")}
        </Button>
        {membersChanged && (
          <span className="font-mono text-[11px] uppercase tracking-widest text-slate-400">
            membership changed
          </span>
        )}
      </div>

      <Card>
        <CardContent className="grid grid-cols-2 gap-x-8 gap-y-3 pt-6 text-sm">
          <div>
            <span className="font-medium text-slate-500">{t("common.created")}</span>
            <p className="mt-0.5 text-slate-900">
              <LocalTime value={queue.createdAt} />
            </p>
          </div>
          <div>
            <span className="font-medium text-slate-500">{t("common.updated")}</span>
            <p className="mt-0.5 text-slate-900">
              <LocalTime value={queue.updatedAt} />
            </p>
          </div>
          {usage && (
            <div className="col-span-2">
              <span className="font-medium text-slate-500">{t("queues.casesRoutedHere")}</span>
              <p className="mt-0.5 text-slate-900">
                {usage.caseCount} in total, {usage.openCaseCount} still open
              </p>
            </div>
          )}
        </CardContent>
      </Card>

      <Dialog open={retireOpen} onOpenChange={setRetireOpen}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>{t("queues.retireTitle", queue.name)}</DialogTitle>
            <DialogDescription>
              {usage
                ? usage.openCaseCount > 0
                  ? `${usage.openCaseCount} open case${
                      usage.openCaseCount === 1 ? "" : "s"
                    } on this queue will stop being claimable.`
                  : "Nothing open is waiting on it."
                : "How much open work is on this queue could not be read."}
            </DialogDescription>
          </DialogHeader>

          <p className="text-sm text-slate-600">
            Retiring takes the queue out of the routing order for good. Cases
            already routed to it keep its name and stay readable and assignable
            by hand. If you only want it to stop taking work for now, pause it
            instead.
          </p>

          <DialogFooter>
            <Button variant="outline" onClick={() => setRetireOpen(false)}>
              {t("common.cancel")}
            </Button>
            {queue.isEnabled && (
              <Button
                variant="secondary"
                onClick={() => {
                  setRetireOpen(false);
                  setEnabled(false);
                }}
              >
                {t("queues.pauseInstead")}
              </Button>
            )}
            <Button variant="destructive" onClick={handleRetire}>
              {t("common.retire")}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <HistoryDialog
        open={historyOpen}
        onOpenChange={setHistoryOpen}
        title={t("queues.thisQueue")}
        entityType="Queue"
        path={historyPath.Queue(organizationId, queueId)}
        entityId={queueId}
        lifecycleNotes={{
          softDeleted:
            "Retired — the queue left the routing order and stopped being claimable. Cases already on it keep its name and stay readable.",
          recovered:
            "Recovered — the queue is back in the routing order under its original name.",
        }}
        onUnauthorized={onUnauthorized}
      />
    </div>
  );
}
