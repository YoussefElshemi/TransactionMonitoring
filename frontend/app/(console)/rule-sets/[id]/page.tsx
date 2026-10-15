"use client";

import { useCallback, useState, useEffect } from "react";
import {
  useRouter,
  useParams,
  useSearchParams,
  usePathname
} from "next/navigation";
import {
  Paged,
  RuleResource,
  RuleSetResource,
  ValidationProblemDetails,
  ProblemDetails
} from "@/types";
import { toast } from "sonner";
import Pagination from "@/components/Pagination";
import { NotFound } from "@/components/NotFound";
import { Plus, Trash2, Eye, Save } from "lucide-react";
import { useAuth } from "@/context/AuthContext";
import { usePermissions } from "@/context/PermissionsContext";
import {
  ownerBody,
  SubOrganizationOwner,
  SubOrganizationOwnerField,
} from "@/components/subOrganizations/SubOrganizationOwnerField";
import { PageHeader } from "@/components/PageHeader";
import { StatusBadge } from "@/components/StatusBadge";
import { SeverityBadge } from "@/components/SeverityBadge";
import { MatchPolarityBadge } from "@/components/MatchPolarityBadge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow
} from "@/components/ui/table";
import { Skeleton } from "@/components/ui/skeleton";
import { Card, CardContent } from "@/components/ui/card";
import { TabBar } from "@/components/TabBar";
import { HistoryPanel, historyPath } from "@/components/history";
import { loginHref } from "@/lib/authRedirect";
import { API_BASE, unwrap } from "@/lib/api";
import { relationText, userName } from "@/lib/resources";

import { apiFetch } from "@/lib/http";

import { LocalTime } from "@/components/LocalTime";
import { PageSizeSelect } from "@/components/PageSizeSelect";
import { useTranslation } from "@/context/LocalizationContext";

