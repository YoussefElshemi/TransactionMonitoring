"use client";

import { useState, useEffect } from "react";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import {
  ApprovalResource,
  Case,
  CaseResource,
  Paged,
  ProblemDetails,
  ValidationProblemDetails
} from "@/types";
import { toast } from "sonner";
import Pagination from "@/components/Pagination";
import { StatusBadge } from "@/components/StatusBadge";
import { SeverityBadge } from "@/components/SeverityBadge";
import { DispositionBadge } from "@/components/DispositionBadge";
import { CaseClose, CloseCaseDialog } from "@/components/cases/CloseCaseDialog";
import { CaseAlertSummary } from "@/components/alerts/AlertParts";
import Link from "next/link";
import { PageHeader } from "@/components/PageHeader";
import { useTranslation } from "@/context/LocalizationContext";
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
import { UserMinus, Eye, Gavel, Send } from "lucide-react";
import { loginHref } from "@/lib/authRedirect";
import { API_BASE, unwrap } from "@/lib/api";
import { approvalHref } from "@/lib/approvals";
import { SEVERITY_FILTERS } from "@/lib/severity";
import { QueueRef } from "@/components/queues/QueueRef";
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
} from "@/components/cases/DeadlineFilters";
import { relationText, userName } from "@/lib/resources";
import { AGEING_REFRESH_MS, isAgeingFrozen, isRuleEscalated } from "@/lib/ageing";
import {
  DISPOSITIONS,
  caseStatusLabel,
  dispositionLabelKey,
  isCaseInProgress,
} from "@/lib/cases";

import { apiFetch } from "@/lib/http";

import { LocalTime } from "@/components/LocalTime";
const ANY_SEVERITY = "All";
const ANY_DISPOSITION = "All";

