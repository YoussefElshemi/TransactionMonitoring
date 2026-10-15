"use client";

import { useCallback, useMemo, useState, useEffect } from "react";
import { useRouter, useParams, useSearchParams, usePathname } from "next/navigation";
import { ApprovalResource, CaseNote, CaseNoteResource, CaseResource, CaseStatus, Paged, ProblemDetails, ValidationProblemDetails } from "@/types";
import { toast } from "sonner";
import Pagination from "@/components/Pagination";
import { NotFound } from "@/components/NotFound";
import { StatusBadge } from "@/components/StatusBadge";
import { SeverityBadge } from "@/components/SeverityBadge";
import { DispositionBadge } from "@/components/DispositionBadge";
import { CaseClose, CloseCaseDialog } from "@/components/cases/CloseCaseDialog";
import { CaseStatusDialog } from "@/components/cases/CaseStatusDialog";
import { ReopenCaseDialog } from "@/components/cases/ReopenCaseDialog";
import { CaseEvidence } from "@/components/evidence";
import { AttachmentsPanel } from "@/components/attachments";
import { EvidencePackButton } from "@/components/cases/EvidencePackButton";
import { CaseLinksPanel } from "@/components/caseLinks";
import { PartyCaseHistory } from "@/components/parties/PartyCaseHistory";
import { CaseAlertsTab } from "@/components/alerts/CaseAlertsTab";
import { CasePayments } from "@/components/alerts/CasePayments";
import { RevealablePayload } from "@/components/RevealablePayload";
import { PageHeader } from "@/components/PageHeader";
import { useTranslation } from "@/context/LocalizationContext";
import { Button } from "@/components/ui/button";
import { Textarea } from "@/components/ui/textarea";
import { Input } from "@/components/ui/input";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from "@/components/ui/dialog";
import { TabBar } from "@/components/TabBar";
import { AssignCaseDialog } from "@/components/AssignCaseDialog";
import { QueueRef } from "@/components/queues/QueueRef";
import { CaseDeadlinePanel } from "@/components/cases/CaseDeadlinePanel";
import { CaseApprovalBanner } from "@/components/approvals/CaseApprovalBanner";
import { OpenFilingAction } from "@/components/suspiciousActivityReports/OpenFilingAction";
import { EscalatedMark } from "@/components/cases/CaseAgeing";
import { HistoryDialog, HistoryPanel, historyPath } from "@/components/history";
import { useAuth } from "@/context/AuthContext";
import { usePermissions } from "@/context/PermissionsContext";
import { UserPlus, UserMinus, Plus, Trash2, History, MoveRight, RotateCcw, Gavel } from "lucide-react";
import { loginHref } from "@/lib/authRedirect";
import { API_BASE, reportApiError, unwrap } from "@/lib/api";
import {
  allowedTransitionsFrom,
  caseStatusLabel,
  isCaseInProgress,
  isTerminalCase,
} from "@/lib/cases";
import { openAlerts, worstAlertSeverity } from "@/lib/alerts";
import { partyName } from "@/lib/parties";
import { useParty } from "@/lib/useParties";
import { relationText, rulesById, shortId, userName } from "@/lib/resources";
import { approvalHref } from "@/lib/approvals";
import { useCaseApproval } from "@/lib/useApprovals";

import { apiFetch } from "@/lib/http";

import { LocalTime } from "@/components/LocalTime";

