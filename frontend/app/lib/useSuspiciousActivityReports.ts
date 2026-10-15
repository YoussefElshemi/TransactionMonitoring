"use client";

import { useCallback, useEffect, useState } from "react";

import { SuspiciousActivityReportResource } from "@/types";
import { API_BASE, unwrap, unwrapPage } from "@/lib/api";

import { apiFetch } from "@/lib/http";

export const OVERDUE_FILING_POLL_MS = 30_000;

export const useOverdueFilingCount = (enabled: boolean): number | null => {
  const [count, setCount] = useState<number | null>(null);

  useEffect(() => {
    if (!enabled) {
      setCount(null);
      return;
    }

    let live = true;

    const read = async () => {
      try {
        const response = await apiFetch(
          `${API_BASE}/suspicious-activity-reports?isOverdue=true&pageSize=1`,
          {
            method: "GET",
            headers: { "Content-Type": "application/json" },
            credentials: "include",
          }
        );
        if (!response.ok) return;
        const page = await unwrapPage<SuspiciousActivityReportResource>(
          response
        );
        if (live) setCount(page.metadata.totalCount);
      } catch {
      }
    };

    read();
    const timer = setInterval(read, OVERDUE_FILING_POLL_MS);

    return () => {
      live = false;
      clearInterval(timer);
    };
  }, [enabled]);

  return count;
};

export const readCaseFiling = async (
  caseId: string
): Promise<SuspiciousActivityReportResource | null> => {
  try {
    const response = await apiFetch(
      `${API_BASE}/cases/${caseId}/suspicious-activity-report`,
      {
        method: "GET",
        headers: { "Content-Type": "application/json" },
        credentials: "include",
      }
    );
    if (!response.ok) return null;
    return await unwrap<SuspiciousActivityReportResource>(response);
  } catch {
    return null;
  }
};

export const useCaseFiling = (
  caseId: string | null | undefined
): {
  report: SuspiciousActivityReportResource | null;
  loading: boolean;
  refresh: () => void;
} => {
  const [report, setReport] = useState<SuspiciousActivityReportResource | null>(
    null
  );
  const [loading, setLoading] = useState(true);
  const [reloadKey, setReloadKey] = useState(0);

  const refresh = useCallback(() => setReloadKey((key) => key + 1), []);

  useEffect(() => {
    if (!caseId) {
      setReport(null);
      setLoading(false);
      return;
    }

    let live = true;
    setLoading(true);

    void (async () => {
      const found = await readCaseFiling(caseId);
      if (!live) return;
      setReport(found);
      setLoading(false);
    })();

    return () => {
      live = false;
    };
  }, [caseId, reloadKey]);

  return { report, loading, refresh };
};
