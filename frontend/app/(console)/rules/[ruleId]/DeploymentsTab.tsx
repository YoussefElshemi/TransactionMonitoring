"use client";

import { useCallback, useEffect, useState } from "react";
import { toast } from "sonner";

import { Paged, RuleDeploymentResource } from "@/types";
import { relationText, userName } from "@/lib/resources";
import Link from "next/link";

import Pagination from "@/components/Pagination";
import { StatusBadge } from "@/components/StatusBadge";
import { Button } from "@/components/ui/button";
import { DateRangeField } from "@/components/DateRangePicker";
import { Skeleton } from "@/components/ui/skeleton";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";

import {
  API_BASE,
  DEPLOYMENT_STATUS_LABELS,
  STRATEGY_LABELS,
  isPendingApprovalDeployment,
  reportApiError,
  versionLabel,
} from "./api";
import { DeploymentProgress } from "./DeploymentProgress";

import { apiFetch } from "@/lib/http";

import { LocalTime } from "@/components/LocalTime";
import { useTranslation } from "@/context/LocalizationContext";

interface DeploymentsTabProps {
  ruleId: string;
  canDeploy: boolean;
  versionIndex: Record<string, number>;
  refreshKey: number;
  onCancelled: () => void;
  onUnauthorized: () => void;
}

const PAGE_SIZE = 10;

