"use client";

import React, { useCallback, useEffect, useState } from "react";
import { toast } from "sonner";
import { Rocket, Undo2 } from "lucide-react";

import { ListRuleVersion, RuleBodyDto, RuleVersion } from "@/types";
import RuleBuilder from "@/components/RuleBuilder";
import Pagination from "@/components/Pagination";
import { StatusBadge } from "@/components/StatusBadge";
import { SeverityBadge } from "@/components/SeverityBadge";
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

import { API_BASE, reportApiError } from "./api";

import { apiFetch } from "@/lib/http";

import { LocalTime } from "@/components/LocalTime";
import { useTranslation } from "@/context/LocalizationContext";

interface VersionsTabProps {
  ruleId: string;
  canWrite: boolean;
  canDeploy: boolean;
  liveVersionId: string | null;
  liveVersionNumber: number | null;
  deployingVersionId: string | null;
  proposedVersionId: string | null;
  deploymentInProgress: boolean;
  deploymentBlockedReason: string | null;
  requiresApproval: boolean | null;
  refreshKey: number;
  onVersionsLoaded: (versions: RuleVersion[]) => void;
  onDeploy: (version: RuleVersion) => void;
  onCreateVersion: (seed?: RuleBodyDto) => void;
  onUnauthorized: () => void;
}

const PAGE_SIZE = 10;

