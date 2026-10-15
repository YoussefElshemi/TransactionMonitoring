"use client";

import { useCallback, useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { toast } from "sonner";
import {
  Archive,
  ArrowDown,
  ArrowUp,
  Eye,
  Plus,
  RotateCcw,
} from "lucide-react";

import { EscalationRule, EscalationRuleResource } from "@/types";
import { API_BASE, reportApiError, unwrap, unwrapPage } from "@/lib/api";
import {
  ACTIONS_NOTE,
  CONDITIONS_NOTE,
  PRIORITY_DEFAULT,
  PRIORITY_HINT,
  PRIORITY_MAX,
  PRIORITY_MIN,
  SWEEP_NOTE,
  actionChips,
  conditionChips,
  isValidConditionMinutes,
  isValidPriority,
} from "@/lib/escalationRules";
import { SEVERITY_FILTERS } from "@/lib/severity";
import Pagination from "@/components/Pagination";
import { PageHeader } from "@/components/PageHeader";
import { StatusBadge } from "@/components/StatusBadge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import { PageSizeSelect } from "@/components/PageSizeSelect";
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
import { useQueueNames } from "./QueuePicker";

import { apiFetch } from "@/lib/http";
import { useTranslation } from "@/context/LocalizationContext";

const ANY_STATE = "All";

const SEVERITY_ANY = "Any";

export function EscalationRulesPanel({
  organizationId,
  detailHref,
  embedded = false,
  onUnauthorized,
}: {
  organizationId: string;
  detailHref: (escalationRuleId: string) => string;
  embedded?: boolean;
  onUnauthorized: () => void;
}) {
  const t = useTranslation();
  const router = useRouter();
  const queueName = useQueueNames(organizationId);

  const [rules, setRules] = useState<EscalationRule[]>([]);
  const [pageNumber, setPageNumber] = useState(1);
  const [pageSize, setPageSize] = useState(25);
  const [searchTerm, setSearchTerm] = useState("");
  const [state, setState] = useState(ANY_STATE);
  const [totalPages, setTotalPages] = useState(1);
  const [loading, setLoading] = useState(true);
  const [busyRuleId, setBusyRuleId] = useState<string | null>(null);

  const [createOpen, setCreateOpen] = useState(false);
  const [saving, setSaving] = useState(false);
  const [newName, setNewName] = useState("");
  const [newDescription, setNewDescription] = useState("");
  const [newPriority, setNewPriority] = useState(String(PRIORITY_DEFAULT));
  const [newUnassignedFor, setNewUnassignedFor] = useState("60");
  const [newMinimumSeverity, setNewMinimumSeverity] = useState<string>(
    SEVERITY_ANY
  );

  const [retireTarget, setRetireTarget] = useState<EscalationRule | null>(null);

  const fetchRules = useCallback(async () => {
    setLoading(true);

    try {
      const query = new URLSearchParams({
        PageNumber: String(pageNumber),
        PageSize: String(pageSize),
      });
      if (searchTerm) query.set("Name", searchTerm);
      if (state !== ANY_STATE) query.set("IsEnabled", String(state === "Enabled"));

      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationId}/escalation-rules?${query}`,
        {
          method: "GET",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
        }
      );

      if (response.ok) {
        const page = await unwrapPage<EscalationRuleResource>(response);
        setRules(page.data.map(({ escalationRule }) => escalationRule));
        setTotalPages(page.metadata.pageCount);
      } else {
        await reportApiError(response, {
          fallback: t("escalations.errorOccurredFetchingEscalationRules"),
          onUnauthorized,
          toastId: "error-fetching-escalation-rules",
        });
      }
    } catch {
      toast.error(t("escalations.errorOccurredFetchingEscalationRules"), {
        id: "error-fetching-escalation-rules",
      });
    } finally {
      setLoading(false);
    }
  }, [organizationId, pageNumber, pageSize, searchTerm, state, onUnauthorized]);

  useEffect(() => {
    fetchRules();
  }, [fetchRules]);

  const patch = async (
    rule: EscalationRule,
    body: Record<string, unknown>,
    success: string,
    description?: string
  ) => {
    setBusyRuleId(rule.escalationRuleId);
    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationId}/escalation-rules/${rule.escalationRuleId}`,
        {
          method: "PATCH",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
          body: JSON.stringify(body),
        }
      );

      if (response.ok) {
        toast.success(success, description ? { description } : undefined);
        await fetchRules();
        return true;
      }

      await reportApiError(response, {
        fallback: t("escalations.failedUpdateEscalationRule"),
        onUnauthorized,
      });
      return false;
    } catch {
      toast.error(t("escalations.failedUpdateEscalationRule"));
      return false;
    } finally {
      setBusyRuleId(null);
    }
  };

  const move = async (index: number, direction: -1 | 1) => {
    const rule = rules[index];
    const neighbour = rules[index + direction];
    if (!rule || !neighbour) return;

    if (rule.priority === neighbour.priority) {
      const nudged = rule.priority + direction;
      if (nudged < PRIORITY_MIN || nudged > PRIORITY_MAX) {
        toast.error(t("escalations.noRoomMoveRule"), {
          description:
            t("common.sharesPriorityNeighbourAlreadyEnd"),
        });
        return;
      }
      await patch(rule, { priority: nudged }, `${rule.name} moved`);
      return;
    }

    setBusyRuleId(rule.escalationRuleId);
    try {
      const writes = [
        { target: rule, priority: neighbour.priority },
        { target: neighbour, priority: rule.priority },
      ];

      for (const write of writes) {
        const response = await apiFetch(
          `${API_BASE}/organizations/${organizationId}/escalation-rules/${write.target.escalationRuleId}`,
          {
            method: "PATCH",
            headers: { "Content-Type": "application/json" },
            credentials: "include",
            body: JSON.stringify({ priority: write.priority }),
          }
        );

        if (!response.ok) {
          await reportApiError(response, {
            fallback: t("escalations.failedReorderEscalationRules"),
            onUnauthorized,
          });
          // Refetch even on a partial failure: the first write may already have
          // landed, and showing the old order would hide a half-applied swap.
          await fetchRules();
          return;
        }
      }

      toast.success(
        t(
          direction === -1 ? "common.namedMovedUp" : "common.namedMovedDown",
          rule.name
        ),
        {
          description: t(
            direction === -1
              ? "escalations.nowTriedBefore"
              : "escalations.nowTriedAfter",
            neighbour.name
          ),
        }
      );
      await fetchRules();
    } catch {
      toast.error(t("escalations.failedReorderEscalationRules"));
      await fetchRules();
    } finally {
      setBusyRuleId(null);
    }
  };

  const handleCreate = async () => {
    setSaving(true);

    try {
      const unassignedFor =
        newUnassignedFor.trim() === "" ? null : Number(newUnassignedFor);

      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationId}/escalation-rules`,
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
          body: JSON.stringify({
            name: newName.trim(),
            description: newDescription.trim() || null,
            priority: Number(newPriority),
            isEnabled: true,
            unassignedForMinutes: unassignedFor,
            minimumSeverity:
              newMinimumSeverity === SEVERITY_ANY ? null : newMinimumSeverity,
          }),
        }
      );

      if (response.ok) {
        const created = await unwrap<EscalationRule>(response);
        toast.success(t("escalations.escalationRuleCreated"), {
          description: SWEEP_NOTE,
        });
        setCreateOpen(false);
        setNewName("");
        setNewDescription("");
        setNewPriority(String(PRIORITY_DEFAULT));
        setNewUnassignedFor("60");
        setNewMinimumSeverity(SEVERITY_ANY);
        router.push(detailHref(created.escalationRuleId));
      } else {
        await reportApiError(response, {
          fallback: t("escalations.failedCreateEscalationRule"),
          onUnauthorized,
        });
      }
    } catch {
      toast.error(t("escalations.failedCreateEscalationRule"));
    } finally {
      setSaving(false);
    }
  };

  const handleRetire = async (rule: EscalationRule) => {
    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationId}/escalation-rules/${rule.escalationRuleId}`,
        { method: "DELETE", credentials: "include" }
      );

      if (response.ok) {
        toast.success(t("common.namedRetired", rule.name), {
          description:
            t("escalations.escalatesNothingFromNowCases"),
          action: { label: t("common.undo"), onClick: () => handleRecover(rule) },
        });
        setRetireTarget(null);
        fetchRules();
      } else {
        await reportApiError(response, {
          fallback: t("escalations.failedRetireEscalationRule2"),
          onUnauthorized,
        });
      }
    } catch {
      toast.error(t("escalations.failedRetireEscalationRule2"));
    }
  };

  const handleRecover = async (rule: EscalationRule) => {
    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationId}/escalation-rules/${rule.escalationRuleId}/recover`,
        { method: "POST", credentials: "include" }
      );

      if (response.ok) {
        toast.success(t("escalations.namedBackInOrder", rule.name));
        fetchRules();
        return;
      }

      if (response.status === 409) {
        toast.error(t("escalations.anotherRuleNamed", rule.name), {
          description:
            t("escalations.escalationRuleNamesAreUnique"),
        });
        return;
      }

      await reportApiError(response, {
        fallback: t("escalations.failedRecoverEscalationRule"),
        onUnauthorized,
      });
    } catch {
      toast.error(t("escalations.failedRecoverEscalationRule"));
    }
  };

  // An empty box is NaN rather than 0 — 0 is a valid priority, and the front of
  // the order is the last place a half-typed field should land.
  const priorityValid = isValidPriority(
    newPriority.trim() === "" ? NaN : Number(newPriority)
  );
  const unassignedText = newUnassignedFor.trim();
  const unassignedValid =
    unassignedText === "" || isValidConditionMinutes(Number(unassignedText));
  const hasCondition =
    unassignedText !== "" || newMinimumSeverity !== SEVERITY_ANY;
  const createBlocked =
    saving ||
    newName.trim().length === 0 ||
    !priorityValid ||
    !unassignedValid ||
    !hasCondition;

  const createButton = (
    <Button size="sm" className="gap-1.5" onClick={() => setCreateOpen(true)}>
      <Plus className="size-4" />
      {t("escalations.newRule")}
    </Button>
  );

  return (
    <div className="space-y-4">
      {!embedded && (
        <PageHeader
          eyebrow={t("escalations.escalations")}
          title={t("escalations.escalationRules")}
          description={t("escalations.whatTheSweepDoesAbout")}
        >
          {createButton}
        </PageHeader>
      )}

      {embedded && (
        <div className="space-y-2">
          <p className="text-sm text-slate-500">
            What the sweep does about a case nobody has got to. Listed in the
            order it tries them: the first rule that matches escalates the case,
            and no other rule is tried — so a case is escalated by exactly one.
          </p>
          <p className="text-sm text-slate-500">
            {SWEEP_NOTE}
          </p>
        </div>
      )}

      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="flex flex-wrap items-center gap-3">
          <Input
            value={searchTerm}
            onChange={(event) => {
              setSearchTerm(event.target.value);
              setPageNumber(1);
            }}
            placeholder={t("common.searchByName")}
            maxLength={64}
            className="w-56"
          />
          <Select
            value={state}
            onValueChange={(next) => {
              setState(next);
              setPageNumber(1);
            }}
          >
            <SelectTrigger className="w-40">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value={ANY_STATE}>{t("escalations.allRules")}</SelectItem>
              <SelectItem value="Enabled">{t("escalations.running")}</SelectItem>
              <SelectItem value="Disabled">{t("escalations.paused")}</SelectItem>
            </SelectContent>
          </Select>
        </div>
        <div className="flex items-center gap-3">
          <PageSizeSelect
            value={pageSize}
            onChange={(next) => {
              setPageSize(next);
              setPageNumber(1);
            }}
          />
          {embedded && createButton}
        </div>
      </div>

      <div className="overflow-hidden rounded-xl border border-slate-200 bg-card">
        <Table>
          <TableHeader>
            <TableRow className="bg-slate-50 hover:bg-slate-50">
              <TableHead className="w-36">{t("escalations.runs")}</TableHead>
              <TableHead>{t("common.rule")}</TableHead>
              <TableHead>{t("escalations.when")}</TableHead>
              <TableHead>{t("escalations.then")}</TableHead>
              <TableHead className="w-28">{t("common.status")}</TableHead>
              <TableHead className="w-32">{t("common.actions")}</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {loading ? (
              Array.from({ length: 5 }).map((_, row) => (
                <TableRow key={row}>
                  {Array.from({ length: 6 }).map((_, cell) => (
                    <TableCell key={cell}>
                      <Skeleton className="h-4 w-full" />
                    </TableCell>
                  ))}
                </TableRow>
              ))
            ) : rules.length === 0 ? (
              <TableRow>
                <TableCell colSpan={6} className="py-14 text-center">
                  <p className="font-mono text-xs uppercase tracking-widest text-slate-400">
                    {searchTerm || state !== ANY_STATE
                      ? "No results found"
                      : "No escalation rules yet"}
                  </p>
                  {!searchTerm && state === ANY_STATE && (
                    <p className="mx-auto mt-3 max-w-md text-sm text-slate-500">
                      Nothing is escalated automatically. Cases still fall due
                      and still breach; the desk simply finds out by looking.
                    </p>
                  )}
                </TableCell>
              </TableRow>
            ) : (
              rules.map((rule, index) => {
                const conditions = conditionChips(rule);
                const actions = actionChips(rule, queueName);
                const busy = busyRuleId === rule.escalationRuleId;

                return (
                  <TableRow
                    key={rule.escalationRuleId}
                    className="hover:bg-slate-50/50"
                  >
                    <TableCell>
                      <div className="flex items-center gap-1">
                        <span className="w-10 font-mono text-sm text-slate-900">
                          {rule.priority}
                        </span>
                        <Button
                          variant="ghost"
                          size="icon"
                          className="h-7 w-7 text-slate-400 hover:text-slate-900"
                          title={t("escalations.runThisRuleEarlier")}
                          aria-label={t("escalations.runThisRuleEarlier")}
                          disabled={busy || index === 0}
                          onClick={() => move(index, -1)}
                        >
                          <ArrowUp className="size-3.5" />
                        </Button>
                        <Button
                          variant="ghost"
                          size="icon"
                          className="h-7 w-7 text-slate-400 hover:text-slate-900"
                          title={t("escalations.runThisRuleLater")}
                          aria-label={t("escalations.runThisRuleLater")}
                          disabled={busy || index === rules.length - 1}
                          onClick={() => move(index, 1)}
                        >
                          <ArrowDown className="size-3.5" />
                        </Button>
                      </div>
                    </TableCell>
                    <TableCell className="max-w-xs">
                      <p className="truncate font-medium text-slate-900">
                        {rule.name}
                      </p>
                      {rule.description && (
                        <p className="truncate text-xs text-slate-500">
                          {rule.description}
                        </p>
                      )}
                    </TableCell>
                    <TableCell className="max-w-sm">
                      <div className="flex flex-wrap items-center gap-1">
                        {conditions.map((chip, position) => (
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
                        ))}
                      </div>
                    </TableCell>
                    <TableCell className="max-w-xs">
                      {actions.length === 0 ? (
                        <span className="font-mono text-[11px] uppercase tracking-widest text-slate-400">
                          stamps only
                        </span>
                      ) : (
                        <div className="flex flex-wrap items-center gap-1">
                          {actions.map((chip) => (
                            <span
                              key={chip.key}
                              className="rounded-full border border-elevated-200 bg-elevated-50 px-2 py-0.5 text-xs text-elevated-700"
                            >
                              {chip.label}
                            </span>
                          ))}
                        </div>
                      )}
                    </TableCell>
                    <TableCell>
                      <StatusBadge
                        status={rule.isEnabled ? "Enabled" : "Disabled"}
                        label={rule.isEnabled ? "Running" : "Paused"}
                      />
                    </TableCell>
                    <TableCell>
                      <div className="flex items-center gap-1">
                        <Button
                          variant="outline"
                          size="sm"
                          className="gap-1.5"
                          onClick={() =>
                            router.push(detailHref(rule.escalationRuleId))
                          }
                        >
                          <Eye className="size-4" />
                          {t("common.view")}
                        </Button>
                        {rule.isEnabled ? (
                          <Button
                            variant="ghost"
                            size="icon"
                            className="h-8 w-8 text-slate-500 hover:text-slate-900"
                            title={t("common.retire")}
                            aria-label={t("common.retire")}
                            disabled={busy}
                            onClick={() => setRetireTarget(rule)}
                          >
                            <Archive className="size-4" />
                          </Button>
                        ) : (
                          <Button
                            variant="ghost"
                            size="icon"
                            className="h-8 w-8 text-settled-700 hover:text-settled-700"
                            title={t("escalations.resume")}
                            aria-label={t("escalations.resume")}
                            disabled={busy}
                            onClick={() =>
                              patch(
                                rule,
                                { isEnabled: true },
                                `${rule.name} is running again`
                              )
                            }
                          >
                            <RotateCcw className="size-4" />
                          </Button>
                        )}
                      </div>
                    </TableCell>
                  </TableRow>
                );
              })
            )}
          </TableBody>
        </Table>
      </div>

      {rules.length > 0 && (
        <Pagination
          handlePrevPage={() => setPageNumber((page) => Math.max(1, page - 1))}
          pageNumber={pageNumber}
          totalPages={totalPages}
          handleNextPage={() =>
            setPageNumber((page) => Math.min(totalPages, page + 1))
          }
        />
      )}

      <Dialog open={createOpen} onOpenChange={setCreateOpen}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>{t("escalations.newEscalationRule")}</DialogTitle>
            <DialogDescription>
              One condition to start with. The hold-expiry window, the payload
              matcher and the two actions are on the next screen.
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-4">
            <div className="space-y-1.5">
              <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
                {t("common.name")}
              </label>
              <Input
                value={newName}
                onChange={(event) => setNewName(event.target.value)}
                placeholder={t("escalations.criticalUnassignedForAnHour")}
                maxLength={64}
              />
              <p className="text-xs text-slate-500">
                {t("escalations.uniquePerOrganisationComparedWithout")}
              </p>
            </div>

            <div className="space-y-1.5">
              <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
                {t("common.description")}
              </label>
              <Input
                value={newDescription}
                onChange={(event) => setNewDescription(event.target.value)}
                placeholder={t("escalations.nobodyHasPickedUpA")}
                maxLength={512}
              />
            </div>

            <div className="space-y-1.5">
              <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
                {t("escalations.priority")}
              </label>
              <Input
                value={newPriority}
                onChange={(event) => setNewPriority(event.target.value)}
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

            <div className="space-y-3 rounded-lg border border-slate-200 p-3">
              <p className="text-xs text-slate-500">{CONDITIONS_NOTE}</p>

              <div className="space-y-1.5">
                <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
                  {t("escalations.unassignedForMinutes")}
                </label>
                <Input
                  value={newUnassignedFor}
                  onChange={(event) => setNewUnassignedFor(event.target.value)}
                  inputMode="numeric"
                  placeholder="60"
                  className="font-mono"
                />
                {!unassignedValid && (
                  <p className="text-xs text-critical-500">
                    {t("escalations.mustBeBetween1And")}
                  </p>
                )}
              </div>

              <div className="space-y-1.5">
                <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
                  {t("escalations.severityFloor")}
                </label>
                <Select
                  value={newMinimumSeverity}
                  onValueChange={setNewMinimumSeverity}
                >
                  <SelectTrigger>
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
              </div>

              {!hasCondition && (
                <p className="text-xs text-critical-500">
                  Give the rule at least one condition. Without one it would
                  escalate every open case in the organisation on the next
                  sweep.
                </p>
              )}
            </div>

            <p className="text-xs text-slate-500">{ACTIONS_NOTE}</p>
          </div>

          <DialogFooter>
            <Button variant="outline" onClick={() => setCreateOpen(false)}>
              {t("common.cancel")}
            </Button>
            <Button onClick={handleCreate} disabled={createBlocked}>
              {saving ? "Creating..." : "Create"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <Dialog
        open={retireTarget !== null}
        onOpenChange={(open) => !open && setRetireTarget(null)}
      >
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>
              {t("escalations.retireTitle", retireTarget?.name ?? "")}
            </DialogTitle>
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
            <Button variant="outline" onClick={() => setRetireTarget(null)}>
              {t("common.cancel")}
            </Button>
            {retireTarget?.isEnabled && (
              <Button
                variant="secondary"
                onClick={() => {
                  const target = retireTarget;
                  setRetireTarget(null);
                  patch(
                    target,
                    { isEnabled: false },
                    `${target.name} paused`,
                    "The sweep skips it. It can be turned back on at any time."
                  );
                }}
              >
                {t("escalations.pauseInstead")}
              </Button>
            )}
            <Button
              variant="destructive"
              onClick={() => retireTarget && handleRetire(retireTarget)}
            >
              {t("common.retire")}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