const Cases = () => {
  const t = useTranslation();
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();

  const redirectUri = searchParams.toString()
    ? `${pathname}?${searchParams.toString()}`
    : pathname;
  const [cases, setCases] = useState<CaseResource[]>([]);
  const [pageNumber, setPageNumber] = useState<number>(
    parseInt(searchParams.get("pageNumber") || "1")
  );
  const [pageSize, setPageSize] = useState<number>(
    parseInt(searchParams.get("pageSize") || "10")
  );
  const [status, setStatus] = useState<string>(
    searchParams.get("status") || "Assigned"
  );
  const [severity, setSeverity] = useState<string>(
    searchParams.get("severity") || ANY_SEVERITY
  );
  const [disposition, setDisposition] = useState<string>(
    searchParams.get("disposition") || ANY_DISPOSITION
  );
  const [searchTerm, setSearchTerm] = useState<string>(
    searchParams.get("friendlyName") || ""
  );
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
  const [totalPages, setTotalPages] = useState<number>(1);
  const [loading, setLoading] = useState<boolean>(false);
  const [fetchedAt, setFetchedAt] = useState(0);
  const elapsed = useElapsedSince(fetchedAt);
  const [closing, setClosing] = useState<Case | null>(null);
  const [resolving, setResolving] = useState<boolean>(false);

  const statuses = [
    "Assigned",
    "InReview",
    "Escalated",
    "PendingInformation",
    "OnHold",
    "Approved",
    "Rejected",
    "Expired",
  ];

  const fetchCases = async (
    pageNumber: number,
    pageSize: number,
    status: string,
    severity: string,
    disposition: string,
    searchTerm: string,
    deadlines: DeadlineFilterState,
    quiet = false
  ) => {
    if (!quiet) setLoading(true);

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

    const deadlineQuery = deadlineParams(deadlines);
    const deadlineSuffix = deadlineQuery.toString() ? `&${deadlineQuery}` : "";

    try {
      const response = await apiFetch(
        `${API_BASE}/cases/my?PageNumber=${pageNumber}&PageSize=${pageSize}&Status=${status}&Severity=${effectiveSeverity}&Disposition=${effectiveDisposition}&FriendlyName=${searchTerm}${deadlineSuffix}`,
        {
          method: "GET",
          headers: {
            "Content-Type": "application/json"
          },
          credentials: "include"
        }
      );

      if (response.ok) {
        const data = (await response.json()) as Paged<CaseResource>;
        setCases(data.data);
        setTotalPages(data.metadata.pageCount);
        setFetchedAt(Date.now());
      } else if (response.status === 400) {
        const errorData = (await response.json()) as ValidationProblemDetails;
        toast.error(
          Object.keys(errorData.errors)
            .map(
              (key) =>
                `${key}${key.length ? ": " : ""}${errorData.errors[key].join("\n")}`
            )
            .join("\n")
        );
      } else if (response.status === 401) {
        toast.error(t("common.pleaseLoginAccessResource"), {
          id: "unauthorized"
        });
        return router.push(loginHref(redirectUri));
      } else if (response.status === 403) {
        const errorData = (await response.json()) as ProblemDetails;
        toast.error(
          errorData.detail ||
            t("common.youDoNotHavePermission"),
          {
            id: "forbidden"
          }
        );
        return router.push("/");
      } else {
        const errorData = (await response.json()) as ProblemDetails;
        toast.error(
          errorData.detail || t("cases.errorOccurredFetchingCases"),
          { id: "error-fetching" }
        );
      }
    } catch {
      toast.error(t("cases.errorOccurredFetchingCases"), {
        id: "error-fetching"
      });
    } finally {
      setLoading(false);
    }
  };

  const handleUnassignCase = async (caseId: string) => {
    setLoading(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/cases/${caseId}/unassign`,
        {
          method: "POST",
          headers: {
            "Content-Type": "application/json"
          },
          credentials: "include",
          body: JSON.stringify({})
        }
      );

      if (response.ok) {
        toast.success(t("cases.successfullyUnassignedCase"));
        fetchCases(pageNumber, pageSize, status, severity, disposition, searchTerm, deadlines);
      } else if (response.status === 400) {
        const errorData = (await response.json()) as ValidationProblemDetails;
        toast.error(
          Object.keys(errorData.errors)
            .map(
              (key) =>
                `${key}${key.length ? ": " : ""}${errorData.errors[key].join("\n")}`
            )
            .join("\n")
        );
      } else if (response.status === 401) {
        toast.error(t("common.pleaseLoginPerformAction"));
        return router.push(loginHref(redirectUri));
      } else if (response.status === 403) {
        const errorData = (await response.json()) as ProblemDetails;
        toast.error(
          errorData.detail ||
            t("common.youDoNotHavePermission2")
        );
      } else {
        const errorData = (await response.json()) as ProblemDetails;
        toast.error(
          errorData.detail || t("cases.errorOccurredUnassigningCase")
        );
      }
    } catch {
      toast.error(t("cases.errorOccurredUnassigningCase"));
    } finally {
      setLoading(false);
    }
  };

  const handleResolveCase = async (caseId: string, close: CaseClose) => {
    setResolving(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/cases/${caseId}/resolve`,
        {
          method: "POST",
          headers: {
            "Content-Type": "application/json"
          },
          credentials: "include",
          body: JSON.stringify(close)
        }
      );

      if (response.status === 202) {
        const { approval } = await unwrap<ApprovalResource>(response);
        setClosing(null);
        toast.success(t("cases.sentApproval"), {
          description:
            t("cases.caseStillOpenStillHolding"),
          action: {
            label: t("rules.openRequest"),
            onClick: () => router.push(approvalHref(approval.approvalId))
          }
        });
        fetchCases(pageNumber, pageSize, status, severity, disposition, searchTerm, deadlines);
      } else if (response.ok) {
        toast.success(t("cases.successfullyResolvedCase"));
        setClosing(null);
        fetchCases(pageNumber, pageSize, status, severity, disposition, searchTerm, deadlines);
      } else if (response.status === 409) {
        const errorData = (await response.json()) as ProblemDetails;
        setClosing(null);
        toast.error(
          errorData.detail || t("cases.caseAlreadyAwaitingApproval"),
          {
            action: {
              label: t("cases.seeRequests"),
              onClick: () =>
                router.push("/approvals?status=Pending&subject=CaseResolution")
            }
          }
        );
      } else if (response.status === 400) {
        const errorData = (await response.json()) as ValidationProblemDetails;
        toast.error(
          Object.keys(errorData.errors)
            .map(
              (key) =>
                `${key}${key.length ? ": " : ""}${errorData.errors[key].join("\n")}`
            )
            .join("\n")
        );
      } else if (response.status === 401) {
        toast.error(t("common.pleaseLoginPerformAction"));
        return router.push(loginHref(redirectUri));
      } else if (response.status === 403) {
        const errorData = (await response.json()) as ProblemDetails;
        toast.error(
          errorData.detail ||
            t("common.youDoNotHavePermission2")
        );
      } else {
        const errorData = (await response.json()) as ProblemDetails;
        toast.error(
          errorData.detail || t("cases.errorOccurredResolvingCase")
        );
      }
    } catch {
      toast.error(t("cases.errorOccurredResolvingCase"));
    } finally {
      setResolving(false);
    }
  };

  useEffect(() => {
    fetchCases(pageNumber, pageSize, status, severity, disposition, searchTerm, deadlines);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [pageNumber, pageSize, status, severity, disposition, searchTerm, deadlines]);

  useEffect(() => {
    const timer = setInterval(() => {
      fetchCases(
        pageNumber,
        pageSize,
        status,
        severity,
        disposition,
        searchTerm,
        deadlines,
        true
      );
    }, AGEING_REFRESH_MS);
    return () => clearInterval(timer);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [pageNumber, pageSize, status, severity, disposition, searchTerm, deadlines]);

  useEffect(() => {
    const params = new URLSearchParams();
    params.set("pageNumber", pageNumber.toString());
    params.set("pageSize", pageSize.toString());
    if (status.length) {
      params.set("status", status.toString());
    }
    if (severity !== ANY_SEVERITY) {
      params.set("severity", severity);
    }
    if (disposition !== ANY_DISPOSITION) {
      params.set("disposition", disposition);
    }
    if (searchTerm.length) {
      params.set("friendlyName", searchTerm.toString());
    }
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
    deadlines,
    router,
  ]);

  const activeTab = status;

  return (
    <div className="space-y-4">
      <PageHeader
        eyebrow={t("cases.mine.eyebrow")}
        title={t("cases.mine.title")}
        description={t("cases.mine.description")}
      >
        <Button variant="outline" size="sm" className="gap-1.5" asChild>
          <Link href="/approvals/mine">
            <Send aria-hidden className="size-4" />
            {t("cases.mine.myRequests")}
          </Link>
        </Button>
      </PageHeader>

      <TabBar
        tabs={statuses}
        activeTab={activeTab}
        label={caseStatusLabel}
        onChange={(tab) => {
          setStatus(tab);
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
              <SelectItem value={ANY_DISPOSITION}>{t("myCases.anyDisposition")}</SelectItem>
              {DISPOSITIONS.map((d) => (
                <SelectItem key={d} value={d}>{t(dispositionLabelKey(d))}</SelectItem>
              ))}
            </SelectContent>
          </Select>
          <DeadlineFilters
            value={deadlines}
            onChange={(next) => { setDeadlines(next); setPageNumber(1); }}
          />
        </div>
        <Select
          value={String(pageSize)}
          onValueChange={(v) => { setPageSize(parseInt(v)); setPageNumber(1); }}
        >
          <SelectTrigger className="w-36">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            {[10, 25, 50, 100].map((s) => (
              <SelectItem key={s} value={String(s)}>{t("common.perPage", s)}</SelectItem>
            ))}
          </SelectContent>
        </Select>
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
              <TableHead className="w-48">
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
                      {isCaseInProgress(_case) && (
                        <>
                          <Button
                            variant="outline"
                            size="sm"
                            className="gap-1.5 text-inflight-700 hover:text-inflight-700"
                            onClick={() => setClosing(_case)}
                          >
                            <Gavel aria-hidden className="size-4" />
                            {t("cases.mine.resolve")}
                            <span className="sr-only">
                              {" "}
                              — {_case.friendlyName}
                            </span>
                          </Button>
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
                        </>
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

      {closing && (
        <CloseCaseDialog
          open
          onOpenChange={(open) => !open && setClosing(null)}
          organizationId={closing.organizationId}
          submitting={resolving}
          contextLabel={closing.friendlyName}
          onSubmit={(close) => handleResolveCase(closing.caseId, close)}
        />
      )}
    </div>
  );
};

export default Cases;