const CaseDetails = () => {
  const t = useTranslation();
  const { id } = useParams();
  const pathname = usePathname();
  const searchParams = useSearchParams();

  const redirectUri = searchParams.toString()
    ? `${pathname}?${searchParams.toString()}`
    : pathname;
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [message, setMessage] = useState("");

  const [caseResource, setCaseResource] = useState<CaseResource | null>(null);
  const _case = caseResource?.case ?? null;
  const caseRules = useMemo(
    () => rulesById(caseResource?.rules),
    [caseResource]
  );
  const transaction = caseResource?.transaction;
  const payload = transaction?.payload;

  const party = useParty(_case?.partyId ?? null);

  const worstSeverity = worstAlertSeverity(_case?.alerts ?? []);
  const stillOpen = openAlerts(_case?.alerts ?? []);
  const [caseNotes, setCaseNotes] = useState<CaseNoteResource[]>([]);
  const [pageNumber, setPageNumber] = useState(parseInt(searchParams.get("pageNumber") || "1"));
  const [pageSize, setPageSize] = useState(parseInt(searchParams.get("pageSize") || "10"));
  const [searchTerm, setSearchTerm] = useState(searchParams.get("friendlyName") || "");
  const [totalPages, setTotalPages] = useState(1);
  const [loading, setLoading] = useState(true);
  const [caseNotesLoading, setCaseNotesLoading] = useState(true);
  const [activeTab, setActiveTab] = useState("Alerts");
  const [historyNote, setHistoryNote] = useState<CaseNote | null>(null);
  const [assignDialogOpen, setAssignDialogOpen] = useState(false);
  const [paymentsToken, setPaymentsToken] = useState(0);
  const [assigning, setAssigning] = useState(false);
  const [closeDialogOpen, setCloseDialogOpen] = useState(false);
  const [statusDialogOpen, setStatusDialogOpen] = useState(false);
  const [reopenDialogOpen, setReopenDialogOpen] = useState(false);
  const [resolving, setResolving] = useState(false);
  const router = useRouter();
  const { user } = useAuth();
  const { holdsOver } = usePermissions();

  const caseScope = { queueId: _case?.queueId ?? null };
  const canAssignOthers = holdsOver("cases:assign", caseScope);
  const canWorkCase = holdsOver("cases:write", caseScope);
  const canConfigureQueues = holdsOver("queues:read", caseScope);

  const goToLogin = useCallback(() => {
    router.push(loginHref(redirectUri));
  }, [router, redirectUri]);

  const fetchCaseById = async (id: string) => {
    setLoading(true);

    try {
      const response = await apiFetch(`${API_BASE}/cases/${id}`, {
        method: "GET",
        headers: {
          "Content-Type": "application/json"
        },
        credentials: "include"
      });

      if (response.ok) {
        setCaseResource(await unwrap<CaseResource>(response));
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
          errorData.detail || t("cases.errorOccurredFetchingCase"),
          { id: "error-fetching-case" }
        );
      }
    } catch {
      toast.error(t("cases.errorOccurredFetchingCase"), {
        id: "error-fetching-case"
      });
    } finally {
      setLoading(false);
    }
  };

  const fetchCaseNotes = async (
    caseId: string,
    pageNumber: number,
    pageSize: number,
    searchTerm: string
  ) => {
    setCaseNotesLoading(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/cases/${caseId}/notes?PageNumber=${pageNumber}&PageSize=${pageSize}&Message=${searchTerm}`,
        {
          method: "GET",
          headers: {
            "Content-Type": "application/json"
          },
          credentials: "include"
        }
      );

      if (response.ok) {
        const data = (await response.json()) as Paged<CaseNoteResource>;
        setCaseNotes(data.data);
        setTotalPages(data.metadata.pageCount);
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
          errorData.detail || t("cases.errorOccurredFetchingCaseNotes"),
          { id: "error-fetching-case-notes" }
        );
      }
    } catch {
      toast.error(t("cases.errorOccurredFetchingCaseNotes"), {
        id: "error-fetching-case-notes"
      });
    } finally {
      setCaseNotesLoading(false);
    }
  };

  const { approval: pendingApproval, refresh: refreshApproval } =
    useCaseApproval(_case?.caseId, () => {
      if (!_case) return;
      fetchCaseById(_case.caseId);
      fetchCaseNotes(_case.caseId, pageNumber, pageSize, searchTerm);
      setPaymentsToken((token) => token + 1);
    });

  const handleCreateCaseNote = async (caseId: string) => {
    try {
      const response = await apiFetch(
        `${API_BASE}/cases/${caseId}/notes`,
        {
          method: "POST",
          headers: {
            "Content-Type": "application/json"
          },
          body: JSON.stringify({
            message
          }),
          credentials: "include"
        }
      );

      if (response.ok) {
        toast.success(t("cases.successfullyCreatedCaseNote"));
        fetchCaseNotes(caseId, pageNumber, pageSize, searchTerm);
        setIsModalOpen(false);
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
        toast.error(errorData.detail || t("cases.failedCreateCaseNote"));
      }
    } catch {
      toast.error(t("cases.failedCreateCaseNote"));
    }
  };

  const handleAssignCase = async (caseId: string, userId?: string) => {
    setAssigning(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/cases/${caseId}/assign`,
        {
          method: "POST",
          headers: {
            "Content-Type": "application/json"
          },
          credentials: "include",
          body: JSON.stringify(userId ? { userId } : {})
        }
      );

      if (response.ok) {
        toast.success(
          userId ? t("cases.successfullyAssignedCase") : t("cases.caseAssignedYou")
        );
        setAssignDialogOpen(false);
        fetchCaseById(caseId);
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
            t("cases.assigningCaseSomebodyElseNeeds"),
          {
            description: t("cases.youCanStillAssignCase"),
            action: {
              label: t("cases.action.assignToMe"),
              onClick: () => handleAssignCase(caseId)
            }
          }
        );
      } else {
        const errorData = (await response.json()) as ProblemDetails;
        toast.error(
          errorData.detail || t("cases.errorOccurredAssigningCase")
        );
      }
    } catch {
      toast.error(t("cases.errorOccurredAssigningCase"));
    } finally {
      setAssigning(false);
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
        fetchCaseById(caseId);
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
        setCloseDialogOpen(false);
        toast.success(t("cases.sentApproval"), {
          description:
            t("cases.caseStillOpenStillHolding"),
          action: {
            label: t("rules.openRequest"),
            onClick: () => router.push(approvalHref(approval.approvalId))
          }
        });
        refreshApproval();
      } else if (response.ok) {
        toast.success(t("cases.successfullyResolvedCase"));
        setCloseDialogOpen(false);
        fetchCaseById(caseId);
        fetchCaseNotes(caseId, pageNumber, pageSize, searchTerm);
        setPaymentsToken((token) => token + 1);
      } else if (response.status === 409) {
        const errorData = (await response.json()) as ProblemDetails;
        setCloseDialogOpen(false);
        toast.error(
          errorData.detail || t("cases.caseAlreadyAwaitingApproval")
        );
        refreshApproval();
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

  const handleChangeCaseStatus = async (
    caseId: string,
    status: CaseStatus,
    note: string | null
  ) => {
    setResolving(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/cases/${caseId}/status`,
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
          body: JSON.stringify({ status, note })
        }
      );

      if (response.ok) {
        toast.success(t("cases.caseMovedTo", caseStatusLabel(status)));
        setStatusDialogOpen(false);
        fetchCaseById(caseId);
        if (note) fetchCaseNotes(caseId, pageNumber, pageSize, searchTerm);
      } else {
        await reportApiError(response, {
          fallback: t("cases.errorOccurredMovingCase"),
          onUnauthorized: goToLogin
        });
      }
    } catch {
      toast.error(t("cases.errorOccurredMovingCase"));
    } finally {
      setResolving(false);
    }
  };

  const handleReopenCase = async (caseId: string, reason: string) => {
    setResolving(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/cases/${caseId}/reopen`,
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
          body: JSON.stringify({ reason })
        }
      );

      if (response.ok) {
        toast.success(t("cases.caseReopened"), {
          description: t("cases.paymentsUnderWereNotTouched")
        });
        setReopenDialogOpen(false);
        fetchCaseById(caseId);
      } else {
        await reportApiError(response, {
          fallback: t("cases.errorOccurredReopeningCase"),
          onUnauthorized: goToLogin
        });
      }
    } catch {
      toast.error(t("cases.errorOccurredReopeningCase"));
    } finally {
      setResolving(false);
    }
  };

  const deleteCaseNote = async (caseId: string, noteId: string) => {
    try {
      const response = await apiFetch(
        `${API_BASE}/cases/${caseId}/notes/${noteId}`,
        {
          method: "DELETE",
          credentials: "include"
        }
      );

      if (response.ok) {
        toast.success(t("cases.successfullyDeletedCaseNote"), {
          action: { label: t("common.undo"), onClick: () => recoverCaseNote(caseId, noteId) },
        });
        fetchCaseNotes(caseId, pageNumber, pageSize, searchTerm);
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
        toast.error(errorData.detail || t("cases.failedDeleteCaseNote"));
      }
    } catch {
      toast.error(t("cases.failedDeleteCaseNote"));
    }
  };

  const recoverCaseNote = async (caseId: string, noteId: string) => {
    try {
      const response = await apiFetch(
        `${API_BASE}/cases/${caseId}/notes/${noteId}/recover`,
        {
          method: "POST",
          credentials: "include"
        }
      );

      if (response.ok) {
        toast.success(t("cases.successfullyRecoveredCaseNote"));
        fetchCaseNotes(caseId, pageNumber, pageSize, searchTerm);
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
        toast.error(errorData.detail || t("cases.failedDeleteCaseNote"));
      }
    } catch {
      toast.error(t("cases.failedDeleteCaseNote"));
    }
  };

  useEffect(() => { fetchCaseById(id as string); }, [id]);
  useEffect(() => { fetchCaseNotes(id as string, pageNumber, pageSize, searchTerm); }, [id, pageNumber, pageSize, searchTerm]);
  useEffect(() => {
    const params = new URLSearchParams();
    params.set("pageNumber", pageNumber.toString());
    params.set("pageSize", pageSize.toString());
    if (searchTerm.length) params.set("friendlyName", searchTerm);
    router.replace(`?${params.toString()}`);
  }, [pageNumber, pageSize, searchTerm, router]);

  if (loading) {
    return (
      <div className="space-y-4">
        <Skeleton className="h-8 w-48" />
        <Card><CardContent className="pt-6 space-y-2">
          {Array.from({ length: 5 }).map((_, i) => <Skeleton key={i} className="h-4 w-full" />)}
        </CardContent></Card>
      </div>
    );
  }

  if (!_case) return <NotFound
        title={t("cases.caseNotFound")}
        description={t("common.notFound.description")}
        backHref="/cases"
        backLabel={t("cases.allCases")}
      />;

  return (
    <div className="space-y-6">
      <PageHeader eyebrow={t("cases.detail.eyebrow")} title={_case.friendlyName}>
        {_case.status === "Pending" &&
          (canAssignOthers ? (
            <Button
              variant="outline"
              size="sm"
              className="gap-1.5 text-inflight-700 hover:text-inflight-700"
              onClick={() => setAssignDialogOpen(true)}
              disabled={assigning}
            >
              <UserPlus aria-hidden className="size-4" />
              {t("cases.detail.assign")}
            </Button>
          ) : canWorkCase ? (
            <Button
              variant="outline"
              size="sm"
              className="gap-1.5 text-inflight-700 hover:text-inflight-700"
              onClick={() => handleAssignCase(_case.caseId)}
              disabled={assigning}
            >
              <UserPlus aria-hidden className="size-4" />
              {assigning
                ? t("cases.detail.assigning")
                : t("cases.action.assignToMe")}
            </Button>
          ) : null)}
        {isCaseInProgress(_case) && (
          <>
            <Button
              variant="outline"
              size="sm"
              className="gap-1.5 text-inflight-700 hover:text-inflight-700"
              onClick={() => setCloseDialogOpen(true)}
              disabled={pendingApproval !== null}
              title={
                pendingApproval ? t("cases.detail.resolveBlocked") : undefined
              }
            >
              <Gavel aria-hidden className="size-4" />
              {t("cases.detail.resolve")}
            </Button>
            {allowedTransitionsFrom(_case.status).length > 0 && (
              <Button
                variant="outline"
                size="sm"
                className="gap-1.5 text-inflight-700 hover:text-inflight-700"
                onClick={() => setStatusDialogOpen(true)}
              >
                <MoveRight aria-hidden className="size-4" />
                {t("cases.detail.move")}
              </Button>
            )}
            <Button
              variant="outline"
              size="sm"
              className="gap-1.5 text-slate-600"
              onClick={() => handleUnassignCase(_case.caseId)}
            >
              <UserMinus aria-hidden className="size-4" />
              {t("cases.detail.unassign")}
            </Button>
          </>
        )}
        <OpenFilingAction caseId={_case.caseId} />
        {isTerminalCase(_case.status) && (
          <Button
            variant="outline"
            size="sm"
            className="gap-1.5 text-inflight-700 hover:text-inflight-700"
            onClick={() => setReopenDialogOpen(true)}
          >
            <RotateCcw aria-hidden className="size-4" />
            {t("cases.detail.reopen")}
          </Button>
        )}
      </PageHeader>

      {pendingApproval && (
        <CaseApprovalBanner
          resource={pendingApproval}
          viewerUserId={user?.userId}
        />
      )}

      <Card>
        <CardHeader>
          <CardTitle className="flex flex-wrap items-center gap-3 text-base font-semibold">
            {t("common.details")}
            <StatusBadge status={_case.status} />
            <SeverityBadge severity={_case.severity} emphasis />
            {worstSeverity && worstSeverity !== _case.severity && (
              <span className="flex items-center gap-1.5">
                <span className="font-mono text-[11px] uppercase tracking-widest text-slate-400">
                  worst since
                </span>
                <SeverityBadge severity={worstSeverity} emphasis />
              </span>
            )}
            {_case.disposition && (
              <span className="flex items-center gap-1.5 border-l border-slate-200 pl-3">
                <span className="font-mono text-[11px] uppercase tracking-widest text-slate-400">
                  concluded
                </span>
                <DispositionBadge disposition={_case.disposition} />
              </span>
            )}
            {_case.escalatedAt && (
              <EscalatedMark escalatedAt={_case.escalatedAt} />
            )}
          </CardTitle>
        </CardHeader>
        <CardContent className="grid grid-cols-2 gap-x-8 gap-y-3 text-sm">
          {_case.status === "Expired" && stillOpen.length > 0 && (
            <div className="col-span-2 rounded-md border border-attention-200 bg-attention-50 px-3 py-2">
              <p className="text-sm font-medium text-attention-700">
                This case expired with {stillOpen.length} alert
                {stillOpen.length === 1 ? "" : "s"} still open.
              </p>
              <p className="mt-0.5 text-xs text-attention-700">
                An expired case cannot be resolved as it stands. Either settle
                what is still held payment by payment in the panel below, or
                reopen the case and work it properly — nobody ever judged the
                finding in it.
              </p>
            </div>
          )}
          <div>
            <span className="font-medium text-slate-500">{t("common.created")}</span>
            <p className="mt-0.5 text-slate-900"><LocalTime value={_case.createdAt} /></p>
          </div>
          <div>
            <span className="font-medium text-slate-500">{t("common.updated")}</span>
            <p className="mt-0.5 text-slate-900"><LocalTime value={_case.updatedAt} /></p>
          </div>
          <div className="min-w-0">
            <span className="font-medium text-slate-500">{t("cases.subject")}</span>
            {_case.partyId ? (
              <p className="mt-0.5">
                <a
                  href={`/parties/${_case.partyId}`}
                  className="text-brand-800 hover:underline"
                >
                  {party ? partyName(party) : shortId(_case.partyId)}
                </a>
              </p>
            ) : (
              <p className="mt-0.5 text-slate-500">
                Nobody — this organisation resolves no parties, so this case
                covers one payment.
              </p>
            )}
          </div>
          <div className="min-w-0">
            <span className="font-medium text-slate-500">{t("cases.queue")}</span>
            <p className="mt-0.5">
              <QueueRef
                queueId={_case.queueId}
                queueName={_case.queueName}
                href={
                  canConfigureQueues
                    ? (queueId) => `/queues/manage/${queueId}`
                    : undefined
                }
              />
            </p>
            {!_case.queueId && (
              <p className="mt-0.5 text-xs text-slate-500">
                No queue&apos;s criteria matched it, so nobody&apos;s desk
                claimed it. Assign it by hand.
              </p>
            )}
          </div>
          {_case.transactionId && (
            <div>
              <span className="font-medium text-slate-500">
                {t("cases.openedOver")}
              </span>
              <p className="mt-0.5">
                <a
                  href={`/transactions/${_case.transactionId}`}
                  className="text-brand-800 hover:underline"
                >
                  {t("cases.theTransactionThatOpenedThis")}
                </a>
              </p>
            </div>
          )}
          {_case.assignedToUserId || _case.assignedAt ? (
            <>
              <div>
                <span className="font-medium text-slate-500">{t("cases.assignedTo")}</span>
                <p className="mt-0.5 text-slate-900">
                  {relationText(
                    userName(caseResource?.assignedToUser),
                    _case.assignedToUserId
                  )}
                </p>
              </div>
              <div>
                <span className="font-medium text-slate-500">{t("cases.assignedAt")}</span>
                <p className="mt-0.5 text-slate-900">
                  {_case.assignedAt
                    ? <LocalTime value={_case.assignedAt} />
                    : "—"}
                </p>
              </div>
            </>
          ) : (
            _case.status === "Expired" && (
              <div className="col-span-2">
                <span className="font-medium text-slate-500">{t("cases.assignedTo")}</span>
                <p className="mt-0.5 text-slate-500">
                  {t("cases.nobodyThisCaseExpiredBefore")}
                </p>
              </div>
            )
          )}
          {isTerminalCase(_case.status) && (
            <div>
              <span className="font-medium text-slate-500">
                {_case.status === "Expired" ? "Expired At" : "Resolved At"}
              </span>
              <p className="mt-0.5 text-slate-900"><LocalTime value={_case.resolvedAt!} /></p>
            </div>
          )}
          {_case.reasonCode && (
            <div className="min-w-0">
              <span className="font-medium text-slate-500">{t("cases.reasonCode")}</span>
              <p className="mt-0.5 text-slate-900">
                <span className="font-mono text-xs">{_case.reasonCode.code}</span>
                <span className="text-slate-500">
                  {" "}
                  — {_case.reasonCode.description}
                </span>
                {!_case.reasonCode.isEnabled && (
                  <span className="ml-1.5 font-mono text-[11px] uppercase tracking-widest text-slate-400">
                    retired
                  </span>
                )}
              </p>
            </div>
          )}
          {_case.reopenedAt && (
            <div className="col-span-2 rounded-md border border-inflight-200 bg-inflight-50 px-3 py-2">
              <p className="text-sm font-medium text-inflight-700">
                {t("cases.reopened")} <LocalTime value={_case.reopenedAt} />.
              </p>
              <p className="mt-0.5 text-xs text-inflight-700">
                No payment was touched by that — everything under this case had
                already been released or blocked. The earlier conclusion, and
                who reached it, are in the History tab below.
              </p>
            </div>
          )}
        </CardContent>
      </Card>

      <CaseDeadlinePanel subject={_case} ageing={caseResource!.ageing} />

      <CasePayments
        key={paymentsToken}
        caseId={_case.caseId}
        originatingTransactionId={_case.transactionId}
        organizationId={_case.organizationId}
        onResolved={() => fetchCaseById(_case.caseId)}
        onUnauthorized={goToLogin}
      />

      {_case.ruleEvaluations.length > 0 && (
        <CaseEvidence
          ruleEvaluations={_case.ruleEvaluations}
          payload={payload}
          rules={caseRules}
        />
      )}

      {transaction && payload && (
        <div>
          <h2 className="mb-3 font-mono text-sm font-medium tracking-tight text-slate-900">
            {t("cases.detail.payload")}
          </h2>
          <RevealablePayload
            transactionId={transaction.transactionId}
            payload={payload}
            masked={transaction.payloadMasked}
            onUnauthorized={goToLogin}
          />
        </div>
      )}

      <div className="space-y-4">
        <TabBar
          tabs={[
            "Alerts",
            "Evidence",
            "Related cases",
            ...(_case.partyId ? ["Party history"] : []),
            "Notes",
            "History",
          ]}
          activeTab={activeTab}
          onChange={setActiveTab}
          actions={
            activeTab === "Notes" ? (
              <Button
                size="sm"
                className="gap-1.5"
                onClick={() => { setIsModalOpen(true); setMessage(""); }}
              >
                <Plus className="size-4" />
                {t("cases.addNote")}
              </Button>
            ) : activeTab === "Evidence" ? (
              <EvidencePackButton
                caseId={_case.caseId}
                caseFriendlyName={_case.friendlyName}
                onUnauthorized={goToLogin}
              />
            ) : null
          }
        />

        {activeTab === "Alerts" ? (
          <CaseAlertsTab caseId={_case.caseId} onUnauthorized={goToLogin} />
        ) : activeTab === "Evidence" ? (
          <AttachmentsPanel caseId={_case.caseId} onUnauthorized={goToLogin} />
        ) : activeTab === "Related cases" ? (
          <CaseLinksPanel caseId={_case.caseId} onUnauthorized={goToLogin} />
        ) : activeTab === "Party history" && _case.partyId ? (
          <PartyCaseHistory
            partyId={_case.partyId}
            excludeCaseId={_case.caseId}
            onUnauthorized={goToLogin}
          />
        ) : activeTab === "History" ? (
          <HistoryPanel
            entityType="Case"
            path={historyPath.Case(_case.caseId)}
            entityId={_case.caseId}
            onUnauthorized={goToLogin}
          />
        ) : (
          <>
          <div className="flex items-center justify-between gap-3">
            <Input
              value={searchTerm}
              onChange={(e) => { setSearchTerm(e.target.value); setPageNumber(1); }}
              placeholder={t("cases.notes.search")}
              aria-label={t("cases.notes.search")}
              className="w-64"
            />
            <Select value={String(pageSize)} onValueChange={(v) => { setPageSize(parseInt(v)); setPageNumber(1); }}>
              <SelectTrigger className="w-36"><SelectValue /></SelectTrigger>
              <SelectContent>
                {[10, 25, 50, 100].map((s) => (
                  <SelectItem key={s} value={String(s)}>{t("common.perPage", s)}</SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          <div className="overflow-hidden rounded-xl border border-slate-200 bg-card">
            {caseNotesLoading ? (
              <div className="p-4 space-y-3">
                {Array.from({ length: 3 }).map((_, i) => <Skeleton key={i} className="h-16 w-full" />)}
              </div>
            ) : caseNotes.length === 0 ? (
              <p className="py-12 text-center text-sm text-slate-500">{t("cases.notes.empty")}</p>
            ) : (
              <div className="divide-y divide-slate-100">
                {caseNotes.map(({ caseNote: note, createdByUser }) => (
                  <div key={note.caseNoteId} className="flex items-start justify-between gap-4 p-4">
                    <div className="flex-1 min-w-0">
                      <p className="text-sm text-slate-700 break-words">{note.message}</p>
                      <p className="mt-1 text-xs text-slate-400">
                        {relationText(
                          userName(createdByUser),
                          note.createdByUserId
                        )}{" "}
                        · <LocalTime value={note.createdAt} />
                      </p>
                    </div>
                    <div className="flex flex-shrink-0 items-center gap-1">
                      <Button
                        variant="ghost"
                        size="icon"
                        className="h-8 w-8 text-slate-400 hover:text-slate-700"
                        title={t("cases.notes.viewHistory")}
                        aria-label={t("cases.notes.viewHistory")}
                        onClick={() => setHistoryNote(note)}
                      >
                        <History aria-hidden className="size-4" />
                      </Button>
                      <Button
                        variant="ghost"
                        size="icon"
                        className="h-8 w-8 text-critical-500 hover:text-critical-700"
                        title={t("cases.notes.delete")}
                        aria-label={t("cases.notes.delete")}
                        onClick={() => deleteCaseNote(_case.caseId, note.caseNoteId)}
                      >
                        <Trash2 aria-hidden className="size-4" />
                      </Button>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>

          {caseNotes.length > 0 && (
            <Pagination
              handlePrevPage={() => setPageNumber((p) => Math.max(1, p - 1))}
              pageNumber={pageNumber}
              totalPages={totalPages}
              handleNextPage={() => setPageNumber((p) => Math.min(totalPages, p + 1))}
            />
          )}
          </>
        )}
      </div>

      <AssignCaseDialog
        open={assignDialogOpen}
        onOpenChange={setAssignDialogOpen}
        assigning={assigning}
        onAssign={(userId) => handleAssignCase(_case.caseId, userId)}
      />

      <CloseCaseDialog
        open={closeDialogOpen}
        onOpenChange={setCloseDialogOpen}
        organizationId={_case.organizationId}
        submitting={resolving}
        onSubmit={(close) => handleResolveCase(_case.caseId, close)}
      />

      <CaseStatusDialog
        open={statusDialogOpen}
        onOpenChange={setStatusDialogOpen}
        currentStatus={_case.status}
        submitting={resolving}
        onSubmit={(status, note) =>
          handleChangeCaseStatus(_case.caseId, status, note)
        }
      />

      <ReopenCaseDialog
        open={reopenDialogOpen}
        onOpenChange={setReopenDialogOpen}
        submitting={resolving}
        onSubmit={(reason) => handleReopenCase(_case.caseId, reason)}
      />

      <HistoryDialog
        open={historyNote !== null}
        onOpenChange={(open) => !open && setHistoryNote(null)}
        title={t("cases.thisNote")}
        entityType="CaseNote"
        path={historyPath.CaseNote(
          _case.caseId,
          historyNote?.caseNoteId ?? ""
        )}
        entityId={historyNote?.caseNoteId}
        onUnauthorized={goToLogin}
      />

      <Dialog open={isModalOpen} onOpenChange={setIsModalOpen}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>{t("cases.addNote")}</DialogTitle>
          </DialogHeader>
          <Textarea
            placeholder={t("cases.notes.write")}
            aria-label={t("cases.notes.write")}
            value={message}
            onChange={(e) => setMessage(e.target.value)}
            rows={4}
          />
          <DialogFooter>
            <Button variant="outline" onClick={() => setIsModalOpen(false)}>
              {t("common.cancel")}
            </Button>
            <Button onClick={() => handleCreateCaseNote(_case.caseId)}>
              {t("cases.save")}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
};

export default CaseDetails;
