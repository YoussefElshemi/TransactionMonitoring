"use client";

import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { format } from "date-fns";
import type { DateRange } from "react-day-picker";
import { toast } from "sonner";

import { API_BASE } from "@/lib/api";
import { userName } from "@/lib/resources";
import { AuditLogResource, Paged, ProblemDetails } from "@/types";

import { apiFetch } from "@/lib/http";
import { useTranslation } from "@/context/LocalizationContext";

export const HISTORY_PAGE_SIZE = 10;

interface UseAuditHistoryOptions {
  path: string;
  enabled?: boolean;
  pageSize?: number;
  onUnauthorized: () => void;
}

export interface ActorLookup {
  nameOf: (userId: string) => string | null;
  nameOfApiClient: (apiClientId: string) => string | null;
}

export interface AuditHistoryQuery {
  logs: AuditLogResource[];
  actors: ActorLookup;
  loading: boolean;
  forbidden: boolean;
  failed: boolean;
  pageNumber: number;
  pageCount: number;
  totalCount: number;
  range: DateRange | undefined;
  setRange: (range: DateRange | undefined) => void;
  setPageNumber: (page: number) => void;
  reload: () => void;
}

const asDateParam = (date: Date): string => format(date, "yyyy-MM-dd");

export function useAuditHistory({
  path,
  enabled = true,
  pageSize = HISTORY_PAGE_SIZE,
  onUnauthorized,
}: UseAuditHistoryOptions): AuditHistoryQuery {
  const t = useTranslation();
  const [logs, setLogs] = useState<AuditLogResource[]>([]);
  const [loading, setLoading] = useState<boolean>(false);
  const [forbidden, setForbidden] = useState<boolean>(false);
  const [failed, setFailed] = useState<boolean>(false);
  const [pageNumber, setPageNumber] = useState<number>(1);
  const [pageCount, setPageCount] = useState<number>(1);
  const [totalCount, setTotalCount] = useState<number>(0);
  const [range, setRangeState] = useState<DateRange | undefined>(undefined);
  const [reloadKey, setReloadKey] = useState<number>(0);

  // Filters and paging can change faster than the network answers; only the
  // newest request is allowed to write to state.
  const requestId = useRef<number>(0);

  // Held in a ref so an inline callback from the caller cannot retrigger the
  // fetch on every render.
  const unauthorizedRef = useRef(onUnauthorized);
  unauthorizedRef.current = onUnauthorized;

  const fromDate = range?.from ? asDateParam(range.from) : "";
  const toDate = range?.to ? asDateParam(range.to) : "";

  const setRange = useCallback((next: DateRange | undefined) => {
    setRangeState(next);
    setPageNumber(1);
  }, []);

  const reload = useCallback(() => setReloadKey((key) => key + 1), []);

  useEffect(() => {
    if (!enabled) return;

    const id = ++requestId.current;
    const params = new URLSearchParams({
      pageNumber: String(pageNumber),
      pageSize: String(pageSize),
    });
    if (fromDate) params.set("fromDate", fromDate);
    if (toDate) params.set("toDate", toDate);

    const load = async () => {
      setLoading(true);
      try {
        const response = await apiFetch(`${API_BASE}${path}?${params}`, {
          credentials: "include",
        });
        if (id !== requestId.current) return;

        if (response.ok) {
          const data = (await response.json()) as Paged<AuditLogResource>;
          if (id !== requestId.current) return;
          setLogs(data.data);
          setPageCount(data.metadata.pageCount);
          setTotalCount(data.metadata.totalCount);
          setForbidden(false);
          setFailed(false);
        } else if (response.status === 401) {
          toast.error(t("common.pleaseLoginAccessResource"), {
            id: "unauthorized",
          });
          unauthorizedRef.current();
        } else if (response.status === 403) {
          setLogs([]);
          setForbidden(true);
          setFailed(false);
        } else {
          const error = (await response.json().catch(() => null)) as
            | ProblemDetails
            | null;
          setLogs([]);
          setFailed(true);
          toast.error(error?.detail || t("history.failedLoadHistory"), {
            id: "error-history",
          });
        }
      } catch {
        if (id !== requestId.current) return;
        setLogs([]);
        setFailed(true);
        toast.error(t("history.failedLoadHistory"), { id: "error-history" });
      } finally {
        if (id === requestId.current) setLoading(false);
      }
    };

    load();
  }, [path, enabled, pageSize, pageNumber, fromDate, toDate, reloadKey]);

  const actors = useMemo<ActorLookup>(() => {
    const users = new Map<string, string>();
    const apiClients = new Map<string, string>();

    for (const { auditLog, changedByUser, changedByApiClient } of logs) {
      if (auditLog.changedByUserId && changedByUser) {
        const name = userName(changedByUser);
        if (name) users.set(auditLog.changedByUserId, name);
      }
      if (auditLog.changedByApiClientId && changedByApiClient) {
        const name =
          changedByApiClient.friendlyName || changedByApiClient.clientId;
        if (name) apiClients.set(auditLog.changedByApiClientId, name);
      }
    }

    return {
      nameOf: (userId) => users.get(userId) ?? null,
      nameOfApiClient: (apiClientId) => apiClients.get(apiClientId) ?? null,
    };
  }, [logs]);

  return {
    logs,
    actors,
    loading,
    forbidden,
    failed,
    pageNumber,
    pageCount,
    totalCount,
    range,
    setRange,
    setPageNumber,
    reload,
  };
}
