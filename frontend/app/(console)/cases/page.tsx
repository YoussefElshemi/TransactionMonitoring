"use client";

import { useState, useEffect, useCallback } from "react";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import { Case, CaseResource, Paged, ProblemDetails, ValidationProblemDetails } from "@/types";
import { toast } from "sonner";
import Pagination from "@/components/Pagination";
import { StatusBadge } from "@/components/StatusBadge";
import { SeverityBadge } from "@/components/SeverityBadge";
import { DispositionBadge } from "@/components/DispositionBadge";
import { CaseAlertSummary } from "@/components/alerts/AlertParts";
import { PageHeader } from "@/components/PageHeader";
import { useTranslation } from "@/context/LocalizationContext";
import { NarrowedListingNote } from "@/components/NarrowedListingNote";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
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
import { Skeleton } from "@/components/ui/skeleton";
import { TabBar } from "@/components/TabBar";
import { AssignCaseDialog } from "@/components/AssignCaseDialog";
import { QueueRef } from "@/components/queues/QueueRef";
import {
  QUEUE_ANY,
  QUEUE_UNROUTED,
  QueueFilterSelect,
} from "@/components/queues/QueueFilterSelect";
import {
  CaseAgeingCell,
  EscalatedMark,
  useElapsedSince,
} from "@/components/cases/CaseAgeing";
import {
  BREACH_ANY,
  BREACH_BREACHED,
  BREACH_WITHIN,
  DUE_WITHIN_ANY,
  DeadlineFilterState,
  DeadlineFilters,
  ESCALATED_ANY,
  ESCALATED_NO,
  ESCALATED_YES,
  deadlineParams,
  hasDeadlineFilters,
} from "@/components/cases/DeadlineFilters";
import { relationText, userName } from "@/lib/resources";
import { usePermissions } from "@/context/PermissionsContext";
import { UserPlus, UserMinus, Eye } from "lucide-react";
import { loginHref } from "@/lib/authRedirect";
import { SEVERITY_FILTERS } from "@/lib/severity";
import { AGEING_REFRESH_MS, isAgeingFrozen, isRuleEscalated } from "@/lib/ageing";
import {
  CASE_STATUSES,
  DISPOSITIONS,
  caseStatusLabel,
  dispositionLabelKey,
  isCaseInProgress,
} from "@/lib/cases";

import { apiFetch } from "@/lib/http";

import { LocalTime } from "@/components/LocalTime";
import { PageSizeSelect } from "@/components/PageSizeSelect";
import { API_BASE } from "@/lib/api";
const STATUSES = ["All", ...CASE_STATUSES];

const ANY_SEVERITY = "All";

const ANY_DISPOSITION = "All";

const initialQueueFilter = (params: URLSearchParams): string => {
  if (params.get("unrouted") === "true") return QUEUE_UNROUTED;
  return params.get("queueId") || QUEUE_ANY;
};