export function VersionsTab({
  ruleId,
  canWrite,
  canDeploy,
  liveVersionId,
  liveVersionNumber,
  deployingVersionId,
  proposedVersionId,
  deploymentInProgress,
  deploymentBlockedReason,
  requiresApproval,
  refreshKey,
  onVersionsLoaded,
  onDeploy,
  onCreateVersion,
  onUnauthorized,
}: VersionsTabProps) {
  const t = useTranslation();
  const [versions, setVersions] = useState<RuleVersion[]>([]);
  const [loading, setLoading] = useState<boolean>(false);
  const [pageNumber, setPageNumber] = useState<number>(1);
  const [totalPages, setTotalPages] = useState<number>(1);
  const [fromDate, setFromDate] = useState<string>("");
  const [toDate, setToDate] = useState<string>("");
  const [expanded, setExpanded] = useState<Set<string>>(new Set());

  const toggle = (id: string) =>
    setExpanded((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });

  const fetchVersions = useCallback(
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
          `${API_BASE}/rules/${ruleId}/versions?${params}`,
          { credentials: "include" }
        );

        if (response.ok) {
          const data = (await response.json()) as ListRuleVersion;
          setVersions(data.data);
          setTotalPages(data.metadata.pageCount);
          onVersionsLoaded(data.data);
        } else {
          await reportApiError(response, {
            fallback: t("rules.failedLoadVersions"),
            onUnauthorized,
            toastId: "error-versions",
          });
        }
      } catch {
        toast.error(t("rules.failedLoadVersions"), { id: "error-versions" });
      } finally {
        setLoading(false);
      }
    },
    [ruleId, fromDate, toDate, onVersionsLoaded, onUnauthorized]
  );

  useEffect(() => {
    fetchVersions(pageNumber);
  }, [fetchVersions, pageNumber, refreshKey]);

  const stateOf = (version: RuleVersion): string => {
    if (version.ruleVersionId === liveVersionId) return "Live";
    if (version.ruleVersionId === deployingVersionId) return "Deploying";
    if (version.ruleVersionId === proposedVersionId) return "PendingApproval";
    return "Undeployed";
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
          aria-label={t("rules.filterRuleVersionsByDate")}
        />
      </div>

      <div className="overflow-hidden rounded-xl border border-slate-200 bg-card">
        <Table>
          <TableHeader>
            <TableRow className="bg-slate-50 hover:bg-slate-50">
              <TableHead className="w-24">
                {t("rules.version")}
              </TableHead>
              <TableHead>
                {t("common.created")}
              </TableHead>
              <TableHead>
                {t("common.state")}
              </TableHead>
              <TableHead>
                {t("rules.policy")}
              </TableHead>
              <TableHead>
                {t("rules.logic")}
              </TableHead>
              <TableHead className="w-56">
                {t("common.actions")}
              </TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {loading ? (
              Array.from({ length: 5 }).map((_, i) => (
                <TableRow key={i}>
                  {Array.from({ length: 6 }).map((_, j) => (
                    <TableCell key={j}>
                      <Skeleton className="h-4 w-full" />
                    </TableCell>
                  ))}
                </TableRow>
              ))
            ) : versions.length === 0 ? (
              <TableRow>
                <TableCell
                  colSpan={6}
                  className="py-14 text-center font-mono text-xs uppercase tracking-widest text-slate-400"
                >
                  {t("rules.noVersionsFound")}
                </TableCell>
              </TableRow>
            ) : (
              versions.map((version) => {
                const state = stateOf(version);
                const isLive = state === "Live";
                const isRollback =
                  liveVersionNumber !== null &&
                  version.versionNumber < liveVersionNumber;
                return (
                  <React.Fragment key={version.ruleVersionId}>
                    <TableRow className="hover:bg-slate-50/50">
                      <TableCell className="font-medium text-slate-900">
                        v{version.versionNumber}
                      </TableCell>
                      <TableCell className="text-sm text-slate-600">
                        <LocalTime value={version.createdAt} />
                      </TableCell>
                      <TableCell>
                        <StatusBadge status={state} />
                      </TableCell>
                      <TableCell>
                        <div className="flex items-center gap-1.5">
                          <StatusBadge status={version.action} />
                          <SeverityBadge severity={version.severity} />
                          {version.riskWeight > 0 && (
                            <span className="font-mono text-[11px] text-slate-400">
                              +{version.riskWeight}
                            </span>
                          )}
                        </div>
                      </TableCell>
                      <TableCell>
                        <Button
                          variant="ghost"
                          size="sm"
                          className="h-auto px-0 text-xs text-brand-800 hover:bg-transparent hover:text-brand-900"
                          onClick={() => toggle(version.ruleVersionId)}
                        >
                          {expanded.has(version.ruleVersionId)
                            ? "Hide"
                            : "Show"}
                        </Button>
                      </TableCell>
                      <TableCell>
                        {canDeploy && !isLive && (
                          <Button
                            variant="outline"
                            size="sm"
                            className="gap-1.5"
                            disabled={deploymentInProgress}
                            title={deploymentBlockedReason ?? undefined}
                            onClick={() => onDeploy(version)}
                          >
                            {isRollback ? (
                              <>
                                <Undo2 className="size-4" />
                                {requiresApproval
                                  ? "Submit rollback"
                                  : "Roll back to this"}
                              </>
                            ) : (
                              <>
                                <Rocket className="size-4" />
                                {requiresApproval
                                  ? "Submit for approval"
                                  : "Deploy"}
                              </>
                            )}
                          </Button>
                        )}
                      </TableCell>
                    </TableRow>
                    {expanded.has(version.ruleVersionId) && (
                      <TableRow className="bg-slate-50">
                        <TableCell colSpan={6} className="py-3">
                          <div className="rounded-lg border border-slate-200 bg-card p-3">
                            <RuleBuilder
                              key={version.ruleVersionId}
                              ruleSetId={version.ruleSetId}
                              initialRules={[version.ruleBody]}
                              submitRuleText=""
                              readOnly
                              matchPolarity={version.matchPolarity}
                            />
                          </div>
                          {canWrite && (
                            <div className="mt-2 flex justify-end">
                              <Button
                                variant="outline"
                                size="sm"
                                onClick={() => onCreateVersion(version.ruleBody)}
                              >
                                {t("rules.editAsNewVersion")}
                              </Button>
                            </div>
                          )}
                        </TableCell>
                      </TableRow>
                    )}
                  </React.Fragment>
                );
              })
            )}
          </TableBody>
        </Table>
      </div>

      {versions.length > 0 && (
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