export function DeploymentsTab({
  ruleId,
  canDeploy,
  versionIndex,
  refreshKey,
  onCancelled,
  onUnauthorized,
}: DeploymentsTabProps) {
  const t = useTranslation();
  const [deployments, setDeployments] = useState<RuleDeploymentResource[]>([]);
  const [loading, setLoading] = useState<boolean>(false);
  const [cancelling, setCancelling] = useState<string | null>(null);
  const [pageNumber, setPageNumber] = useState<number>(1);
  const [totalPages, setTotalPages] = useState<number>(1);
  const [fromDate, setFromDate] = useState<string>("");
  const [toDate, setToDate] = useState<string>("");

  const fetchDeployments = useCallback(
    async (page: number) => {
      setLoading(true);
      try {
        const params = new URLSearchParams({
          pageNumber: String(page),
          pageSize: String(PAGE_SIZE),
        });
        if (fromDate) params.set("fromDate", fromDate);
        if (toDate) params.set("toDate", toDate);

        const response = await apiFetch(
          `${API_BASE}/rules/${ruleId}/deployments?${params}`,
          { credentials: "include" }
        );

        if (response.ok) {
          const data = (await response.json()) as Paged<RuleDeploymentResource>;
          setDeployments(data.data);
          setTotalPages(data.metadata.pageCount);
        } else {
          await reportApiError(response, {
            fallback: t("rules.failedLoadDeployments"),
            onUnauthorized,
            toastId: "error-deployments",
          });
        }
      } catch {
        toast.error(t("rules.failedLoadDeployments"), { id: "error-deployments" });
      } finally {
        setLoading(false);
      }
    },
    [ruleId, fromDate, toDate, onUnauthorized]
  );

  useEffect(() => {
    fetchDeployments(pageNumber);
  }, [fetchDeployments, pageNumber, refreshKey]);

  const cancelDeployment = async (deploymentId: string) => {
    setCancelling(deploymentId);
    try {
      const response = await apiFetch(
        `${API_BASE}/rules/${ruleId}/deployments/${deploymentId}/cancel`,
        { method: "POST", credentials: "include" }
      );

      if (response.ok) {
        toast.success(t("rules.rolloutCancelledRuleStaysPrevious"));
        onCancelled();
        fetchDeployments(pageNumber);
      } else {
        await reportApiError(response, {
          fallback: t("rules.failedCancelDeployment"),
          onUnauthorized,
        });
      }
    } catch {
      toast.error(t("rules.failedCancelDeployment"));
    } finally {
      setCancelling(null);
    }
  };

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center gap-3">
        <DateRangeField
          from={fromDate}
          to={toDate}
          onChange={(from, to) => {
            setFromDate(from);
            setToDate(to);
            setPageNumber(1);
          }}
          aria-label={t("rules.filterRuleDeploymentsByDate")}
        />
      </div>

      <div className="overflow-hidden rounded-xl border border-slate-200 bg-card">
        <Table>
          <TableHeader>
            <TableRow className="bg-slate-50 hover:bg-slate-50">
              <TableHead>
                {t("rules.change")}
              </TableHead>
              <TableHead>
                {t("rules.strategy")}
              </TableHead>
              <TableHead>
                {t("common.status")}
              </TableHead>
              <TableHead className="w-48">
                {t("rules.rollout")}
              </TableHead>
              <TableHead>
                {t("rules.deployedBy")}
              </TableHead>
              <TableHead>
                {t("common.started")}
              </TableHead>
              <TableHead className="w-24">
                {t("common.actions")}
              </TableHead>
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
            ) : deployments.length === 0 ? (
              <TableRow>
                <TableCell
                  colSpan={7}
                  className="py-14 text-center font-mono text-xs uppercase tracking-widest text-slate-400"
                >
                  {t("rules.noDeploymentsFound")}
                </TableCell>
              </TableRow>
            ) : (
              deployments.map(({ ruleDeployment: deployment, createdByUser }) => (
                <TableRow
                  key={deployment.ruleDeploymentId}
                  className="hover:bg-slate-50/50"
                >
                  <TableCell className="text-sm text-slate-900">
                    <span className="text-slate-500">
                      {versionLabel(
                        deployment.previousRuleVersionId,
                        versionIndex
                      )}
                    </span>
                    <span className="mx-1.5 text-slate-400">→</span>
                    <span className="font-medium">
                      {versionLabel(
                        deployment.targetRuleVersionId,
                        versionIndex
                      )}
                    </span>
                  </TableCell>
                  <TableCell className="text-sm text-slate-600">
                    {STRATEGY_LABELS[deployment.strategy] ??
                      deployment.strategy}
                  </TableCell>
                  <TableCell>
                    <StatusBadge
                      status={deployment.status}
                      label={DEPLOYMENT_STATUS_LABELS[deployment.status]}
                    />
                  </TableCell>
                  <TableCell>
                    {isPendingApprovalDeployment(deployment) ? (
                      <span className="text-sm text-slate-400">{t("rules.notStarted")}</span>
                    ) : deployment.strategy === "Gradual" ? (
                      <DeploymentProgress
                        percentage={deployment.currentPercentage}
                        muted={deployment.status !== "InProgress"}
                      />
                    ) : (
                      <span className="text-sm text-slate-400">{t("rules.instant")}</span>
                    )}
                  </TableCell>
                  <TableCell className="text-sm text-slate-600">
                    {relationText(
                      userName(createdByUser),
                      deployment.createdByUserId
                    )}
                  </TableCell>
                  <TableCell className="text-sm text-slate-600">
                    <LocalTime value={deployment.createdAt} />
                  </TableCell>
                  <TableCell>
                    {deployment.status === "InProgress" && canDeploy && (
                      <Button
                        variant="outline"
                        size="sm"
                        disabled={cancelling === deployment.ruleDeploymentId}
                        onClick={() =>
                          cancelDeployment(deployment.ruleDeploymentId)
                        }
                      >
                        {cancelling === deployment.ruleDeploymentId
                          ? "Cancelling..."
                          : "Cancel"}
                      </Button>
                    )}
                    {isPendingApprovalDeployment(deployment) && (
                      <Link
                        href="/approvals?status=Pending&subject=RuleDeployment"
                        className="text-sm text-brand-800 hover:underline"
                      >
                        {t("rules.openRequest")}
                      </Link>
                    )}
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </div>

      {deployments.length > 0 && (
        <Pagination
          handlePrevPage={() => setPageNumber((p) => Math.max(1, p - 1))}
          pageNumber={pageNumber}
          totalPages={totalPages}
          handleNextPage={() =>
            setPageNumber((p) => Math.min(totalPages, p + 1))
          }
        />
      )}
    </div>
  );
}