const Cases = () => {
  const t = useTranslation();
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const redirectUri = searchParams.toString() ? `${pathname}?${searchParams.toString()}` : pathname;

  const [cases, setCases] = useState<CaseResource[]>([]);
  const [pageNumber, setPageNumber] = useState(parseInt(searchParams.get("pageNumber") || "1"));
  const [pageSize, setPageSize] = useState(parseInt(searchParams.get("pageSize") || "10"));
  // An *absent* `status` starts on Pending, the usual thing to be looking at.
  // An explicitly empty one means All, which is what the untriaged link sends —
  // work nobody has triaged is not usefully narrowed to one status.
  const [status, setStatus] = useState(
    searchParams.get("status") ?? "Pending"
  );
  const [queueFilter, setQueueFilter] = useState(() =>
    initialQueueFilter(new URLSearchParams(searchParams.toString()))
  );
  const [queueMemberUserId, setQueueMemberUserId] = useState(
    searchParams.get("queueMemberUserId") || ""
  );
  const [severity, setSeverity] = useState(
    searchParams.get("severity") || ANY_SEVERITY
  );
  const [disposition, setDisposition] = useState(
    searchParams.get("disposition") || ANY_DISPOSITION
  );
  const [searchTerm, setSearchTerm] = useState(searchParams.get("friendlyName") || "");
  const [deadlines, setDeadlines] = useState<DeadlineFilterState>(() => ({
    breach:
      searchParams.get("breached") === "true"
        ? BREACH_BREACHED
        : searchParams.get("breached") === "false"
        ? BREACH_WITHIN
        : BREACH_ANY,
    escalated:
      searchParams.get("escalated") === "true"
        ? ESCALATED_YES
        : searchParams.get("escalated") === "false"
        ? ESCALATED_NO
        : ESCALATED_ANY,
    dueWithin: searchParams.get("dueWithinHours") || DUE_WITHIN_ANY,
  }));
  const [totalPages, setTotalPages] = useState(1);
  const [loading, setLoading] = useState(false);
  const [fetchedAt, setFetchedAt] = useState(0);
  const elapsed = useElapsedSince(fetchedAt);
  const [assignTarget, setAssignTarget] = useState<Case | null>(null);
  const [assigning, setAssigning] = useState(false);
  const { holdsOver } = usePermissions();

  const fetchCases = useCallback(
    async (
      pageNumber: number,
      pageSize: number,
      status: string,
      severity: string,
      disposition: string,
      searchTerm: string,
      queueFilter: string,
      queueMemberUserId: string,
      deadlines: DeadlineFilterState,
      quiet = false
    ) => {
      if (!quiet) setLoading(true);
      const effectiveStatus = STATUSES.includes(status) ? status : "";
      const effectiveSeverity = SEVERITY_FILTERS.includes(
        severity as (typeof SEVERITY_FILTERS)[number]
      )
        ? severity
        : "";
      const effectiveDisposition = DISPOSITIONS.includes(
        disposition as (typeof DISPOSITIONS)[number]
      )
        ? disposition
        : "";
      const routing = new URLSearchParams();
      if (queueFilter === QUEUE_UNROUTED) routing.set("Unrouted", "true");
      else if (queueFilter !== QUEUE_ANY) routing.set("QueueId", queueFilter);
      if (queueMemberUserId) routing.set("QueueMemberUserId", queueMemberUserId);
      const routingQuery = routing.toString() ? `&${routing}` : "";
      const deadlineQuery = deadlineParams(deadlines);
      const deadlineSuffix = deadlineQuery.toString()
        ? `&${deadlineQuery}`
        : "";
      try {
        const response = await apiFetch(
          `${API_BASE}/cases?PageNumber=${pageNumber}&PageSize=${pageSize}&Status=${effectiveStatus}&Severity=${effectiveSeverity}&Disposition=${effectiveDisposition}&FriendlyName=${searchTerm}${routingQuery}${deadlineSuffix}`,
          { method: "GET", headers: { "Content-Type": "application/json" }, credentials: "include" }
        );
        if (response.ok) {
          const data = (await response.json()) as Paged<CaseResource>;
          setCases(data.data);
          setTotalPages(data.metadata.pageCount);
          setFetchedAt(Date.now());
        } else if (response.status === 401) {
          toast.error(t("common.pleaseLoginAccessResource"), { id: "unauthorized" });
          router.push(loginHref(redirectUri));
        } else if (response.status === 403) {
          const e = (await response.json()) as ProblemDetails;
          toast.error(e.detail || t("common.youDoNotHavePermission"), { id: "forbidden" });
          router.push("/");
        } else {
          const e = (await response.json()) as ProblemDetails;
          toast.error(e.detail || t("cases.errorOccurredFetchingCases"), { id: "error-fetching" });
        }
      } catch {
        toast.error(t("cases.errorOccurredFetchingCases"), { id: "error-fetching" });
      } finally {
        setLoading(false);
      }
    },
    [router, redirectUri]
  );

  const handleAssignCase = async (caseId: string, userId?: string) => {
    setAssigning(true);
    try {
      const response = await apiFetch(`${API_BASE}/cases/${caseId}/assign`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        credentials: "include",
        body: JSON.stringify(userId ? { userId } : {}),
      });
      if (response.ok) {
        setAssignTarget(null);
        if (userId) {
          toast.success(t("cases.successfullyAssignedCase"));
          fetchCases(pageNumber, pageSize, status, severity, disposition, searchTerm, queueFilter, queueMemberUserId, deadlines);
        } else {
          toast.success(t("cases.caseAssignedYou"));
          router.push(`/cases/${caseId}`);
        }
      } else if (response.status === 400) {
        const e = (await response.json()) as ValidationProblemDetails;
        toast.error(
          Object.keys(e.errors)
            .map((key) => `${key}${key.length ? ": " : ""}${e.errors[key].join("\n")}`)
            .join("\n")
        );
      } else if (response.status === 401) {
        toast.error(t("common.pleaseLoginPerformAction"));
        router.push(loginHref(redirectUri));
      } else if (response.status === 403) {
        const e = (await response.json()) as ProblemDetails;
        toast.error(e.detail || t("cases.assigningCaseSomebodyElseNeeds"), {
          description: t("cases.youCanStillAssignCase"),
          action: { label: t("cases.action.assignToMe"), onClick: () => handleAssignCase(caseId) },
        });
      } else {
        const e = (await response.json()) as ProblemDetails;
        toast.error(e.detail || t("cases.errorOccurredAssigningCase"));
      }
    } catch {
      toast.error(t("cases.errorOccurredAssigningCase"));
    } finally {
      setAssigning(false);
    }
  };

  const handleUnassignCase = async (caseId: string) => {
    try {
      const response = await apiFetch(`${API_BASE}/cases/${caseId}/unassign`, {
        method: "POST", headers: { "Content-Type": "application/json" }, credentials: "include", body: JSON.stringify({}),
      });
      if (response.ok) {
        toast.success(t("cases.successfullyUnassignedCase"));
        fetchCases(pageNumber, pageSize, status, severity, disposition, searchTerm, queueFilter, queueMemberUserId, deadlines);
      } else if (response.status === 401) {
        toast.error(t("common.pleaseLoginPerformAction"));
        router.push(loginHref(redirectUri));
      } else if (response.status === 403) {
        const e = (await response.json()) as ProblemDetails;
        toast.error(e.detail || t("common.youDoNotHavePermission2"));
      } else {
        const e = (await response.json()) as ProblemDetails;
        toast.error(e.detail || t("cases.errorOccurredUnassigningCase"));
      }
    } catch {
      toast.error(t("cases.errorOccurredUnassigningCase"));
    }
  };

  useEffect(() => {
    fetchCases(pageNumber, pageSize, status, severity, disposition, searchTerm, queueFilter, queueMemberUserId, deadlines);
  }, [
    fetchCases,
    pageNumber,
    pageSize,
    status,
    severity,
    disposition,
    searchTerm,
    queueFilter,
    queueMemberUserId,
    deadlines,
  ]);

  useEffect(() => {
    const timer = setInterval(() => {
      fetchCases(
        pageNumber,
        pageSize,
        status,
        severity,
        disposition,
        searchTerm,
        queueFilter,
        queueMemberUserId,
        deadlines,
        true
      );
    }, AGEING_REFRESH_MS);
    return () => clearInterval(timer);
  }, [
    fetchCases,
    pageNumber,
    pageSize,
    status,
    severity,
    disposition,
    searchTerm,
    queueFilter,
    queueMemberUserId,
    deadlines,
  ]);

  useEffect(() => {
    const params = new URLSearchParams();
    params.set("pageNumber", pageNumber.toString());
    params.set("pageSize", pageSize.toString());
    // Always written, even when empty: an absent `status` means Pending on a
    // fresh load and an empty one means All, so dropping the empty value would
    // turn a shared "untriaged, any status" link back into a Pending one.
    params.set("status", status);
    if (severity !== ANY_SEVERITY) params.set("severity", severity);
    if (disposition !== ANY_DISPOSITION) params.set("disposition", disposition);
    if (searchTerm) params.set("friendlyName", searchTerm);
    if (queueFilter === QUEUE_UNROUTED) params.set("unrouted", "true");
    else if (queueFilter !== QUEUE_ANY) params.set("queueId", queueFilter);
    if (queueMemberUserId) params.set("queueMemberUserId", queueMemberUserId);
    if (deadlines.breach === BREACH_BREACHED) params.set("breached", "true");
    else if (deadlines.breach === BREACH_WITHIN) params.set("breached", "false");
    if (deadlines.escalated === ESCALATED_YES) params.set("escalated", "true");
    else if (deadlines.escalated === ESCALATED_NO) {
      params.set("escalated", "false");
    }
    if (deadlines.dueWithin !== DUE_WITHIN_ANY) {
      params.set("dueWithinHours", deadlines.dueWithin);
    }
    router.replace(`?${params.toString()}`);
  }, [
    pageNumber,
    pageSize,
    status,
    severity,
    disposition,
    searchTerm,
    queueFilter,
    queueMemberUserId,
    deadlines,
    router,
  ]);

  const activeTab = status || "All";

  const applyView = (next: Partial<DeadlineFilterState>) => {
    setStatus("");
    setDeadlines({ ...deadlines, ...next });
    setPageNumber(1);
  };

  return (
    <div className="space-y-4">
      <PageHeader
        eyebrow={t("cases.list.eyebrow")}
        title={t("cases.list.title")}
        description={t("cases.list.description")}
      >
        <Button
          variant="outline"
          size="sm"
          onClick={() =>
            applyView({ breach: BREACH_BREACHED, dueWithin: DUE_WITHIN_ANY })
          }
        >
          {t("cases.list.view.breached")}
        </Button>
        <Button
          variant="outline"
          size="sm"
          onClick={() => applyView({ breach: BREACH_ANY, dueWithin: "4" })}
        >
          {t("cases.list.view.dueInFourHours")}
        </Button>
        {hasDeadlineFilters(deadlines) && (
          <Button
            variant="ghost"
            size="sm"
            className="text-slate-500 hover:text-slate-900"
            onClick={() => {
              setDeadlines({
                breach: BREACH_ANY,
                escalated: ESCALATED_ANY,
                dueWithin: DUE_WITHIN_ANY,
              });
              setPageNumber(1);
            }}
          >
            {t("cases.list.view.clearDeadlines")}
          </Button>
        )}
      </PageHeader>

      <NarrowedListingNote permission="cases:read" noun="case" />

      {queueMemberUserId && (
        <div className="flex items-start justify-between gap-4 rounded-lg border border-slate-200 bg-slate-50 px-4 py-3">
          <div>
            <p className="text-sm font-medium text-slate-900">
              {t("cases.casesOnTheQueuesOne")}
            </p>
            <p className="mt-0.5 text-sm text-slate-600">
              What they could be handed, not what they are holding — the
              Assigned To column is what says the second.
            </p>
          </div>
          <Button
            variant="outline"
            size="sm"
            onClick={() => { setQueueMemberUserId(""); setPageNumber(1); }}
          >
            {t("cases.clear")}
          </Button>
        </div>
      )}

      {queueFilter === QUEUE_UNROUTED && (
        <div className="rounded-lg border border-attention-200 bg-attention-50 px-4 py-3">
          <p className="text-sm font-medium text-attention-700">
            {t("cases.untriagedNoQueueAposS")}
          </p>
          <p className="mt-0.5 text-sm text-attention-700">
            Nobody is being handed them and they appear on no queue&apos;s depth.
            Either assign them by hand, or widen a queue so work like this lands
            somewhere next time.
          </p>
        </div>
      )}

      <TabBar
        tabs={STATUSES}
        activeTab={activeTab}
        label={caseStatusLabel}
        onChange={(tab) => {
          setStatus(tab === "All" ? "" : tab);
          setPageNumber(1);
        }}
      />

      <div className="flex items-center justify-between gap-3">
        <div className="flex items-center gap-3">
          <Input
            value={searchTerm}
            onChange={(e) => { setSearchTerm(e.target.value); setPageNumber(1); }}
            placeholder={t("common.searchByName")}
            className="w-64"
          />
          <Select
            value={severity}
            onValueChange={(v) => { setSeverity(v); setPageNumber(1); }}
          >
            <SelectTrigger className="w-44">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value={ANY_SEVERITY}>{t("filter.anySeverity")}</SelectItem>
              {SEVERITY_FILTERS.map((s) => (
                <SelectItem key={s} value={s}>{s}</SelectItem>
              ))}
            </SelectContent>
          </Select>
          <Select
            value={disposition}
            onValueChange={(v) => { setDisposition(v); setPageNumber(1); }}
          >
            <SelectTrigger className="w-48">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value={ANY_DISPOSITION}>{t("cases.anyDisposition")}</SelectItem>
              {DISPOSITIONS.map((d) => (
                <SelectItem key={d} value={d}>{t(dispositionLabelKey(d))}</SelectItem>
              ))}
            </SelectContent>
          </Select>
          <QueueFilterSelect
            value={queueFilter}
            onChange={(next) => { setQueueFilter(next); setPageNumber(1); }}
          />
          <DeadlineFilters
            value={deadlines}
            onChange={(next) => { setDeadlines(next); setPageNumber(1); }}
          />
        </div>
        <PageSizeSelect
          value={pageSize}
          onChange={(next) => {
            setPageSize(next);
            setPageNumber(1);
          }}
        />
      </div>

      <div className="overflow-hidden rounded-xl border border-slate-200 bg-card">
        <Table>
          <TableHeader>
            <TableRow className="bg-slate-50 hover:bg-slate-50">
              <TableHead>{t("cases.column.name")}</TableHead>
              <TableHead>{t("cases.column.severity")}</TableHead>
              <TableHead className="w-32">{t("cases.column.holds")}</TableHead>
              <TableHead className="w-28">{t("cases.column.due")}</TableHead>
              <TableHead>{t("cases.column.status")}</TableHead>
              <TableHead>{t("cases.column.concluded")}</TableHead>
              <TableHead>{t("cases.column.created")}</TableHead>
              <TableHead>{t("cases.column.queue")}</TableHead>
              <TableHead>{t("cases.column.assignedTo")}</TableHead>
              <TableHead className="w-32">
                <span className="sr-only">{t("common.actions")}</span>
              </TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {loading ? (
              Array.from({ length: 5 }).map((_, i) => (
                <TableRow key={i}>
                  {Array.from({ length: 10 }).map((_, j) => (
                    <TableCell key={j}><Skeleton className="h-4 w-full" /></TableCell>
                  ))}
                </TableRow>
              ))
            ) : cases.length === 0 ? (
              <TableRow>
                <TableCell colSpan={10} className="py-14 text-center font-mono text-xs uppercase tracking-widest text-slate-400">
                  {t("cases.empty")}
                </TableCell>
              </TableRow>
            ) : (
              cases.map(({ case: _case, ageing, assignedToUser }) => (
                <TableRow key={_case.caseId} className="hover:bg-slate-50/50">
                  <TableCell className="font-medium text-slate-900">
                    {_case.friendlyName}
                  </TableCell>
                  <TableCell>
                    <SeverityBadge severity={_case.severity} emphasis />
                  </TableCell>
                  <TableCell>
                    <CaseAlertSummary
                      alerts={_case.alerts}
                      caseSeverity={_case.severity}
                    />
                  </TableCell>
                  <TableCell>
                    <CaseAgeingCell
                      ageing={ageing}
                      dueAt={_case.dueAt}
                      frozen={isAgeingFrozen(_case)}
                      elapsedMilliseconds={elapsed}
                    />
                  </TableCell>
                  <TableCell>
                    <div className="space-y-1">
                      <StatusBadge status={_case.status} />
                      {isRuleEscalated(_case) && (
                        <EscalatedMark escalatedAt={_case.escalatedAt!} />
                      )}
                    </div>
                  </TableCell>
                  <TableCell>
                    {_case.disposition ? (
                      <DispositionBadge disposition={_case.disposition} />
                    ) : (
                      <span className="text-sm text-slate-300">—</span>
                    )}
                  </TableCell>
                  <TableCell className="text-sm text-slate-600">
                    <LocalTime value={_case.createdAt} />
                  </TableCell>
                  <TableCell>
                    <QueueRef
                      queueId={_case.queueId}
                      queueName={_case.queueName}
                      href={
                        holdsOver("queues:read", { queueId: _case.queueId })
                          ? (queueId) => `/queues/manage/${queueId}`
                          : undefined
                      }
                    />
                  </TableCell>
                  <TableCell className="text-sm text-slate-600">
                    {relationText(
                      userName(assignedToUser),
                      _case.assignedToUserId
                    )}
                  </TableCell>
                  <TableCell>
                    <div className="flex items-center gap-1">
                      <Button
                        variant="outline"
                        size="sm"
                        className="gap-1.5"
                        onClick={() => router.push(`/cases/${_case.caseId}`)}
                      >
                        <Eye className="size-4" />
                        {t("common.view")}
                      </Button>
                      {_case.status === "Pending" &&
                        (() => {
                          const target = { queueId: _case.queueId };
                          const assignOthers = holdsOver("cases:assign", target);
                          const takeSelf = holdsOver("cases:write", target);
                          if (!assignOthers && !takeSelf) return null;

                          return (
                            <Button
                              variant="outline"
                              size="icon"
                              className="h-8 w-8 text-inflight-700 hover:text-inflight-700"
                              onClick={() =>
                                assignOthers
                                  ? setAssignTarget(_case)
                                  : handleAssignCase(_case.caseId)
                              }
                              disabled={assigning}
                              title={
                                assignOthers
                                  ? t("cases.action.assign")
                                  : t("cases.action.assignToMe")
                              }
                              aria-label={
                                assignOthers
                                  ? t("cases.action.assignTo", _case.friendlyName)
                                  : t("cases.action.assignToMeNamed", _case.friendlyName)
                              }
                            >
                              <UserPlus aria-hidden className="size-4" />
                            </Button>
                          );
                        })()}
                      {isCaseInProgress(_case) && (
                        <Button
                          variant="outline"
                          size="icon"
                          className="h-8 w-8 text-slate-600 hover:text-slate-900"
                          onClick={() => handleUnassignCase(_case.caseId)}
                          title={t("cases.action.unassign")}
                          aria-label={t(
                            "cases.action.unassignNamed",
                            _case.friendlyName
                          )}
                        >
                          <UserMinus aria-hidden className="size-4" />
                        </Button>
                      )}
                    </div>
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </div>

      {cases.length > 0 && (
        <Pagination
          handlePrevPage={() => setPageNumber((p) => Math.max(1, p - 1))}
          pageNumber={pageNumber}
          totalPages={totalPages}
          handleNextPage={() => setPageNumber((p) => Math.min(totalPages, p + 1))}
        />
      )}

      <AssignCaseDialog
        open={assignTarget !== null}
        onOpenChange={(open) => !open && setAssignTarget(null)}
        assigning={assigning}
        onAssign={(userId) => {
          if (assignTarget) handleAssignCase(assignTarget.caseId, userId);
        }}
      />
    </div>
  );
};

export default Cases;
