"use client";

import {
  BackTest,
  BackTestResource,
  ListRuleVersion,
  Paged,
  RuleDeploymentResource,
  RuleEvaluationResource,
  RuleResource,
  RuleSetResource,
  ProblemDetails,
  Rule,
  RuleBodyDto,
  RuleDeployment,
  RuleSet,
  RuleVersion,
  SchemaViolation,
  ValidationProblemDetails
} from "@/types";
import RuleBuilder from "@/components/RuleBuilder";
import { PayloadView } from "@/components/PayloadView";
import { NotFound } from "@/components/NotFound";
import Pagination from "@/components/Pagination";
import {
  useParams,
  usePathname,
  useRouter,
  useSearchParams
} from "next/navigation";
import { useCallback, useEffect, useRef, useState } from "react";
import { toast } from "sonner";
import Link from "next/link";
import { Copy, Trash2, ArrowLeftRight, Eye, Play, Plus, Siren } from "lucide-react";
import { useAuth } from "@/context/AuthContext";
import { useEnvironment } from "@/context/ModeContext";
import { usePermissions } from "@/context/PermissionsContext";
import { PageHeader } from "@/components/PageHeader";
import { useTranslation } from "@/context/LocalizationContext";
import { StatusBadge } from "@/components/StatusBadge";
import { RuleEvidence } from "@/components/evidence";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue
} from "@/components/ui/select";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle
} from "@/components/ui/dialog";
import { TabBar } from "@/components/TabBar";
import { HistoryTab } from "./HistoryTab";
import { DetailsCard } from "./DetailsCard";
import { DeployDialog } from "./DeployDialog";
import { DeploymentProgress } from "./DeploymentProgress";
import { DeploymentsTab } from "./DeploymentsTab";
import { VersionsTab } from "./VersionsTab";
import {
  API_BASE,
  STRATEGY_LABELS,
  isPendingApprovalDeployment,
  reportApiError,
  versionLabel,
} from "./api";
import { RuleApprovalBanner } from "@/components/approvals/RuleApprovalBanner";
import {
  APPROVAL_COUNT_POLL_MS,
  useDeploymentApprovalRequired,
} from "@/lib/useApprovals";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { loginHref } from "@/lib/authRedirect";
import { unwrap } from "@/lib/api";

import { apiFetch } from "@/lib/http";
import { useIdempotencyKey } from "@/lib/useIdempotencyKey";

import { LocalTime } from "@/components/LocalTime";
import { PageSizeSelect } from "@/components/PageSizeSelect";
const DEPLOYMENT_POLL_MS = 5000;

const LIVE_LOGIC = "live";

