"use client";

import { useCallback, useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { toast } from "sonner";
import { Archive, History, Pause, Play, Save, X } from "lucide-react";

import {
  EscalationRule,
  EscalationRuleResource,
  RuleBodyDto,
  RuleSeverity,
} from "@/types";
import { API_BASE, reportApiError, unwrap } from "@/lib/api";
import {
  ACTIONS_NOTE,
  ACTION_HINTS,
  CONDITIONS_NOTE,
  CONDITION_HINTS,
  CONDITION_MINUTES_MAX,
  EscalationRuleDraft,
  NO_CONDITION_DESCRIPTION,
  NO_CONDITION_ERROR,
  PRIORITY_HINT,
  SWEEP_NOTE,
  draftFrom,
  draftProblem,
  escalationRulePatchBody,
  isDirty,
  isValidConditionMinutes,
  isValidPriority,
  wouldHaveAnyCondition,
} from "@/lib/escalationRules";
import { SEVERITY_FILTERS } from "@/lib/severity";
import { HistoryDialog, historyPath } from "@/components/history";
import { NotFound } from "@/components/NotFound";
import { PageHeader } from "@/components/PageHeader";
import { StatusBadge } from "@/components/StatusBadge";
import RuleBuilder from "@/components/RuleBuilder";
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
import { QueuePicker } from "./QueuePicker";

import { apiFetch } from "@/lib/http";

import { LocalTime } from "@/components/LocalTime";
import { useFormat } from "@/lib/useFormat";
import { useTranslation } from "@/context/LocalizationContext";

const SEVERITY_ANY = "Any";

export function EscalationRuleDetail({
  organizationId,
  escalationRuleId,
  backHref,
  onUnauthorized,
}: {
  organizationId: string;
  escalationRuleId: string;
  backHref: string;
  onUnauthorized: () => void;
}) {
  const t = useTranslation();
  const format = useFormat();
  const router = useRouter();

  const [rule, setRule] = useState<EscalationRule | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [historyOpen, setHistoryOpen] = useState(false);
  const [retireOpen, setRetireOpen] = useState(false);

  const [draft, setDraft] = useState<EscalationRuleDraft | null>(null);
  const [priorityText, setPriorityText] = useState("");
  const [unassignedText, setUnassignedText] = useState("");
  const [holdExpiryText, setHoldExpiryText] = useState("");
  // Bumped whenever a saved matcher is loaded, to remount the rule builder — it
  // seeds its tree from `initialRules` once and does not watch the prop.
  const [matcherEpoch, setMatcherEpoch] = useState(0);

  const fetchRule = useCallback(async () => {
    setLoading(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationId}/escalation-rules/${escalationRuleId}`,
        {
          method: "GET",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
        }
      );

      if (response.ok) {
        const { escalationRule } = await unwrap<EscalationRuleResource>(
          response
        );
        setRule(escalationRule);
        setDraft(draftFrom(escalationRule));
        setPriorityText(String(escalationRule.priority));
        setUnassignedText(
          escalationRule.unassignedForMinutes === null
            ? ""
            : String(escalationRule.unassignedForMinutes)
        );
        setHoldExpiryText(
          escalationRule.holdExpiryWithinMinutes === null
            ? ""
            : String(escalationRule.holdExpiryWithinMinutes)
        );
        setMatcherEpoch((epoch) => epoch + 1);
      } else {
        setRule(null);
        await reportApiError(response, {
          fallback: t("escalations.errorOccurredFetchingEscalationRule"),
          onUnauthorized,
        });
      }
    } catch {
      setRule(null);
      toast.error(t("escalations.errorOccurredFetchingEscalationRule"));
    } finally {
      setLoading(false);
    }
  }, [organizationId, escalationRuleId, onUnauthorized]);

  useEffect(() => {
    fetchRule();
  }, [fetchRule]);

  const dirty = Boolean(rule && draft && isDirty(draft, rule));

  const update = (patch: Partial<EscalationRuleDraft>) =>
    setDraft((current) => (current ? { ...current, ...patch } : current));

  const readMinutes = (text: string): number | null =>
    text.trim() === "" ? null : Number(text);

  const handleSave = async () => {
    if (!rule || !draft) return;

    const problem = draftProblem(draft);
    if (problem) {
      toast.error(problem.message, {
        description: problem.description,
      });
      return;
    }

    setSaving(true);

    try {
      const body = escalationRulePatchBody(draft, rule);

      if (Object.keys(body).length === 0) {
        toast.info(t("common.nothingSave"));
        return;
      }

      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationId}/escalation-rules/${escalationRuleId}`,
        {
          method: "PATCH",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
          body: JSON.stringify(body),
        }
      );

      if (!response.ok) {
        await reportApiError(response, {
          fallback: t("escalations.failedSaveEscalationRule"),
          onUnauthorized,
        });
        return;
      }

      toast.success(t("escalations.escalationRuleSaved"), { description: SWEEP_NOTE });
      await fetchRule();
    } catch {
      toast.error(t("escalations.failedSaveEscalationRule"));
    } finally {
      setSaving(false);
    }
  };

  const setEnabled = async (isEnabled: boolean) => {
    if (!rule) return;
    setSaving(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationId}/escalation-rules/${escalationRuleId}`,
        {
          method: "PATCH",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
          body: JSON.stringify({ isEnabled }),
        }
      );

      if (response.ok) {
        toast.success(
          isEnabled ? t("escalations.namedRunningAgain", rule.name) : t("common.namedPaused", rule.name),
          {
            description: isEnabled
              ? t("escalations.sweepTriesAgainUsualPlace")
              : t("escalations.sweepSkipsNextRuleDown"),
          }
        );
        await fetchRule();
      } else {
        await reportApiError(response, {
          fallback: t("escalations.failedUpdateEscalationRule"),
          onUnauthorized,
        });
      }
    } catch {
      toast.error(t("escalations.failedUpdateEscalationRule"));
    } finally {
      setSaving(false);
    }
  };

  const handleRetire = async () => {
    if (!rule) return;

    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationId}/escalation-rules/${escalationRuleId}`,
        { method: "DELETE", credentials: "include" }
      );

      if (response.ok) {
        toast.success(t("common.namedRetired", rule.name), {
          description:
            t("escalations.casesAlreadyEscalatedKeepTheir"),
        });
        setRetireOpen(false);
        router.push(backHref);
      } else {
        await reportApiError(response, {
          fallback: t("escalations.failedRetireEscalationRule"),
          onUnauthorized,
        });
      }
    } catch {
      toast.error(t("escalations.failedRetireEscalationRule"));
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

  if (!rule || !draft) {
    return (
      <NotFound
        title={t("escalations.escalationRuleNotFound")}
        description={t("escalations.itMayHaveBeenRetired")}
        backHref={backHref}
        backLabel={t("escalations.allEscalationRules")}
      />
    );
  }

  const priorityValid = isValidPriority(draft.priority);
  const unassignedValid =
    draft.unassignedForMinutes === null ||
    isValidConditionMinutes(draft.unassignedForMinutes);
  const holdExpiryValid =
    draft.holdExpiryWithinMinutes === null ||
    isValidConditionMinutes(draft.holdExpiryWithinMinutes);
  const hasCondition = wouldHaveAnyCondition(draft);

  return (
    <div className="space-y-6">
      <PageHeader eyebrow={t("escalations.escalationRule")} title={rule.name}>
        <Button
          variant="outline"
          size="sm"
          className="gap-1.5"
          onClick={() => setHistoryOpen(true)}
        >
          <History className="size-4" />
          {t("common.history")}
        </Button>
        {rule.isEnabled ? (
          <Button
            variant="outline"
            size="sm"
            className="gap-1.5 text-slate-600"
            disabled={saving}
            onClick={() => setEnabled(false)}
          >
            <Pause className="size-4" />
            {t("escalations.pause")}
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
            {t("escalations.resume")}
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

      <Card>
        <CardContent className="space-y-5 pt-6">
          <div className="flex flex-wrap items-center gap-3">
            <StatusBadge
              status={rule.isEnabled ? "Enabled" : "Disabled"}
              label={rule.isEnabled ? "Running" : "Paused"}
            />
            <span className="font-mono text-[11px] uppercase tracking-widest text-slate-400">
              runs at priority {rule.priority}
            </span>
          </div>

          {!rule.isEnabled && (
            <p className="rounded-md border border-slate-200 bg-slate-50 px-3 py-2 text-sm text-slate-600">
              Paused. The sweep skips this rule entirely and the next one down in
              priority order gets the chance instead. Cases it escalated
              already keep their stamp.
            </p>
          )}

          <p className="rounded-md border border-slate-200 bg-slate-50 px-3 py-2 text-sm text-slate-600">
            {SWEEP_NOTE}
          </p>

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
                {t("escalations.priority")}
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
              <p className="text-xs text-slate-500">{PRIORITY_HINT}</p>
              {!priorityValid && (
                <p className="text-xs text-critical-500">
                  {t("escalations.mustBeAWholeNumber")}
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
        </CardContent>
      </Card>

      <Card>
        <CardContent className="space-y-5 pt-6">
          <div>
            <h2 className="font-mono text-sm font-medium text-slate-900">
              {t("escalations.whenItFires")}
            </h2>
            <p className="mt-1 max-w-prose text-sm text-slate-500">
              {CONDITIONS_NOTE}
            </p>
            {!hasCondition && (
              <p className="mt-2 rounded-md border border-critical-200 bg-critical-50 px-3 py-2 text-sm text-critical-700">
                {NO_CONDITION_ERROR}. {NO_CONDITION_DESCRIPTION}
              </p>
            )}
          </div>

          <div className="grid gap-5 sm:grid-cols-2">
            <div className="space-y-1.5">
              <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
                {t("escalations.unassignedForMinutes")}
              </label>
              <Input
                value={unassignedText}
                onChange={(event) => {
                  const text = event.target.value;
                  setUnassignedText(text);
                  update({ unassignedForMinutes: readMinutes(text) });
                }}
                inputMode="numeric"
                placeholder={t("escalations.notConstrained")}
                className="font-mono"
              />
              <p className="text-xs text-slate-500">
                {CONDITION_HINTS.unassignedForMinutes}
              </p>
              {!unassignedValid && (
                <p className="text-xs text-critical-500">
                  Must be between 1 and{" "}
                  {format.count(CONDITION_MINUTES_MAX)} minutes.
                </p>
              )}
            </div>

            <div className="space-y-1.5">
              <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
                {t("escalations.holdExpiresWithinMinutes")}
              </label>
              <Input
                value={holdExpiryText}
                onChange={(event) => {
                  const text = event.target.value;
                  setHoldExpiryText(text);
                  update({ holdExpiryWithinMinutes: readMinutes(text) });
                }}
                inputMode="numeric"
                placeholder={t("escalations.notConstrained")}
                className="font-mono"
              />
              <p className="text-xs text-slate-500">
                {CONDITION_HINTS.holdExpiryWithinMinutes}
              </p>
              {!holdExpiryValid && (
                <p className="text-xs text-critical-500">
                  Must be between 1 and{" "}
                  {format.count(CONDITION_MINUTES_MAX)} minutes.
                </p>
              )}
            </div>
          </div>

          <div className="space-y-1.5">
            <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
              {t("escalations.severityFloor")}
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
              {CONDITION_HINTS.minimumSeverity}
            </p>
          </div>

          <div className="space-y-2">
            <div className="flex items-center justify-between gap-3">
              <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
                {t("escalations.matcher")}
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
                  {t("escalations.removeMatcher")}
                </Button>
              )}
            </div>
            <p className="max-w-prose text-xs text-slate-500">
              {CONDITION_HINTS.matcher}
            </p>

            {draft.matcher ? (
              <div className="space-y-2 rounded-lg border border-slate-200 p-3">
                <p className="rounded-md border border-attention-200 bg-attention-50 px-3 py-2 text-xs text-attention-700">
                  Editing here is separate from the rule. Press{" "}
                  <span className="font-medium">{t("escalations.useThisMatcher")}</span> to bring
                  changes back to the form, then Save the rule to keep them.
                </p>
                <RuleBuilder
                  key={matcherEpoch}
                  ruleSetId=""
                  initialRules={[draft.matcher]}
                  submitRuleText={t("escalations.useThisMatcher")}
                  saveButtonText={t("organization.useThisMatcher")}
                  readOnly={false}
                  allowVelocity={false}
                  handleSaveBody={(rules) => {
                    update({ matcher: rules[0] ?? null });
                    toast.success(t("common.matcherUpdated"), {
                      description: t("escalations.saveRuleKeep"),
                    });
                  }}
                />
              </div>
            ) : (
              <div className="rounded-lg border border-dashed border-slate-200 px-4 py-6 text-center">
                <p className="mx-auto max-w-md text-sm text-slate-500">
                  No matcher. Add one to escalate on the transaction itself — an
                  amount band, a debtor country, a party&apos;s risk score as it
                  stands now rather than as it stood when the case opened.
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
                  {t("escalations.addAMatcher")}
                </Button>
              </div>
            )}
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardContent className="space-y-5 pt-6">
          <div>
            <h2 className="font-mono text-sm font-medium text-slate-900">
              {t("escalations.whatItDoes")}
            </h2>
            <p className="mt-1 max-w-prose text-sm text-slate-500">
              {ACTIONS_NOTE}
            </p>
          </div>

          <div className="space-y-1.5">
            <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
              {t("escalations.moveToQueue")}
            </label>
            <QueuePicker
              organizationId={organizationId}
              value={draft.targetQueueId}
              onChange={(targetQueueId) => update({ targetQueueId })}
              disabled={saving}
            />
            <p className="text-xs text-slate-500">
              {ACTION_HINTS.targetQueueId}
            </p>
          </div>

          <div className="space-y-1.5">
            <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
              {t("escalations.raiseSeverityTo")}
            </label>
            <Select
              value={draft.raiseSeverityTo ?? SEVERITY_ANY}
              onValueChange={(next) =>
                update({
                  raiseSeverityTo:
                    next === SEVERITY_ANY ? null : (next as RuleSeverity),
                })
              }
              disabled={saving}
            >
              <SelectTrigger className="max-w-xs">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value={SEVERITY_ANY}>{t("escalations.leaveItAlone")}</SelectItem>
                {SEVERITY_FILTERS.map((severity) => (
                  <SelectItem key={severity} value={severity}>
                    {severity}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
            <p className="max-w-prose text-xs text-slate-500">
              {ACTION_HINTS.raiseSeverityTo}
            </p>
          </div>
        </CardContent>
      </Card>

      <div className="flex items-center gap-3">
        <Button
          className="gap-1.5"
          onClick={handleSave}
          disabled={
            saving ||
            !dirty ||
            !priorityValid ||
            !unassignedValid ||
            !holdExpiryValid ||
            !hasCondition ||
            draft.name.trim().length === 0
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
          {t("escalations.back")}
        </Button>
      </div>

      <Card>
        <CardContent className="grid grid-cols-2 gap-x-8 gap-y-3 pt-6 text-sm">
          <div>
            <span className="font-medium text-slate-500">{t("common.created")}</span>
            <p className="mt-0.5 text-slate-900">
              <LocalTime value={rule.createdAt} />
            </p>
          </div>
          <div>
            <span className="font-medium text-slate-500">{t("common.updated")}</span>
            <p className="mt-0.5 text-slate-900">
              <LocalTime value={rule.updatedAt} />
            </p>
          </div>
        </CardContent>
      </Card>

      <Dialog open={retireOpen} onOpenChange={setRetireOpen}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>{t("escalations.retireTitle", rule.name)}</DialogTitle>
            <DialogDescription>
              {t("escalations.itEscalatesNothingFromNow")}
            </DialogDescription>
          </DialogHeader>

          <p className="text-sm text-slate-600">
            Retiring takes the rule out of the sweep for good, and out of this
            console: cases it already escalated keep their stamp, but the rule
            can no longer be opened and their &quot;escalated by&quot; becomes an
            id nobody can resolve. Its history survives. If you only want it to
            stop firing for now, pause it instead.
          </p>

          <DialogFooter>
            <Button variant="outline" onClick={() => setRetireOpen(false)}>
              {t("common.cancel")}
            </Button>
            {rule.isEnabled && (
              <Button
                variant="secondary"
                onClick={() => {
                  setRetireOpen(false);
                  setEnabled(false);
                }}
              >
                {t("escalations.pauseInstead")}
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
        title={t("escalations.thisEscalationRule")}
        entityType="EscalationRule"
        path={historyPath.EscalationRule(organizationId, escalationRuleId)}
        entityId={escalationRuleId}
        lifecycleNotes={{
          softDeleted:
            "Retired — the rule left the sweep and stopped being readable. Cases it escalated keep their stamp, and this trail is what explains it.",
          recovered:
            "Recovered — the rule is back in the sweep under its original name and priority.",
        }}
        onUnauthorized={onUnauthorized}
      />
    </div>
  );
}
