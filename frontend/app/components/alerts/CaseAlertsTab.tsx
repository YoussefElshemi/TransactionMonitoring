"use client";

import { useCallback, useEffect, useState } from "react";
import { toast } from "sonner";

import { ALERT_STATUSES, AlertResource } from "@/types";
import { API_BASE, reportApiError, unwrapPage } from "@/lib/api";
import { bySeverityDescending } from "@/lib/alerts";
import { SEVERITY_FILTERS } from "@/lib/severity";
import { AlertTable } from "@/components/alerts/AlertTable";
import Pagination from "@/components/Pagination";
import { PageSizeSelect } from "@/components/PageSizeSelect";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";

import { apiFetch } from "@/lib/http";
import { useTranslation } from "@/context/LocalizationContext";

const ANY_STATUS = "All";
const ANY_SEVERITY = "All";

export function CaseAlertsTab({
  caseId,
  onUnauthorized,
}: {
  caseId: string;
  onUnauthorized: () => void;
}) {
  const t = useTranslation();
  const [alerts, setAlerts] = useState<AlertResource[]>([]);
  const [pageNumber, setPageNumber] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [status, setStatus] = useState(ANY_STATUS);
  const [severity, setSeverity] = useState(ANY_SEVERITY);
  const [totalPages, setTotalPages] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(true);

  const fetchAlerts = useCallback(async () => {
    setLoading(true);

    const params = new URLSearchParams({
      PageNumber: String(pageNumber),
      PageSize: String(pageSize),
    });
    if (status !== ANY_STATUS) params.set("Status", status);
    if (severity !== ANY_SEVERITY) params.set("Severity", severity);

    try {
      const response = await apiFetch(
        `${API_BASE}/cases/${caseId}/alerts?${params}`,
        {
          method: "GET",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
        }
      );

      if (response.ok) {
        const page = await unwrapPage<AlertResource>(response);
        setAlerts(page.data);
        setTotalPages(page.metadata.pageCount || 1);
        setTotalCount(page.metadata.totalCount);
      } else {
        setAlerts([]);
        await reportApiError(response, {
          fallback: t("alerts.errorOccurredFetchingCaseS"),
          onUnauthorized,
          toastId: "error-fetching-case-alerts",
        });
      }
    } catch {
      setAlerts([]);
      toast.error(t("alerts.errorOccurredFetchingCaseS"), {
        id: "error-fetching-case-alerts",
      });
    } finally {
      setLoading(false);
    }
  }, [caseId, pageNumber, pageSize, status, severity, onUnauthorized]);

  useEffect(() => {
    fetchAlerts();
  }, [fetchAlerts]);

  const ordered = bySeverityDescending(
    alerts,
    (resource) => resource.alert.severity
  );

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="flex flex-wrap items-center gap-3">
          <Select
            value={status}
            onValueChange={(value) => {
              setStatus(value);
              setPageNumber(1);
            }}
          >
            <SelectTrigger className="w-44">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value={ANY_STATUS}>{t("filter.anyStatus")}</SelectItem>
              {ALERT_STATUSES.map((candidate) => (
                <SelectItem key={candidate} value={candidate}>
                  {candidate}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
          <Select
            value={severity}
            onValueChange={(value) => {
              setSeverity(value);
              setPageNumber(1);
            }}
          >
            <SelectTrigger className="w-44">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value={ANY_SEVERITY}>{t("filter.anySeverity")}</SelectItem>
              {SEVERITY_FILTERS.map((candidate) => (
                <SelectItem key={candidate} value={candidate}>
                  {candidate}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
        <PageSizeSelect
          value={pageSize}
          onChange={(next) => {
            setPageSize(next);
            setPageNumber(1);
          }}
        />
      </div>

      {!loading && alerts.length > 0 && (
        <p className="font-mono text-xs uppercase tracking-widest text-slate-400">
          {totalCount} {totalCount === 1 ? "alert" : "alerts"}
          {totalPages > 1
            ? " · most severe first on this page"
            : " · most severe first"}
        </p>
      )}

      <AlertTable
        alerts={ordered}
        loading={loading}
        showCase={false}
        emptyMessage={
          status === ANY_STATUS && severity === ANY_SEVERITY
            ? t("alerts.noAlertsCasePredatesAlerts")
            : t("alerts.noAlertsMatch")
        }
      />

      {alerts.length > 0 && (
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
