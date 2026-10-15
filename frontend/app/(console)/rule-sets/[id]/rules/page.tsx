"use client";

import { useState, useEffect } from "react";
import {
  useRouter,
  useParams,
  useSearchParams,
  usePathname
} from "next/navigation";
import {
  Paged,
  RuleResource,
  ProblemDetails,
  ValidationProblemDetails
} from "@/types";
import { toast } from "sonner";
import Pagination from "@/components/Pagination";
import { StatusBadge } from "@/components/StatusBadge";
import { SeverityBadge } from "@/components/SeverityBadge";
import { MatchPolarityBadge } from "@/components/MatchPolarityBadge";
import { PageHeader } from "@/components/PageHeader";
import { useTranslation } from "@/context/LocalizationContext";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { Skeleton } from "@/components/ui/skeleton";
import { PageSizeSelect } from "@/components/PageSizeSelect";
import { Eye, Plus, Trash2 } from "lucide-react";
import { usePermissions } from "@/context/PermissionsContext";
import { loginHref } from "@/lib/authRedirect";
import { relationText, userName } from "@/lib/resources";

import { apiFetch } from "@/lib/http";
import { API_BASE } from "@/lib/api";

const Rules = () => {
  const t = useTranslation();
  const { id } = useParams();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const router = useRouter();
  const { holdsOver } = usePermissions();
  const canWriteRules = holdsOver("rules:write", { ruleSetId: id as string });

  const redirectUri = searchParams.toString()
    ? `${pathname}?${searchParams.toString()}`
    : pathname;
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

  const fetchRules = async (
    ruleSetId: string,
    pageNumber: number,
    pageSize: number,
    searchTerm: string
  ) => {
    setLoading(true);
    try {
      const response = await apiFetch(
        `${API_BASE}/rule-sets/${ruleSetId}/rules?PageNumber=${pageNumber}&PageSize=${pageSize}&FriendlyName=${searchTerm}`,
        {
          method: "GET",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
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
                `${key}${key.length ? ": " : ""}${errorData.errors[key].join("\n")}`
            )
            .join("\n")
        );
      } else if (response.status === 401) {
        toast.error(t("common.pleaseLoginAccessResource"), { id: "unauthorized" });
        return router.push(loginHref(redirectUri));
      } else if (response.status === 403) {
        const errorData = (await response.json()) as ProblemDetails;
        toast.error(
          errorData.detail || t("common.youDoNotHavePermission"),
          { id: "forbidden" }
        );
        return router.push("/");
      } else {
        const errorData = (await response.json()) as ProblemDetails;
        toast.error(errorData.detail || t("ruleSets.errorOccurredFetchingRules"), {
          id: "error-fetching-rules",
        });
      }
    } catch {
      toast.error(t("ruleSets.errorOccurredFetchingRules"), {
        id: "error-fetching-rules",
      });
    } finally {
      setLoading(false);
    }
  };

  const deleteRule = async (ruleSetId: string, ruleId: string) => {
    try {
      const response = await apiFetch(
        `${API_BASE}/rules/${ruleId}`,
        { method: "DELETE", credentials: "include" }
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
          errorData.detail || t("common.youDoNotHavePermission2")
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
        { method: "POST", credentials: "include" }
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
          errorData.detail || t("common.youDoNotHavePermission2")
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
    fetchRules(id as string, pageNumber, pageSize, searchTerm);
  }, [id, pageNumber, pageSize, searchTerm]);

  useEffect(() => {
    const params = new URLSearchParams();
    params.set("pageNumber", pageNumber.toString());
    params.set("pageSize", pageSize.toString());
    if (searchTerm.length) params.set("friendlyName", searchTerm);
    router.replace(`?${params.toString()}`);
  }, [pageNumber, pageSize, searchTerm, router]);

  return (
    <div className="space-y-4">
      <PageHeader eyebrow={t("rules.list.eyebrow")} title={t("rules.list.title")}>
        {canWriteRules && (
          <Button
            size="sm"
            className="gap-1.5"
            onClick={() => router.push(`/rule-sets/${id}/create-rule`)}
          >
            <Plus aria-hidden className="size-4" />
            {t("rules.list.new")}
          </Button>
        )}
      </PageHeader>

      <div className="flex items-center justify-between gap-3">
        <Input
          value={searchTerm}
          onChange={(e) => {
            setSearchTerm(e.target.value);
            setPageNumber(1);
          }}
          placeholder={t("ruleSets.searchByFriendlyName")}
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
              <TableHead>{t("rules.column.name")}</TableHead>
              <TableHead>{t("rules.column.createdBy")}</TableHead>
              <TableHead>{t("rules.column.status")}</TableHead>
              <TableHead>{t("rules.column.action")}</TableHead>
              <TableHead>{t("rules.column.severity")}</TableHead>
              <TableHead>{t("rules.column.firesWhen")}</TableHead>
              <TableHead className="w-32">{t("common.actions")}</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {loading ? (
              Array.from({ length: 5 }).map((_, i) => (
                <TableRow key={i}>
                  {Array.from({ length: 7 }).map((_, j) => (
                    <TableCell key={j}>
                      <Skeleton className="h-4 w-full" />
                    </TableCell>
                  ))}
                </TableRow>
              ))
            ) : rules.length === 0 ? (
              <TableRow>
                <TableCell colSpan={7} className="py-14 text-center font-mono text-xs uppercase tracking-widest text-slate-400">
                  {t("rules.empty")}
                </TableCell>
              </TableRow>
            ) : (
              rules.map(({ rule, createdByUser }) => (
                <TableRow key={rule.ruleId} className="hover:bg-slate-50/50">
                  <TableCell className="font-medium text-slate-900">
                    {rule.friendlyName}
                  </TableCell>
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
                        onClick={() =>
                          router.push(`/rules/${rule.ruleId}`)
                        }
                      >
                        <Eye aria-hidden className="size-4" />
                        {t("rules.action.view")}
                        <span className="sr-only"> — {rule.friendlyName}</span>
                      </Button>
                      {canWriteRules && (
                        <Button
                          variant="ghost"
                          size="icon"
                          className="h-8 w-8 text-critical-500 hover:text-critical-700"
                          onClick={() => deleteRule(id as string, rule.ruleId)}
                          title={t("rules.action.delete")}
                          aria-label={t("rules.action.deleteNamed", rule.friendlyName)}
                        >
                          <Trash2 aria-hidden className="size-4" />
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
    </div>
  );
};

export default Rules;
