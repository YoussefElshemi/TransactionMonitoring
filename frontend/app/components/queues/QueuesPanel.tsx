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
  TriangleAlert,
} from "lucide-react";

import { Queue, QueueResource, QueueUsage, ROUTING_POLICIES, RoutingPolicy } from "@/types";
import { API_BASE, reportApiError, unwrap, unwrapPage } from "@/lib/api";
import {
  PRIORITY_DEFAULT,
  PRIORITY_MAX,
  PRIORITY_MIN,
  ROUTING_POLICY_HINTS,
  criteriaChips,
  hasIdleRouting,
  isCatchAll,
  isValidPriority,
  routingPolicyLabel,
} from "@/lib/queues";
import Pagination from "@/components/Pagination";
import { PageHeader } from "@/components/PageHeader";
import { NarrowedListingNote } from "@/components/NarrowedListingNote";
import { StatusBadge } from "@/components/StatusBadge";
import {
  ownerBody,
  SubOrganizationOwner,
  SubOrganizationOwnerField,
} from "@/components/subOrganizations/SubOrganizationOwnerField";
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

import { apiFetch } from "@/lib/http";
import { useTranslation } from "@/context/LocalizationContext";

const ANY_POLICY = "All";
const ANY_STATE = "All";

export function QueuesPanel({
  organizationId,
  detailHref,
  dashboardHref,
  embedded = false,
  onUnauthorized,
}: {
  organizationId: string;
  detailHref: (queueId: string) => string;
  dashboardHref?: string;
  embedded?: boolean;
  onUnauthorized: () => void;
}) {
  const t = useTranslation();
  const router = useRouter();

  const [queues, setQueues] = useState<Queue[]>([]);
  const [pageNumber, setPageNumber] = useState(1);
  const [pageSize, setPageSize] = useState(25);
  const [searchTerm, setSearchTerm] = useState("");
  const [policy, setPolicy] = useState(ANY_POLICY);
  const [state, setState] = useState(ANY_STATE);
  const [totalPages, setTotalPages] = useState(1);
  const [loading, setLoading] = useState(true);
  const [busyQueueId, setBusyQueueId] = useState<string | null>(null);

  const [createOpen, setCreateOpen] = useState(false);
  const [saving, setSaving] = useState(false);
  const [newName, setNewName] = useState("");
  const [newDescription, setNewDescription] = useState("");
  const [newPriority, setNewPriority] = useState(String(PRIORITY_DEFAULT));
  const [newPolicy, setNewPolicy] = useState<RoutingPolicy>("Manual");
  const [newOwner, setNewOwner] = useState<SubOrganizationOwner>(undefined);

  const [retireTarget, setRetireTarget] = useState<{
    queue: Queue;
    usage: QueueUsage | null;
  } | null>(null);

  const fetchQueues = useCallback(async () => {
    setLoading(true);

    try {
      const query = new URLSearchParams({
        PageNumber: String(pageNumber),
        PageSize: String(pageSize),
      });
      if (searchTerm) query.set("Name", searchTerm);
      if (policy !== ANY_POLICY) query.set("RoutingPolicy", policy);
      if (state !== ANY_STATE) query.set("IsEnabled", String(state === "Enabled"));

      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationId}/queues?${query}`,
        {
          method: "GET",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
        }
      );

      if (response.ok) {
        const page = await unwrapPage<QueueResource>(response);
        setQueues(page.data.map(({ queue }) => queue));
        setTotalPages(page.metadata.pageCount);
      } else {
        await reportApiError(response, {
          fallback: t("queues.errorOccurredFetchingQueues"),
          onUnauthorized,
          toastId: "error-fetching-queues",
        });
      }
    } catch {
      toast.error(t("queues.errorOccurredFetchingQueues"), {
        id: "error-fetching-queues",
      });
    } finally {
      setLoading(false);
    }
  }, [organizationId, pageNumber, pageSize, searchTerm, policy, state, onUnauthorized]);

  useEffect(() => {
    fetchQueues();
  }, [fetchQueues]);

  const patch = async (
    queue: Queue,
    body: Record<string, unknown>,
    success: string,
    description?: string
  ) => {
    setBusyQueueId(queue.queueId);
    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationId}/queues/${queue.queueId}`,
        {
          method: "PATCH",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
          body: JSON.stringify(body),
        }
      );

      if (response.ok) {
        toast.success(success, description ? { description } : undefined);
        await fetchQueues();
        return true;
      }

      await reportApiError(response, {
        fallback: t("queues.failedUpdateQueue"),
        onUnauthorized,
      });
      return false;
    } catch {
      toast.error(t("queues.failedUpdateQueue"));
      return false;
    } finally {
      setBusyQueueId(null);
    }
  };

  const move = async (index: number, direction: -1 | 1) => {
    const queue = queues[index];
    const neighbour = queues[index + direction];
    if (!queue || !neighbour) return;

    if (queue.priority === neighbour.priority) {
      const nudged = queue.priority + direction;
      if (nudged < PRIORITY_MIN || nudged > PRIORITY_MAX) {
        toast.error(t("queues.noRoomMoveQueue"), {
          description:
            t("common.sharesPriorityNeighbourAlreadyEnd"),
        });
        return;
      }
      await patch(queue, { priority: nudged }, t("queues.namedMoved", queue.name));
      return;
    }

    setBusyQueueId(queue.queueId);
    try {
      const writes = [
        { target: queue, priority: neighbour.priority },
        { target: neighbour, priority: queue.priority },
      ];

      for (const write of writes) {
        const response = await apiFetch(
          `${API_BASE}/organizations/${organizationId}/queues/${write.target.queueId}`,
          {
            method: "PATCH",
            headers: { "Content-Type": "application/json" },
            credentials: "include",
            body: JSON.stringify({ priority: write.priority }),
          }
        );

        if (!response.ok) {
          await reportApiError(response, {
            fallback: t("queues.failedReorderQueues"),
            onUnauthorized,
          });
          // Refetch even on a partial failure: the first write may already have
          // landed, and leaving the table showing the order it had before would
          // hide a half-applied swap.
          await fetchQueues();
          return;
        }
      }

      toast.success(
        t(
          direction === -1 ? "common.namedMovedUp" : "common.namedMovedDown",
          queue.name
        ),
        {
          description: t(
            direction === -1 ? "queues.nowTriedBefore" : "queues.nowTriedAfter",
            neighbour.name
          ),
        }
      );
      await fetchQueues();
    } catch {
      toast.error(t("queues.failedReorderQueues"));
      await fetchQueues();
    } finally {
      setBusyQueueId(null);
    }
  };

  const handleCreate = async () => {
    setSaving(true);

    try {
      const priority = Number(newPriority);

      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationId}/queues`,
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
          body: JSON.stringify({
            name: newName.trim(),
            description: newDescription.trim() || null,
            priority,
            routingPolicy: newPolicy,
            isEnabled: true,
            ...ownerBody(newOwner),
          }),
        }
      );

      if (response.ok) {
        const created = await unwrap<Queue>(response);
        toast.success(t("queues.queueCreated"), {
          description:
            t("queues.takesEverythingReachesUntilYou"),
        });
        setCreateOpen(false);
        setNewName("");
        setNewDescription("");
        setNewPriority(String(PRIORITY_DEFAULT));
        setNewPolicy("Manual");
        router.push(detailHref(created.queueId));
      } else {
        await reportApiError(response, {
          fallback: t("queues.failedCreateQueue"),
          onUnauthorized,
        });
      }
    } catch {
      toast.error(t("queues.failedCreateQueue"));
    } finally {
      setSaving(false);
    }
  };

  const askToRetire = async (queue: Queue) => {
    setRetireTarget({ queue, usage: null });

    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationId}/queues/${queue.queueId}/usages`,
        {
          method: "GET",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
        }
      );

      if (response.ok) {
        setRetireTarget({ queue, usage: await unwrap<QueueUsage>(response) });
      }
    } catch {
    }
  };

  const handleRetire = async (queue: Queue) => {
    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationId}/queues/${queue.queueId}`,
        { method: "DELETE", credentials: "include" }
      );

      if (response.ok) {
        toast.success(t("common.namedRetired", queue.name), {
          description:
            t("queues.takesNoNewWorkCannot"),
          action: { label: t("common.undo"), onClick: () => handleRecover(queue) },
        });
        setRetireTarget(null);
        fetchQueues();
      } else {
        await reportApiError(response, {
          fallback: t("queues.failedRetireQueue2"),
          onUnauthorized,
        });
      }
    } catch {
      toast.error(t("queues.failedRetireQueue2"));
    }
  };

  const handleRecover = async (queue: Queue) => {
    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationId}/queues/${queue.queueId}/recover`,
        { method: "POST", credentials: "include" }
      );

      if (response.ok) {
        toast.success(t("queues.namedTakingWorkAgain", queue.name));
        fetchQueues();
        return;
      }

      if (response.status === 409) {
        toast.error(t("queues.anotherQueueNamed", queue.name), {
          description:
            t("queues.queueNamesAreUniqueSo"),
        });
        return;
      }

      await reportApiError(response, {
        fallback: t("queues.failedRecoverQueue"),
        onUnauthorized,
      });
    } catch {
      toast.error(t("queues.failedRecoverQueue"));
    }
  };

  // An empty box is NaN rather than 0 — 0 is a valid priority, and the front
  // of the routing order is the last place a half-typed field should land.
  const priorityValid = isValidPriority(
    newPriority.trim() === "" ? NaN : Number(newPriority)
  );
  const createBlocked = saving || newName.trim().length === 0 || !priorityValid;

  const createButton = (
    <Button size="sm" className="gap-1.5" onClick={() => setCreateOpen(true)}>
      <Plus className="size-4" />
      {t("queues.newQueue")}
    </Button>
  );

  return (
    <div className="space-y-4">
      {!embedded && (
        <PageHeader
          eyebrow={t("queues.queues2")}
          title={t("queues.queues")}
          description={t("queues.namedDesksAndTheCriteria")}
        >
          {dashboardHref && (
            <Button
              variant="outline"
              size="sm"
              onClick={() => router.push(dashboardHref)}
            >
              {t("queues.dashboard")}
            </Button>
          )}
          {createButton}
        </PageHeader>
      )}

      {embedded && (
        <p className="text-sm text-slate-500">
          Named desks and the criteria that decide which cases land on them.
          Declaring none is fine — cases then open unrouted exactly as they did
          before queues existed.
        </p>
      )}

      <NarrowedListingNote permission="queues:read" noun="queue" />

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
            value={policy}
            onValueChange={(next) => {
              setPolicy(next);
              setPageNumber(1);
            }}
          >
            <SelectTrigger className="w-44">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value={ANY_POLICY}>{t("queues.anyPolicy")}</SelectItem>
              {ROUTING_POLICIES.map((option) => (
                <SelectItem key={option} value={option}>
                  {routingPolicyLabel(option)}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
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
              <SelectItem value={ANY_STATE}>{t("queues.allQueues")}</SelectItem>
              <SelectItem value="Enabled">{t("queues.takingWork")}</SelectItem>
              <SelectItem value="Disabled">{t("queues.paused")}</SelectItem>
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
              <TableHead className="w-36">{t("queues.tried")}</TableHead>
              <TableHead>{t("queues.queue")}</TableHead>
              <TableHead>{t("queues.takes")}</TableHead>
              <TableHead className="w-40">{t("queues.handsOut")}</TableHead>
              <TableHead className="w-24">{t("queues.members")}</TableHead>
              <TableHead className="w-28">{t("common.status")}</TableHead>
              <TableHead className="w-32">{t("common.actions")}</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {loading ? (
              Array.from({ length: 5 }).map((_, row) => (
                <TableRow key={row}>
                  {Array.from({ length: 7 }).map((_, cell) => (
                    <TableCell key={cell}>
                      <Skeleton className="h-4 w-full" />
                    </TableCell>
                  ))}
                </TableRow>
              ))
            ) : queues.length === 0 ? (
              <TableRow>
                <TableCell colSpan={7} className="py-14 text-center">
                  <p className="font-mono text-xs uppercase tracking-widest text-slate-400">
                    {searchTerm || policy !== ANY_POLICY || state !== ANY_STATE
                      ? "No results found"
                      : "No queues configured yet"}
                  </p>
                  {!searchTerm && policy === ANY_POLICY && state === ANY_STATE && (
                    <p className="mx-auto mt-3 max-w-md text-sm text-slate-500">
                      Nothing is being routed. Cases open unrouted and wait for a
                      supervisor to assign them, exactly as they did before
                      queues existed.
                    </p>
                  )}
                </TableCell>
              </TableRow>
            ) : (
              queues.map((queue, index) => {
                const chips = criteriaChips(queue);
                const idle = hasIdleRouting(queue);
                const busy = busyQueueId === queue.queueId;

                return (
                  <TableRow key={queue.queueId} className="hover:bg-slate-50/50">
                    <TableCell>
                      <div className="flex items-center gap-1">
                        <span className="w-10 font-mono text-sm text-slate-900">
                          {queue.priority}
                        </span>
                        <Button
                          variant="ghost"
                          size="icon"
                          className="h-7 w-7 text-slate-400 hover:text-slate-900"
                          title={t("queues.tryThisQueueEarlier")}
                          aria-label={t("queues.tryThisQueueEarlier")}
                          disabled={busy || index === 0}
                          onClick={() => move(index, -1)}
                        >
                          <ArrowUp className="size-3.5" />
                        </Button>
                        <Button
                          variant="ghost"
                          size="icon"
                          className="h-7 w-7 text-slate-400 hover:text-slate-900"
                          title={t("queues.tryThisQueueLater")}
                          aria-label={t("queues.tryThisQueueLater")}
                          disabled={busy || index === queues.length - 1}
                          onClick={() => move(index, 1)}
                        >
                          <ArrowDown className="size-3.5" />
                        </Button>
                      </div>
                    </TableCell>
                    <TableCell className="max-w-xs">
                      <p className="truncate font-medium text-slate-900">
                        {queue.name}
                      </p>
                      {queue.description && (
                        <p className="truncate text-xs text-slate-500">
                          {queue.description}
                        </p>
                      )}
                    </TableCell>
                    <TableCell className="max-w-xs">
                      {isCatchAll(queue) ? (
                        <span className="font-mono text-[11px] uppercase tracking-widest text-attention-700">
                          {t("queues.everything")}
                        </span>
                      ) : (
                        <div className="flex flex-wrap items-center gap-1">
                          {chips.map((chip, position) => (
                            <span
                              key={chip.key}
                              className="flex items-center gap-1"
                            >
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
                      )}
                    </TableCell>
                    <TableCell>
                      <span
                        className="text-sm text-slate-600"
                        title={
                          ROUTING_POLICY_HINTS[
                            queue.routingPolicy as RoutingPolicy
                          ]
                        }
                      >
                        {routingPolicyLabel(queue.routingPolicy)}
                      </span>
                    </TableCell>
                    <TableCell>
                      <span className="flex items-center gap-1.5 font-mono text-sm text-slate-900">
                        {queue.members.length}
                        {idle && (
                          <TriangleAlert
                            className="size-4 text-attention-700"
                            aria-label={t("queues.noMembersToHandWork")}
                          />
                        )}
                      </span>
                    </TableCell>
                    <TableCell>
                      <StatusBadge
                        status={queue.isEnabled ? "Enabled" : "Disabled"}
                        label={queue.isEnabled ? t("queues.takingWork") : "Paused"}
                      />
                    </TableCell>
                    <TableCell>
                      <div className="flex items-center gap-1">
                        <Button
                          variant="outline"
                          size="sm"
                          className="gap-1.5"
                          onClick={() => router.push(detailHref(queue.queueId))}
                        >
                          <Eye className="size-4" />
                          {t("common.view")}
                        </Button>
                        {queue.isEnabled ? (
                          <Button
                            variant="ghost"
                            size="icon"
                            className="h-8 w-8 text-slate-500 hover:text-slate-900"
                            title={t("common.retire")}
                            aria-label={t("common.retire")}
                            disabled={busy}
                            onClick={() => askToRetire(queue)}
                          >
                            <Archive className="size-4" />
                          </Button>
                        ) : (
                          <Button
                            variant="ghost"
                            size="icon"
                            className="h-8 w-8 text-settled-700 hover:text-settled-700"
                            title={t("queues.resumeTakingWork")}
                            aria-label={t("queues.resumeTakingWork")}
                            disabled={busy}
                            onClick={() =>
                              patch(
                                queue,
                                { isEnabled: true },
                                t("queues.namedTakingWorkAgain", queue.name)
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

      {queues.length > 0 && (
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
            <DialogTitle>{t("queues.newQueue2")}</DialogTitle>
            <DialogDescription>
              A desk cases land on. You will set what it takes, and who works it,
              on the next screen.
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
                placeholder={t("queues.highValueEmea")}
                maxLength={64}
              />
              <p className="text-xs text-slate-500">
                Unique per organisation, compared without case. Unlike a reason
                code, a queue can be renamed later.
              </p>
            </div>

            <div className="space-y-1.5">
              <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
                {t("common.description")}
              </label>
              <Input
                value={newDescription}
                onChange={(event) => setNewDescription(event.target.value)}
                placeholder={t("queues.transactionsOver10kWithAn")}
                maxLength={512}
              />
            </div>

            <div className="space-y-1.5">
              <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
                {t("queues.priority")}
              </label>
              <Input
                value={newPriority}
                onChange={(event) => setNewPriority(event.target.value)}
                inputMode="numeric"
                className="w-32 font-mono"
              />
              <p className="text-xs text-slate-500">
                0–1000, and <span className="font-medium">lower is tried
                first</span>. Left at 1000 the queue goes to the back of the
                line, so it cannot take work off the queues already there.
              </p>
              {!priorityValid && newPriority.trim().length > 0 && (
                <p className="text-xs text-critical-500">
                  {t("queues.mustBeAWholeNumber")}
                </p>
              )}
            </div>

            <div className="space-y-1.5">
              <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
                {t("queues.handsOut")}
              </label>
              <Select
                value={newPolicy}
                onValueChange={(next) => setNewPolicy(next as RoutingPolicy)}
              >
                <SelectTrigger>
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
                {ROUTING_POLICY_HINTS[newPolicy]}
              </p>
            </div>

            <SubOrganizationOwnerField
              organizationId={organizationId}
              value={newOwner}
              onChange={setNewOwner}
              disabled={saving}
              allowUnchanged={false}
              label={t("queues.ownedBy")}
            />
          </div>

          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => setCreateOpen(false)}
              disabled={saving}
            >
              {t("common.cancel")}
            </Button>
            <Button onClick={handleCreate} disabled={createBlocked}>
              {saving ? "Creating..." : "Create"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <RetireDialog
        target={retireTarget}
        onOpenChange={(open) => !open && setRetireTarget(null)}
        onRetire={handleRetire}
        onPause={(queue) => {
          setRetireTarget(null);
          patch(
            queue,
            { isEnabled: false },
            t("common.namedPaused", queue.name),
            t("queues.routingSkipsTurnBackOn")
          );
        }}
      />
    </div>
  );
}

function RetireDialog({
  target,
  onOpenChange,
  onRetire,
  onPause,
}: {
  target: { queue: Queue; usage: QueueUsage | null } | null;
  onOpenChange: (open: boolean) => void;
  onRetire: (queue: Queue) => void;
  onPause: (queue: Queue) => void;
}) {
  const t = useTranslation();
  const usage = target?.usage;

  return (
    <Dialog open={target !== null} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>
              {t("queues.retireTitle", target?.queue.name ?? "")}
            </DialogTitle>
          <DialogDescription>
            {usage
              ? usage.openCaseCount > 0
                ? `${usage.openCaseCount} open case${
                    usage.openCaseCount === 1 ? "" : "s"
                  } on this queue will stop being claimable. ${
                    usage.memberCount
                  } member${usage.memberCount === 1 ? "" : "s"} will have one fewer queue to pull from.`
                : "Nothing open is waiting on it, so nobody loses work they were about to pick up."
              : "What this would touch could not be read, so how much open work is on this queue is unknown."}
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-2 text-sm text-slate-600">
          <p>
            Retiring takes the queue out of the routing order and stops it being
            claimed from. Cases already routed to it keep its name and stay
            readable and assignable by hand — a case whose desk vanished would
            read as work nobody ever triaged.
          </p>
          {usage && usage.caseCount > 0 && (
            <p className="font-mono text-[11px] uppercase tracking-widest text-slate-400">
              {usage.caseCount} case{usage.caseCount === 1 ? "" : "s"} name this
              queue in total
            </p>
          )}
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            {t("common.cancel")}
          </Button>
          {target?.queue.isEnabled && (
            <Button variant="secondary" onClick={() => onPause(target.queue)}>
              {t("queues.pauseInstead")}
            </Button>
          )}
          {target && (
            <Button variant="destructive" onClick={() => onRetire(target.queue)}>
              {t("common.retire")}
            </Button>
          )}
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