const RuleSetDetails = () => {
  const t = useTranslation();
  const { id } = useParams();
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const { user } = useAuth();
  const { holdsAll, holdsOver } = usePermissions();

  const ruleSetScope = { ruleSetId: id as string };
  const canEditRuleSet = holdsOver("rule-sets:write", ruleSetScope);
  const canWriteRules = holdsOver("rules:write", ruleSetScope);
  const canReadHistory = holdsAll("rule-sets:read", "audit-logs:read");

  const redirectUri = searchParams.toString()
    ? `${pathname}?${searchParams.toString()}`
    : pathname;
  const [ruleSetResource, setRuleSetResource] =
    useState<RuleSetResource | null>(null);
  const ruleSet = ruleSetResource?.ruleSet ?? null;
  const [friendlyName, setFriendlyName] = useState<string>("");
  const [isEnabled, setIsEnabled] = useState<boolean>(false);
  const [reviewThreshold, setReviewThreshold] = useState<string>("");
  const [owner, setOwner] = useState<SubOrganizationOwner>(undefined);
  const [rules, setRules] = useState<RuleResource[]>([]);
  const [pageNumber, setPageNumber] = useState<number>(
    parseInt(searchParams.get("pageNumber") || "1")
  );
  const [pageSize, setPageSize] = useState<number>(
    parseInt(searchParams.get("pageSize") || "10")
  );
  const [searchTerm, setSearchTerm] = useState<string>(
    searchParams.get("friendlyName") || ""
  );
  const [totalPages, setTotalPages] = useState<number>(1);

  const [loading, setLoading] = useState<boolean>(true);
  const [rulesLoading, setRulesLoading] = useState<boolean>(true);
  const [saveLoading, setSaveLoading] = useState<boolean>(false);
  const [activeTab, setActiveTab] = useState<string>("Rules");

  const goToLogin = useCallback(() => {
    router.push(loginHref(redirectUri));
  }, [router, redirectUri]);

  const fetchRuleSetById = async (id: string) => {
    setLoading(true);

    try {
      const response = await apiFetch(`${API_BASE}/rule-sets/${id}`, {
        method: "GET",
        headers: {
          "Content-Type": "application/json"
        },
        credentials: "include"
      });

      if (response.ok) {
        const data = await unwrap<RuleSetResource>(response);
        setRuleSetResource(data);
        setFriendlyName(data.ruleSet.friendlyName);
        setIsEnabled(data.ruleSet.isEnabled);
        setReviewThreshold(
          data.ruleSet.reviewThreshold === null
            ? ""
            : String(data.ruleSet.reviewThreshold)
        );
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
          errorData.detail || t("ruleSets.errorOccurredFetchingRuleSet"),
          { id: "error-fetching-rule-sets" }
        );
      }
    } catch {
      toast.error(t("ruleSets.errorOccurredFetchingRuleSet"), {
        id: "error-fetching-rule-sets"
      });
    } finally {
      setLoading(false);
    }
  };

  const fetchRules = async (
    ruleSetId: string,
    pageNumber: number,
    pageSize: number,
    searchTerm: string
  ) => {
    setRulesLoading(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/rule-sets/${ruleSetId}/rules?PageNumber=${pageNumber}&PageSize=${pageSize}&FriendlyName=${searchTerm}`,
        {
          method: "GET",
          headers: {
            "Content-Type": "application/json"
          },
          credentials: "include"
        }
      );

      if (response.ok) {
        const data = (await response.json()) as Paged<RuleResource>;
        setRules(data.data);
        setTotalPages(data.metadata.pageCount);
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
          errorData.detail || t("ruleSets.errorOccurredFetchingRules"),
          { id: "error-fetching-rules" }
        );
      }
    } catch {
      toast.error(t("ruleSets.errorOccurredFetchingRules"), {
        id: "error-fetching-rules"
      });
    } finally {
      setRulesLoading(false);
    }
  };

  const parsedReviewThreshold =
    reviewThreshold.trim() === "" ? null : Number(reviewThreshold);

  const handleSave = async () => {
    if (!ruleSet) return;
    if (
      ruleSet.friendlyName == friendlyName &&
      ruleSet.isEnabled == isEnabled &&
      ruleSet.reviewThreshold == parsedReviewThreshold &&
      owner === undefined
    )
      return;

    setSaveLoading(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/rule-sets/${ruleSet.ruleSetId}`,
        {
          method: "PATCH",
          headers: {
            "Content-Type": "application/json"
          },
          credentials: "include",
          body: JSON.stringify({
            friendlyName,
            isEnabled,
            reviewThreshold: parsedReviewThreshold,
            ...ownerBody(owner)
          })
        }
      );

      if (response.ok) {
        toast.success(t("ruleSets.successfullyUpdatedRuleSet"));
        setOwner(undefined);
        fetchRuleSetById(ruleSet.ruleSetId);
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
        toast.error(
          errorData.detail || t("common.errorOccurredSavingChanges")
        );
      }
    } catch {
      toast.error(t("common.errorOccurredSavingChanges"));
    } finally {
      setSaveLoading(false);
    }
  };

  const deleteRuleSet = async (ruleSetId: string) => {
    try {
      const response = await apiFetch(
        `${API_BASE}/rule-sets/${ruleSetId}`,
        {
          method: "DELETE",
          credentials: "include"
        }
      );

      if (response.ok) {
        toast.success(t("ruleSets.successfullyDeletedRuleSet"), {
          action: { label: t("common.undo"), onClick: () => recoverRuleSet(ruleSetId) },
        });
        router.push("/rule-sets");
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
        toast.error(errorData.detail || t("ruleSets.failedDeleteRuleSet"));
      }
    } catch {
      toast.error(t("ruleSets.failedDeleteRuleSet"));
    }
  };

  const recoverRuleSet = async (ruleSetId: string) => {
    try {
      const response = await apiFetch(
        `${API_BASE}/rule-sets/${ruleSetId}/recover`,
        {
          method: "POST",
          credentials: "include"
        }
      );

      if (response.ok) {
        toast.success(t("ruleSets.successfullyRecoveredRuleSet"));
        router.push(`/rule-sets/${ruleSetId}`);
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
        toast.error(errorData.detail || t("ruleSets.failedRecoverRuleSet"));
      }
    } catch {
      toast.error(t("ruleSets.failedRecoverRuleSet"));
    }
  };

  const deleteRule = async (ruleSetId: string, ruleId: string) => {
    try {
      const response = await apiFetch(
        `${API_BASE}/rules/${ruleId}`,
        {
          method: "DELETE",
          credentials: "include"
        }
      );

      if (response.ok) {
        toast.success(t("common.successfullyDeletedRule"), {
          action: { label: t("common.undo"), onClick: () => recoverRule(ruleSetId, ruleId) },
        });
        fetchRules(ruleSetId, pageNumber, pageSize, searchTerm);
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

  const recoverRule = async (ruleSetId: string, ruleId: string) => {
    try {
      const response = await apiFetch(
        `${API_BASE}/rules/${ruleId}/recover`,
        {
          method: "POST",
          credentials: "include"
        }
      );

      if (response.ok) {
        toast.success(t("common.successfullyRecoveredRule"));
        fetchRules(ruleSetId, pageNumber, pageSize, searchTerm);
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

  useEffect(() => {
    fetchRuleSetById(id as string);
  }, [id]);

  useEffect(() => {
    fetchRules(id as string, pageNumber, pageSize, searchTerm);
  }, [id, pageNumber, pageSize, searchTerm]);

  useEffect(() => {
    const params = new URLSearchParams();
    params.set("pageNumber", pageNumber.toString());
    params.set("pageSize", pageSize.toString());
    if (searchTerm.length) {
      params.set("friendlyName", searchTerm);
    }
    router.replace(`?${params.toString()}`);
  }, [pageNumber, pageSize, searchTerm, router]);

  if (loading) return (
    <div className="space-y-4">
      <Skeleton className="h-8 w-48" />
      <Card><CardContent className="pt-6 space-y-3">
        {Array.from({ length: 4 }).map((_, i) => <Skeleton key={i} className="h-4 w-full" />)}
      </CardContent></Card>
    </div>
  );
  if (!ruleSet) return <NotFound
        title={t("ruleSets.ruleSetNotFound")}
        description={t("common.notFound.description")}
        backHref="/rule-sets"
        backLabel={t("ruleSets.allRuleSets")}
      />;

  return (
    <div className="space-y-6">
      <PageHeader eyebrow={t("ruleSets.ruleSet")} title={ruleSet.friendlyName}>
        {canEditRuleSet && (
          <>
            <Button
              variant="outline"
              size="sm"
              className="gap-1.5"
              onClick={handleSave}
              disabled={saveLoading}
            >
              <Save className="size-4" />
              {saveLoading ? "Saving..." : "Save"}
            </Button>
            <Button
              variant="ghost"
              size="icon"
              className="h-8 w-8 text-critical-500 hover:text-critical-700"
              onClick={() => deleteRuleSet(ruleSet.ruleSetId)}
            >
              <Trash2 className="size-4" />
            </Button>
          </>
        )}
      </PageHeader>

      <Card>
        <CardContent className="pt-6 space-y-4">
          <div className="grid grid-cols-2 gap-x-8 gap-y-4">
            <div className="space-y-1.5">
              <label className="font-mono text-xs uppercase tracking-widest text-slate-400">{t("common.name")}</label>
              <Input
                value={friendlyName}
                onChange={(e) => setFriendlyName(e.target.value)}
                disabled={!canEditRuleSet}
              />
            </div>
            <div className="space-y-1.5">
              <label className="font-mono text-xs uppercase tracking-widest text-slate-400">{t("common.status")}</label>
              <div className="flex items-center gap-2 pt-1">
                <input
                  type="checkbox"
                  checked={isEnabled}
                  onChange={(e) => setIsEnabled(e.target.checked)}
                  disabled={!canEditRuleSet}
                  className="h-4 w-4 rounded border-slate-300 accent-brand-800"
                />
                <span className="text-sm text-slate-700">{t("common.enabled")}</span>
              </div>
            </div>
            <div className="space-y-1.5">
              <label className="font-mono text-xs uppercase tracking-widest text-slate-400">
                {t("ruleSets.reviewThreshold")}
              </label>
              <Input
                type="number"
                min={0}
                placeholder={t("ruleSets.serverDefault100")}
                value={reviewThreshold}
                onChange={(e) => setReviewThreshold(e.target.value)}
                disabled={!canEditRuleSet}
              />
              <p className="text-xs text-slate-500">
                Risk score at which a fired rule&apos;s action is escalated to
                Review. Empty falls back to the server default, not 0.
              </p>
            </div>
            <SubOrganizationOwnerField
              organizationId={user!.organizationId}
              value={owner}
              onChange={setOwner}
              disabled={!canEditRuleSet}
              label={t("ruleSets.ownedBy")}
            />
            <div>
              <span className="text-sm font-medium text-slate-500">{t("ruleSets.createdBy")}</span>
              <p className="mt-0.5 text-sm text-slate-900">
                {relationText(
                  userName(ruleSetResource?.createdByUser),
                  ruleSet.createdByUserId
                )}
              </p>
            </div>
            <div>
              <span className="text-sm font-medium text-slate-500">{t("common.created")}</span>
              <p className="mt-0.5 text-sm text-slate-900"><LocalTime value={ruleSet.createdAt} /></p>
            </div>
            <div>
              <span className="text-sm font-medium text-slate-500">{t("common.updated")}</span>
              <p className="mt-0.5 text-sm text-slate-900"><LocalTime value={ruleSet.updatedAt} /></p>
            </div>
          </div>
        </CardContent>
      </Card>

      <div className="space-y-4">
        <TabBar
          tabs={["Rules", ...(canReadHistory ? ["History"] : [])]}
          activeTab={activeTab}
          onChange={setActiveTab}
          actions={
            activeTab === "Rules" &&
            canWriteRules && (
              <Button
                size="sm"
                className="gap-1.5"
                onClick={() =>
                  router.push(`/rule-sets/${ruleSet.ruleSetId}/create-rule`)
                }
              >
                <Plus className="size-4" />
                {t("ruleSets.addRule")}
              </Button>
            )
          }
        />

        {activeTab === "History" && canReadHistory ? (
          <HistoryPanel
            entityType="RuleSet"
            path={historyPath.RuleSet(ruleSet.ruleSetId)}
            entityId={ruleSet.ruleSetId}
            onUnauthorized={goToLogin}
          />
        ) : (
          <>
          <div className="flex items-center justify-between gap-3">
            <Input
              value={searchTerm}
              onChange={(e) => { setSearchTerm(e.target.value); setPageNumber(1); }}
              placeholder={t("ruleSets.searchRules")}
              className="w-64"
            />
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
                  <TableHead>{t("common.name")}</TableHead>
                  <TableHead>{t("ruleSets.createdBy")}</TableHead>
                  <TableHead>{t("common.status")}</TableHead>
                  <TableHead>{t("ruleSets.action")}</TableHead>
                  <TableHead>{t("common.severity")}</TableHead>
                  <TableHead>{t("ruleSets.firesWhen")}</TableHead>
                  <TableHead className="w-32">{t("common.actions")}</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {rulesLoading ? (
                  Array.from({ length: 5 }).map((_, i) => (
                    <TableRow key={i}>
                      {Array.from({ length: 7 }).map((_, j) => (
                        <TableCell key={j}><Skeleton className="h-4 w-full" /></TableCell>
                      ))}
                    </TableRow>
                  ))
                ) : rules.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={7} className="py-14 text-center font-mono text-xs uppercase tracking-widest text-slate-400">
                      {t("ruleSets.noRulesFound")}
                    </TableCell>
                  </TableRow>
                ) : (
                  rules.map(({ rule, createdByUser }) => (
                    <TableRow key={rule.ruleId} className="hover:bg-slate-50/50">
                      <TableCell className="font-medium text-slate-900">{rule.friendlyName}</TableCell>
                      <TableCell className="text-sm text-slate-600">
                        {relationText(
                          userName(createdByUser),
                          rule.createdByUserId
                        )}
                      </TableCell>
                      <TableCell>
                        <StatusBadge status={rule.isEnabled ? "Enabled" : "Disabled"} />
                      </TableCell>
                      <TableCell>
                        <StatusBadge status={rule.action} />
                      </TableCell>
                      <TableCell>
                        <SeverityBadge severity={rule.severity} />
                      </TableCell>
                      <TableCell>
                        <MatchPolarityBadge matchPolarity={rule.matchPolarity} />
                      </TableCell>
                      <TableCell>
                        <div className="flex items-center gap-1">
                          <Button
                            variant="outline"
                            size="sm"
                            className="gap-1.5"
                            onClick={() => router.push(`/rules/${rule.ruleId}`)}
                          >
                            <Eye className="size-4" />
                            {t("common.view")}
                          </Button>
                          {canWriteRules && (
                            <Button
                              variant="ghost"
                              size="icon"
                              className="h-8 w-8 text-critical-500 hover:text-critical-700"
                              onClick={() => deleteRule(ruleSet.ruleSetId, rule.ruleId)}
                            >
                              <Trash2 className="size-4" />
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

          {rules.length > 0 && (
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
    </div>
  );
};

export default RuleSetDetails;