const RuleDetails = () => {
  const t = useTranslation();
  const [rule, setRule] = useState<Rule | null>(null);
  const [loading, setLoading] = useState<boolean>(true);
  const [ruleSetModalOpen, setRuleSetModalOpen] = useState<boolean>(false);
  const [modalLoading, setModalLoading] = useState<boolean>(false);
  const [modalAction, setModalAction] = useState<"Copy" | "Transfer" | "">(
    ""
  );
  const [ruleSetSearchTerm, setRuleSetSearchTerm] = useState<string>("");
  const [filteredRuleSets, setFilteredRuleSets] = useState<RuleSet[]>([]);
  const [dropdownOpen, setDropdownOpen] = useState<boolean>(false);
  const [selectedRuleSetId, setSelectedRuleSetId] = useState<string>("");
  const [evaluations, setEvaluations] = useState<RuleEvaluationResource[]>([]);
  const [expandedPayloads, setExpandedPayloads] = useState<Set<string>>(
    new Set()
  );
  const [evalPageNumber, setEvalPageNumber] = useState<number>(1);
  const [evalPageSize, setEvalPageSize] = useState<number>(10);
  const [evalTotalPages, setEvalTotalPages] = useState<number>(1);
  const [evalLoading, setEvalLoading] = useState<boolean>(false);
  const [activeTab, setActiveTab] = useState<string>("Versions");
  const tabsRef = useRef<HTMLDivElement>(null);
  const [backTests, setBackTests] = useState<BackTest[]>([]);
  const [backTestPageNumber, setBackTestPageNumber] = useState<number>(1);
  const [backTestTotalPages, setBackTestTotalPages] = useState<number>(1);
  const [backTestLoading, setBackTestLoading] = useState<boolean>(false);
  const [hasActiveBackTest, setHasActiveBackTest] = useState<boolean>(false);
  const [creatingBackTest, setCreatingBackTest] = useState<boolean>(false);
  const [backTestModalOpen, setBackTestModalOpen] = useState<boolean>(false);
  const [sampleSize, setSampleSize] = useState<number>(100);
  const [backTestVersion, setBackTestVersion] = useState<string>(LIVE_LOGIC);

  const [activeDeployment, setActiveDeployment] = useState<RuleDeployment | null>(null);
  const [proposedDeployment, setProposedDeployment] =
    useState<RuleDeployment | null>(null);
  const [liveVersionId, setLiveVersionId] = useState<string | null>(null);
  const [versionIndex, setVersionIndex] = useState<Record<string, number>>({});
  const [recentVersions, setRecentVersions] = useState<RuleVersion[]>([]);
  const [versionsKey, setVersionsKey] = useState<number>(0);
  const [deploymentsKey, setDeploymentsKey] = useState<number>(0);
  const [deployTarget, setDeployTarget] = useState<RuleVersion | null>(null);
  const [versionEditorSeed, setVersionEditorSeed] = useState<RuleBodyDto | null>(null);
  const [versionViolations, setVersionViolations] = useState<SchemaViolation[]>(
    []
  );

  const router = useRouter();
  const { ruleId } = useParams();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const { user } = useAuth();
  const { mode } = useEnvironment();
  const { holds, holdsAll, holdsOver } = usePermissions();

  const ruleScope = { ruleSetId: rule?.ruleSetId ?? null };
  const canWriteRule = holdsOver("rules:write", ruleScope);
  const canDeployRule = holdsOver("rules:deploy", ruleScope);
  const canBackTest = holdsOver("backtests:write", ruleScope);
  const canReadHistory = holdsAll("rules:read", "audit-logs:read");

  const redirectUri = searchParams.toString()
    ? `${pathname}?${searchParams.toString()}`
    : pathname;

  const goToLogin = useCallback(() => {
    router.push(loginHref(redirectUri));
  }, [router, redirectUri]);

  const togglePayload = (id: string) => {
    setExpandedPayloads((previous) => {
      const next = new Set(previous);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  };


  const fetchBackTests = async (pageNumber: number) => {
    setBackTestLoading(true);
    try {
      const response = await apiFetch(
        `${API_BASE}/backTests?ruleId=${ruleId}&pageNumber=${pageNumber}&pageSize=10`,
        { credentials: "include" }
      );
      if (response.ok) {
        const data = (await response.json()) as Paged<BackTestResource>;
        setBackTests(data.data.map((resource) => resource.backTest));
        setBackTestTotalPages(data.metadata.pageCount);
        setHasActiveBackTest(
          data.data.some(
            ({ backTest: bt }) =>
              bt.status === "Pending" || bt.status === "Running"
          )
        );
      } else if (response.status === 401) {
        toast.error(t("common.pleaseLoginAccessResource"), { id: "unauthorized" });
        return router.push(loginHref(redirectUri));
      } else {
        const errorData = (await response.json()) as ProblemDetails;
        toast.error(errorData.detail || t("rules.failedLoadBacktests"), {
          id: "error-backtests",
        });
      }
    } catch {
      toast.error(t("rules.failedLoadBacktests"), { id: "error-backtests" });
    } finally {
      setBackTestLoading(false);
    }
  };

  const openBackTestModal = () => {
    setBackTestVersion(LIVE_LOGIC);
    setBackTestModalOpen(true);
  };

  const backTestSubmission = useIdempotencyKey();

  const createBackTest = async () => {
    setCreatingBackTest(true);
    try {
      const payload: { sampleSize: number; versionNumber?: number } = {
        sampleSize,
      };
      if (backTestVersion !== LIVE_LOGIC) {
        payload.versionNumber = Number(backTestVersion);
      }

      const response = await apiFetch(
        `${API_BASE}/rules/${ruleId}/backTests`,
        {
          method: "POST",
          credentials: "include",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify(payload),
        },
        { idempotencyKey: backTestSubmission.current() }
      );
      if (response.ok) {
        backTestSubmission.completed();
        const data = await unwrap<BackTestResource>(response);
        setBackTestModalOpen(false);
        router.push(`/back-tests/${data.backTest.backTestId}`);
      } else if (response.status === 409) {
        const conflict = (await response.json()) as ProblemDetails & {
          title?: string;
        };

        if (conflict.title?.toLowerCase().includes("x-idempotency-key")) {
          toast.error(t("rules.backtestAlreadySubmitted"), {
            description:
              t("rules.earlierAttemptWentThroughReload"),
          });
        } else {
          toast.error(t("rules.backtestAlreadyRunningRule"));
          setHasActiveBackTest(true);
        }
      } else if (response.status === 401) {
        toast.error(t("common.pleaseLoginPerformAction"));
        return router.push(loginHref(redirectUri));
      } else if (response.status === 403) {
        const errorData = (await response.json()) as ProblemDetails;
        toast.error(
          errorData.detail || t("common.youDoNotHavePermission2")
        );
      } else if (response.status === 402) {
      } else if (response.status === 429) {
        const retry = response.headers.get("Retry-After");
        toast.error(t("common.sandboxEvaluationLimitReached"), { description: retry ? t("common.tryAgainInSeconds", retry) : t("common.shared120RequestsMinuteLimit") });
      } else {
        const errorData = (await response.json()) as ProblemDetails;
        toast.error(errorData.detail || t("rules.failedStartBacktest"));
      }
    } catch {
      toast.error(t("rules.failedStartBacktest"));
    } finally {
      setCreatingBackTest(false);
    }
  };

  const handleRuleSetSelect = (ruleSetId: string) => {
    const ruleSet = filteredRuleSets.find((u) => u.ruleSetId === ruleSetId);
    if (ruleSet) {
      setSelectedRuleSetId(ruleSetId);
      setRuleSetSearchTerm(ruleSet.friendlyName);
    }
    setDropdownOpen(false);
  };

  const handleCloseModal = () => {
    setRuleSetModalOpen(false);
  };

  const handleOpenModal = (action: "Copy" | "Transfer") => {
    setModalAction(action);
    setRuleSetModalOpen(true);
  };

  const handleModalAction = () => {
    if (modalAction === "Transfer") {
      transferRule(selectedRuleSetId);
    } else {
      copyRule(selectedRuleSetId);
    }
  };

  const fetchRuleSets = async (searchTerm: string) => {
    setModalLoading(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/rule-sets?FriendlyName=${searchTerm}`,
        {
          method: "GET",
          headers: {
            "Content-Type": "application/json"
          },
          credentials: "include"
        }
      );

      if (response.ok) {
        const data = (await response.json()) as Paged<RuleSetResource>;
        setFilteredRuleSets(data.data.map((resource) => resource.ruleSet));
      } else if (response.status === 400) {
        const errorData = (await response.json()) as ValidationProblemDetails;
        toast.error(
          Object.keys(errorData.errors)
            .map(
              (key) =>
                `${key}${key.length ? ": " : ""}${errorData.errors[key].join(
                  "\n"
                )}`
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
          errorData.detail || t("common.errorOccurredFetchingRuleSets"),
          { id: "error-fetching-rule-sets" }
        );
      }
    } catch {
      toast.error(t("common.errorOccurredFetchingRuleSets"), {
        id: "error-fetching-rule-sets"
      });
    } finally {
      setModalLoading(false);
    }
  };

  const transferRule = async (targetRuleSetId: string) => {
    const originalRuleSetId = rule!.ruleSetId;
    try {
      const response = await apiFetch(
        `${API_BASE}/rules/${ruleId}/transfer`,
        {
          method: "POST",
          credentials: "include",
          headers: {
            "Content-Type": "application/json"
          },
          body: JSON.stringify({ targetRuleSetId })
        }
      );

      if (response.ok) {
        toast.success(t("rules.successfullyTransferredRule"), {
          action: { label: t("common.undo"), onClick: () => transferRule(originalRuleSetId) },
        });
        router.push(`/rules/${ruleId}`);
      } else if (response.status === 400) {
        const errorData = (await response.json()) as ValidationProblemDetails;
        toast.error(
          Object.keys(errorData.errors)
            .map(
              (key) =>
                `${key}${key.length ? ": " : ""}${errorData.errors[key].join(
                  "\n"
                )}`
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
        toast.error(errorData.detail || t("rules.failedTransferRule"));
      }
    } catch {
      toast.error(t("rules.failedTransferRule"));
    }
  };

  const copyRule = async (targetRuleSetId: string) => {
    try {
      const response = await apiFetch(
        `${API_BASE}/rules/${ruleId}/copy`,
        {
          method: "POST",
          credentials: "include",
          headers: {
            "Content-Type": "application/json"
          },
          body: JSON.stringify({ targetRuleSetId })
        }
      );

      if (response.ok) {
        const data = await unwrap<RuleResource>(response);
        toast.success(t("rules.successfullyCopiedRule"), {
          action: {
            label: t("common.undo"),
            onClick: () => deleteRule(data.rule.ruleId),
          },
        });
        router.push(`/rules/${data.rule.ruleId}`);
      } else if (response.status === 400) {
        const errorData = (await response.json()) as ValidationProblemDetails;
        toast.error(
          Object.keys(errorData.errors)
            .map(
              (key) =>
                `${key}${key.length ? ": " : ""}${errorData.errors[key].join(
                  "\n"
                )}`
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
        toast.error(errorData.detail || t("rules.failedCopyRule"));
      }
    } catch {
      toast.error(t("rules.failedCopyRule"));
    }
  };

  const fetchRuleById = async (silent = false) => {
    if (!silent) {
      setLoading(true);
      setRule(null);
    }

    try {
      const response = await apiFetch(
        `${API_BASE}/rules/${ruleId}`,
        {
          method: "GET",
          headers: {
            "Content-Type": "application/json"
          },
          credentials: "include"
        }
      );

      if (response.ok) {
        const data = await unwrap<RuleResource>(response);
        setRule(data.rule);
      } else if (response.status === 400) {
        const errorData = (await response.json()) as ValidationProblemDetails;
        toast.error(
          Object.keys(errorData.errors)
            .map(
              (key) =>
                `${key}${key.length ? ": " : ""}${errorData.errors[key].join(
                  "\n"
                )}`
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
          errorData.detail || t("rules.errorOccurredFetchingRule"),
          { id: "error-fetching" }
        );
      }
    } catch {
      toast.error(t("rules.errorOccurredFetchingRule"), {
        id: "error-fetching"
      });
    } finally {
      if (!silent) setLoading(false);
    }
  };

  const deleteRule = async (targetRuleId: string = ruleId as string) => {
    const isCurrentRule = targetRuleId === ruleId;
    try {
      const response = await apiFetch(
        `${API_BASE}/rules/${targetRuleId}`,
        {
          method: "DELETE",
          credentials: "include"
        }
      );

      if (response.ok) {
        toast.success(t("common.successfullyDeletedRule"), {
          action: { label: t("common.undo"), onClick: () => recoverRule(targetRuleId) },
        });
        if (isCurrentRule) {
          router.push(`/rule-sets/${rule!.ruleSetId}`);
        }
      } else if (response.status === 400) {
        const errorData = (await response.json()) as ValidationProblemDetails;
        toast.error(
          Object.keys(errorData.errors)
            .map(
              (key) =>
                `${key}${key.length ? ": " : ""}${errorData.errors[key].join(
                  "\n"
                )}`
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
        toast.error(errorData.detail || t("common.failedDeleteRule"));
      }
    } catch {
      toast.error(t("common.failedDeleteRule"));
    }
  };

  const recoverRule = async (targetRuleId: string = ruleId as string) => {
    try {
      const response = await apiFetch(
        `${API_BASE}/rules/${targetRuleId}/recover`,
        {
          method: "POST",
          credentials: "include"
        }
      );

      if (response.ok) {
        toast.success(t("common.successfullyRecoveredRule"));
        router.push(`/rules/${targetRuleId}`);
      } else if (response.status === 400) {
        const errorData = (await response.json()) as ValidationProblemDetails;
        toast.error(
          Object.keys(errorData.errors)
            .map(
              (key) =>
                `${key}${key.length ? ": " : ""}${errorData.errors[key].join(
                  "\n"
                )}`
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
        toast.error(errorData.detail || t("common.failedRecoverRule"));
      }
    } catch {
      toast.error(t("common.failedRecoverRule"));
    }
  };

  const fetchEvaluations = async (
    ruleId: string,
    pageNumber: number,
    pageSize: number
  ) => {
    setEvalLoading(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/rule-evaluations?ruleId=${ruleId}&PageNumber=${pageNumber}&PageSize=${pageSize}`,
        {
          method: "GET",
          headers: {
            "Content-Type": "application/json"
          },
          credentials: "include"
        }
      );

      if (response.ok) {
        const data = (await response.json()) as Paged<RuleEvaluationResource>;
        setEvaluations(data.data);
        setEvalTotalPages(data.metadata.pageCount);
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
          { id: "forbidden" }
        );
        return router.push("/");
      } else {
        const errorData = (await response.json()) as ProblemDetails;
        toast.error(
          errorData.detail || t("rules.errorOccurredFetchingEvaluations"),
          { id: "error-fetching-evaluations" }
        );
      }
    } catch {
      toast.error(t("rules.errorOccurredFetchingEvaluations"), {
        id: "error-fetching-evaluations"
      });
    } finally {
      setEvalLoading(false);
    }
  };

  const showTab = (tab: string) => {
    setActiveTab(tab);
    tabsRef.current?.scrollIntoView({ behavior: "smooth", block: "start" });
  };

  const indexVersions = useCallback((versions: RuleVersion[]) => {
    setVersionIndex((prev) => {
      const next = { ...prev };
      let changed = false;
      versions.forEach((version) => {
        if (next[version.ruleVersionId] !== version.versionNumber) {
          next[version.ruleVersionId] = version.versionNumber;
          changed = true;
        }
      });
      return changed ? next : prev;
    });
  }, []);

  const fetchDeploymentStatus = useCallback(async () => {
    try {
      const response = await apiFetch(
        `${API_BASE}/rules/${ruleId}/deployments?pageNumber=1&pageSize=20`,
        { credentials: "include" }
      );
      if (!response.ok) return;
      const data = (await response.json()) as Paged<RuleDeploymentResource>;
      const newestFirst = data.data
        .map((resource) => resource.ruleDeployment)
        .sort(
        (a, b) =>
          new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()
      );
      setActiveDeployment(
        newestFirst.find((d) => d.status === "InProgress") ?? null
      );
      setProposedDeployment(
        newestFirst.find((d) => isPendingApprovalDeployment(d)) ?? null
      );
      setLiveVersionId(
        newestFirst.find((d) => d.status === "Completed")
          ?.targetRuleVersionId ?? null
      );
    } catch {
    }
  }, [ruleId]);

  const fetchRecentVersions = useCallback(async () => {
    try {
      const response = await apiFetch(
        `${API_BASE}/rules/${ruleId}/versions?pageNumber=1&pageSize=20`,
        { credentials: "include" }
      );
      if (!response.ok) return;
      const data = (await response.json()) as ListRuleVersion;
      indexVersions(data.data);
      setRecentVersions(
        [...data.data].sort((a, b) => b.versionNumber - a.versionNumber)
      );
    } catch {
    }
  }, [ruleId, indexVersions]);

  const createVersion = async (rules: RuleBodyDto[]) => {
    try {
      // Versions don't inherit the rule's current action/severity/riskWeight
      // — omitting them here would silently reset the new version to
      // Review/Medium/0. Carry the rule's current values through explicitly
      // since this editor only lets the user change the rule body.
      const response = await apiFetch(`${API_BASE}/rules/${ruleId}/versions`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          ruleBody: rules[0],
          action: rule?.action,
          severity: rule?.severity,
          riskWeight: rule?.riskWeight,
        }),
        credentials: "include",
      });

      if (response.ok) {
        const data = await unwrap<RuleVersion>(response);
        indexVersions([data]);
        setVersionViolations([]);
        setVersionEditorSeed(null);
        setVersionsKey((k) => k + 1);
        setActiveTab("Versions");
        toast.success(
          t("rules.createdVersionNotLive", data.versionNumber),
          {
            description: requiresDeploymentApproval
              ? t("rules.deployingRaisesRequestSomebodyOther")
              : undefined,
            action:
              canDeployRule && !deploymentBlocked
                ? {
                    label: requiresDeploymentApproval
                      ? t("rules.submitApproval")
                      : "Deploy",
                    onClick: () => setDeployTarget(data),
                  }
                : undefined,
          }
        );
      } else {
        await reportApiError(response, {
          fallback: t("rules.failedCreateVersion"),
          onUnauthorized: goToLogin,
          onSchemaViolations: (report) =>
            setVersionViolations(report.violations),
        });
      }
    } catch {
      toast.error(t("rules.failedCreateVersion"));
    }
  };

  const handleDeployed = async () => {
    setDeployTarget(null);
    await Promise.all([fetchDeploymentStatus(), fetchRuleById(true)]);
    setDeploymentsKey((k) => k + 1);
  };

  const handleCancelled = async () => {
    await fetchDeploymentStatus();
    setDeploymentsKey((k) => k + 1);
  };

  useEffect(() => {
    fetchRuleById();
  }, [ruleId]);

  useEffect(() => {
    fetchDeploymentStatus();
    fetchRecentVersions();
  }, [fetchDeploymentStatus, fetchRecentVersions]);

  const isProposed = proposedDeployment !== null;
  useEffect(() => {
    if (!isProposed) return;
    const timer = setInterval(() => {
      fetchDeploymentStatus();
      setDeploymentsKey((k) => k + 1);
    }, APPROVAL_COUNT_POLL_MS);
    return () => clearInterval(timer);
  }, [isProposed, fetchDeploymentStatus]);

  // Poll only while something is rolling out. Keyed on the boolean so the
  // interval isn't torn down every time `currentPercentage` ticks up.
  const requiresDeploymentApproval = useDeploymentApprovalRequired(
    user?.organizationId,
    holds("approval-policies:read")
  );

  const isDeploying = activeDeployment !== null;
  const deploymentBlockedReason = activeDeployment
    ? "A deployment is already in progress for this rule"
    : proposedDeployment
      ? "A deployment for this rule is already awaiting approval"
      : null;
  const deploymentBlocked = deploymentBlockedReason !== null;
  useEffect(() => {
    if (!isDeploying) return;
    const timer = setInterval(() => {
      fetchDeploymentStatus();
      setDeploymentsKey((k) => k + 1);
    }, DEPLOYMENT_POLL_MS);
    return () => clearInterval(timer);
  }, [isDeploying, fetchDeploymentStatus]);

  const wasDeploying = useRef<boolean>(false);
  useEffect(() => {
    if (wasDeploying.current && !isDeploying) {
      fetchRuleById(true);
      fetchRecentVersions();
    }
    wasDeploying.current = isDeploying;
  }, [isDeploying]); // eslint-disable-line react-hooks/exhaustive-deps

  useEffect(() => {
    if (ruleSetSearchTerm.trim() !== "") {
      fetchRuleSets(ruleSetSearchTerm);
    } else {
      setFilteredRuleSets([]);
    }
  }, [ruleSetSearchTerm]);

  useEffect(() => {
    if (ruleId) {
      fetchEvaluations(ruleId as string, evalPageNumber, evalPageSize);
    }
  }, [ruleId, evalPageNumber, evalPageSize]);

  useEffect(() => {
    if (ruleId) {
      fetchBackTests(backTestPageNumber);
    }
  }, [ruleId, backTestPageNumber]);

  if (loading) return (
    <div className="space-y-4">
      <Skeleton className="h-8 w-48" />
      <Skeleton className="h-64 w-full" />
    </div>
  );
  if (!rule) return <NotFound
        title={t("rules.ruleNotFound")}
        description={t("common.notFound.description")}
        backHref="/rule-sets"
        backLabel={t("rules.allRuleSets")}
      />;

  const liveVersionNumber =
    liveVersionId !== null ? versionIndex[liveVersionId] ?? null : null;
  const knownVersionNumbers = Object.values(versionIndex);
  const latestVersionNumber = knownVersionNumbers.length
    ? Math.max(...knownVersionNumbers)
    : null;
  const undeployedCount =
    latestVersionNumber !== null && liveVersionNumber !== null
      ? latestVersionNumber - liveVersionNumber
      : 0;

  const selectedBackTestVersion =
    backTestVersion === LIVE_LOGIC ? null : Number(backTestVersion);
  const backTestVersionOptions = recentVersions.map((v) => v.versionNumber);
  if (
    selectedBackTestVersion !== null &&
    !backTestVersionOptions.includes(selectedBackTestVersion)
  ) {
    backTestVersionOptions.push(selectedBackTestVersion);
  }
  backTestVersionOptions.sort((a, b) => b - a);

  return (
    <div className="space-y-6">
      {mode === "test" && <div className="rounded-md border border-attention-300 bg-attention-50 px-3 py-2 text-sm text-attention-900">{t("rules.sandboxBacktestIsolatedTestData")}</div>}
      <PageHeader eyebrow={t("rules.detail.eyebrow")} title={rule.friendlyName}>
        <Button variant="outline" size="sm" className="gap-1.5" asChild>
          <Link href={`/alerts?ruleId=${rule.ruleId}&status=All`}>
            <Siren className="size-4" />
            {t("rules.alerts")}
          </Link>
        </Button>
        {canBackTest && (
          <Button
            variant="outline"
            size="sm"
            className="gap-1.5"
            onClick={() => openBackTestModal()}
            disabled={hasActiveBackTest || creatingBackTest}
          >
            <Play className="size-4" />
            {t("rules.runBacktest")}
          </Button>
        )}
        {canWriteRule && (
          <>
            <Button variant="outline" size="sm" className="gap-1.5" onClick={() => handleOpenModal("Copy")}>
              <Copy className="size-4" />
              {t("rules.copy")}
            </Button>
            <Button variant="outline" size="sm" className="gap-1.5" onClick={() => handleOpenModal("Transfer")}>
              <ArrowLeftRight className="size-4" />
              {t("rules.transfer")}
            </Button>
            <Button
              variant="ghost"
              size="icon"
              className="h-8 w-8 text-critical-500 hover:text-critical-700"
              onClick={() => deleteRule()}
            >
              <Trash2 className="size-4" />
            </Button>
          </>
        )}
      </PageHeader>

      <DetailsCard
        rule={rule}
        canEdit={canWriteRule}
        onSaved={setRule}
        onUnauthorized={goToLogin}
      />

      {proposedDeployment && (
        <RuleApprovalBanner
          deployment={proposedDeployment}
          versionIndex={versionIndex}
          viewerUserId={user?.userId}
        />
      )}

      {activeDeployment && (
        <div className="rounded-lg border border-inflight-200 bg-inflight-50/60 p-4">
          <div className="flex flex-wrap items-center gap-3">
            <StatusBadge status="Deploying" label={t("rules.deploying")} />
            <span className="text-sm text-slate-700">
              {versionLabel(activeDeployment.previousRuleVersionId, versionIndex)}
              <span className="mx-1.5 text-slate-400">→</span>
              <span className="font-medium">
                {versionLabel(activeDeployment.targetRuleVersionId, versionIndex)}
              </span>
              <span className="ml-2 text-slate-500">
                ({STRATEGY_LABELS[activeDeployment.strategy] ??
                  activeDeployment.strategy}
                {activeDeployment.strategy === "Gradual" &&
                  activeDeployment.stepPercentage !== null &&
                  activeDeployment.stepIntervalSeconds !== null &&
                  ` — +${activeDeployment.stepPercentage}% every ${activeDeployment.stepIntervalSeconds}s`}
                )
              </span>
            </span>
            {canDeployRule && (
              <Button
                variant="outline"
                size="sm"
                className="ml-auto"
                onClick={() => showTab("Deployments")}
              >
                {t("rules.manage")}
              </Button>
            )}
          </div>
          <DeploymentProgress
            className="mt-3"
            percentage={activeDeployment.currentPercentage}
          />
          <p className="mt-2 text-xs text-slate-500">
            The live rule keeps its current logic until this rollout reaches
            100%.
          </p>
        </div>
      )}

      <div className="rounded-lg border border-slate-200 bg-card p-4">
        <RuleBuilder
          ruleSetId={rule.ruleSetId}
          initialRules={[rule.ruleBody]}
          submitRuleText=""
          readOnly
          matchPolarity={rule.matchPolarity}
          toolbarLeading={
            <>
              <h2 className="text-sm font-semibold text-slate-900">
                {t("rules.ruleLogic")}
              </h2>
              <StatusBadge
                status="Live"
                label={
                  liveVersionNumber !== null
                    ? `Live · v${liveVersionNumber}`
                    : "Live"
                }
              />
              {undeployedCount > 0 && (
                <StatusBadge
                  status="Undeployed"
                  label={`${undeployedCount} newer version${undeployedCount > 1 ? "s" : ""} not live`}
                />
              )}
            </>
          }
          toolbarActions={
            canWriteRule && (
              <Button
                size="sm"
                className="gap-1.5"
                onClick={() => setVersionEditorSeed(rule.ruleBody)}
              >
                <Plus className="size-4" />
                {t("rules.newVersion")}
              </Button>
            )
          }
        />
      </div>

      <div ref={tabsRef} className="scroll-mt-4 space-y-4">
        <TabBar
          aria-label={t("rules.detail.tabs")}
          tabs={[
            "Versions",
            "Deployments",
            "Evaluation History",
            "Back Tests",
            ...(canReadHistory ? ["History"] : []),
          ]}
          activeTab={activeTab}
          onChange={setActiveTab}
        />

        {activeTab === "Versions" && (
          <VersionsTab
            ruleId={ruleId as string}
            canWrite={canWriteRule}
            canDeploy={canDeployRule}
            liveVersionId={liveVersionId}
            liveVersionNumber={liveVersionNumber}
            deployingVersionId={activeDeployment?.targetRuleVersionId ?? null}
            proposedVersionId={proposedDeployment?.targetRuleVersionId ?? null}
            deploymentInProgress={deploymentBlocked}
            deploymentBlockedReason={deploymentBlockedReason}
            requiresApproval={requiresDeploymentApproval}
            refreshKey={versionsKey}
            onVersionsLoaded={indexVersions}
            onDeploy={setDeployTarget}
            onCreateVersion={(seed) => setVersionEditorSeed(seed ?? rule.ruleBody)}
            onUnauthorized={goToLogin}
          />
        )}

        {activeTab === "Deployments" && (
          <DeploymentsTab
            ruleId={ruleId as string}
            canDeploy={canDeployRule}
            versionIndex={versionIndex}
            refreshKey={deploymentsKey}
            onCancelled={handleCancelled}
            onUnauthorized={goToLogin}
          />
        )}

        {activeTab === "Evaluation History" && (
          <div className="space-y-4">
            <div className="flex items-center justify-end">
              <PageSizeSelect
                value={evalPageSize}
                onChange={(next) => {
                  setEvalPageSize(next);
                  setEvalPageNumber(1);
                }}
              />
            </div>

            {evalLoading ? (
              <div className="space-y-2">
                {Array.from({ length: 3 }).map((_, i) => (
                  <Skeleton key={i} className="h-16 w-full" />
                ))}
              </div>
            ) : evaluations.length === 0 ? (
              <p className="py-8 text-center text-sm text-slate-500">{t("rules.noEvaluationsFound")}</p>
            ) : (
              <div className="space-y-2">
                {evaluations.map(({ ruleEvaluation: ev, transaction }) => (
                  <RuleEvidence
                    key={ev.ruleEvaluationId}
                    evaluation={ev}
                    showRule={false}
                    payload={transaction?.payload}
                    actions={
                      <>
                        <a
                          href={`/transactions/${ev.transactionId}`}
                          className="text-xs text-brand-800 hover:underline"
                        >
                          {t("rules.viewTransaction")}
                        </a>
                        {transaction && (
                          <Button
                            variant="ghost"
                            size="sm"
                            className="h-auto px-0 text-xs text-brand-800 hover:bg-transparent hover:text-brand-900"
                            onClick={() => togglePayload(ev.ruleEvaluationId)}
                          >
                            {expandedPayloads.has(ev.ruleEvaluationId)
                              ? "Hide payload"
                              : "Show payload"}
                          </Button>
                        )}
                      </>
                    }
                  >
                    {expandedPayloads.has(ev.ruleEvaluationId) && transaction && (
                      <div className="mt-3">
                        <PayloadView
                          payload={transaction.payload}
                          masked={transaction.payloadMasked}
                        />
                      </div>
                    )}
                  </RuleEvidence>
                ))}
              </div>
            )}

            {evaluations.length > 0 && (
              <Pagination
                handlePrevPage={() => setEvalPageNumber((p) => Math.max(1, p - 1))}
                pageNumber={evalPageNumber}
                totalPages={evalTotalPages}
                handleNextPage={() => setEvalPageNumber((p) => Math.min(evalTotalPages, p + 1))}
              />
            )}
          </div>
        )}

        {activeTab === "Back Tests" && (
          <div className="space-y-4">
            {backTestLoading ? (
              <div className="space-y-2">
                {Array.from({ length: 3 }).map((_, i) => (
                  <Skeleton key={i} className="h-14 w-full" />
                ))}
              </div>
            ) : backTests.length === 0 ? (
              <p className="py-8 text-center text-sm text-slate-500">{t("rules.noBacktestsRunYet")}</p>
            ) : (
              <>
                <div className="overflow-hidden rounded-xl border border-slate-200 bg-card">
                  <Table>
                    <TableHeader>
                      <TableRow className="bg-slate-50 hover:bg-slate-50">
                        <TableHead>ID</TableHead>
                        <TableHead>{t("common.status")}</TableHead>
                        <TableHead>{t("common.created")}</TableHead>
                        <TableHead className="w-24">{t("common.actions")}</TableHead>
                      </TableRow>
                    </TableHeader>
                    <TableBody>
                      {backTests.map((bt) => (
                        <TableRow key={bt.backTestId} className="hover:bg-slate-50/50">
                          <TableCell className="font-mono text-xs text-slate-600">
                            {bt.backTestId.slice(0, 8)}…
                          </TableCell>
                          <TableCell>
                            <StatusBadge status={bt.status} />
                          </TableCell>
                          <TableCell className="text-sm text-slate-600">
                            <LocalTime value={bt.createdAt} />
                          </TableCell>
                          <TableCell>
                            <Button
                              variant="outline"
                              size="sm"
                              className="gap-1.5"
                              onClick={() => router.push(`/back-tests/${bt.backTestId}`)}
                            >
                              <Eye className="size-4" />
                              {t("common.view")}
                            </Button>
                          </TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                </div>
                <Pagination
                  handlePrevPage={() => setBackTestPageNumber((p) => Math.max(1, p - 1))}
                  pageNumber={backTestPageNumber}
                  totalPages={backTestTotalPages}
                  handleNextPage={() => setBackTestPageNumber((p) => Math.min(backTestTotalPages, p + 1))}
                />
              </>
            )}
          </div>
        )}

        {activeTab === "History" && canReadHistory && (
          <HistoryTab
            ruleId={ruleId as string}
            matchPolarity={rule.matchPolarity}
            onUnauthorized={goToLogin}
          />
        )}
      </div>

      <Dialog
        open={versionEditorSeed !== null}
        onOpenChange={(open) => {
          if (open) return;
          setVersionEditorSeed(null);
          setVersionViolations([]);
        }}
      >
        <DialogContent className="flex h-[85vh] w-[min(96vw,80rem)] max-w-[96vw] flex-col overflow-hidden sm:max-w-[80rem]">
          <DialogHeader className="shrink-0">
            <DialogTitle>{t("rules.newVersion")}</DialogTitle>
            <DialogDescription>
              Saves an undeployed snapshot of this logic. The live rule is
              unchanged until you deploy the version
              {requiresDeploymentApproval
                ?
                  " — and, in this organisation, until somebody else agrees to that deployment."
                : "."}
            </DialogDescription>
          </DialogHeader>
          <div className="min-h-0 flex-1 overflow-y-auto">
            {versionEditorSeed !== null && (
              <RuleBuilder
                key={JSON.stringify(versionEditorSeed)}
                ruleSetId={rule.ruleSetId}
                initialRules={[versionEditorSeed]}
                submitRuleText=""
                readOnly={false}
                matchPolarity={rule.matchPolarity}
                saveButtonText={t("rules.createVersion")}
                schemaViolations={versionViolations}
                handleSaveBody={createVersion}
              />
            )}
          </div>
        </DialogContent>
      </Dialog>

      <DeployDialog
        ruleId={ruleId as string}
        version={deployTarget}
        requiresApproval={requiresDeploymentApproval}
        isRollback={
          deployTarget !== null &&
          liveVersionNumber !== null &&
          deployTarget.versionNumber < liveVersionNumber
        }
        onOpenChange={(open) => !open && setDeployTarget(null)}
        onDeployed={handleDeployed}
        onUnauthorized={goToLogin}
      />

      <Dialog open={ruleSetModalOpen} onOpenChange={setRuleSetModalOpen}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>
              {modalAction === "Transfer"
                ? t("rules.transferRuleTitle")
                : t("rules.copyRuleTitle")}
            </DialogTitle>
          </DialogHeader>
          <div className="relative">
            {modalLoading && (
              <div className="absolute inset-0 z-10 flex items-center justify-center rounded bg-card/70">
                <div className="h-6 w-6 animate-spin rounded-full border-2 border-brand-600 border-t-transparent" />
              </div>
            )}
            <Input
              placeholder={t("rules.searchRuleSets")}
              value={ruleSetSearchTerm}
              onChange={(e) => setRuleSetSearchTerm(e.target.value)}
              onFocus={() => setDropdownOpen(true)}
              onBlur={() => setTimeout(() => setDropdownOpen(false), 200)}
            />
            {dropdownOpen && filteredRuleSets.length > 0 && (
              <ul className="absolute left-0 right-0 z-20 mt-1 max-h-48 overflow-y-auto rounded-md border border-slate-200 bg-card shadow-md">
                {filteredRuleSets.map((rs) => (
                  <li
                    key={rs.ruleSetId}
                    className="cursor-pointer px-3 py-2 text-sm text-slate-700 hover:bg-slate-50"
                    onMouseDown={() => handleRuleSetSelect(rs.ruleSetId)}
                  >
                    {rs.friendlyName}
                  </li>
                ))}
              </ul>
            )}
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={handleCloseModal} disabled={modalLoading}>{t("common.cancel")}</Button>
            <Button
              onClick={handleModalAction}
              disabled={modalLoading || !selectedRuleSetId}
            >
              {modalAction === "Transfer"
                ? t("rules.transfer")
                : t("rules.copy")}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <Dialog open={backTestModalOpen} onOpenChange={setBackTestModalOpen}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>{t("rules.runBacktest")}</DialogTitle>
          </DialogHeader>
          <div className="space-y-4 py-2">
            <div className="space-y-1.5">
              <label className="font-mono text-xs uppercase tracking-widest text-slate-400">
                {t("rules.ruleLogicToTest")}
              </label>
              <Select
                value={backTestVersion}
                onValueChange={setBackTestVersion}
                disabled={creatingBackTest}
              >
                <SelectTrigger><SelectValue /></SelectTrigger>
                <SelectContent>
                  <SelectItem value={LIVE_LOGIC}>
                    Live logic
                    {liveVersionNumber !== null ? ` (v${liveVersionNumber})` : ""}
                  </SelectItem>
                  {backTestVersionOptions.map((n) => (
                    <SelectItem key={n} value={String(n)}>
                      v{n}
                      {n === liveVersionNumber ? " — live" : ""}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              <p className="text-xs text-slate-500">
                Testing an undeployed version does not deploy it — the live rule
                is unaffected.
              </p>
            </div>

            <div className="flex items-center justify-between">
              <label htmlFor="sampleSize" className="font-mono text-xs uppercase tracking-widest text-slate-400">
                {t("rules.dataSampleSize")}
              </label>
              <span className="text-sm font-semibold text-slate-900">{sampleSize}%</span>
            </div>
            <input
              id="sampleSize"
              type="range"
              min={0}
              max={100}
              step={1}
              value={sampleSize}
              onChange={(e) => setSampleSize(Number(e.target.value))}
              disabled={creatingBackTest}
              className="w-full accent-brand-800"
            />
            <p className="text-xs text-slate-500">
              {t("rules.selectThePercentageOfAvailable")}
            </p>
          </div>
          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => setBackTestModalOpen(false)}
              disabled={creatingBackTest}
            >
              {t("common.cancel")}
            </Button>
            <Button onClick={createBackTest} disabled={creatingBackTest}>
              {creatingBackTest ? "Starting..." : "Run BackTest"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
};

export default RuleDetails;
