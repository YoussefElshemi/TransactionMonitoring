"use client";

import { useState, useEffect, useRef, useCallback } from "react";
import { useParams, useRouter, usePathname, useSearchParams } from "next/navigation";
import {
  BackTestResource,
  BackTestResult,
  ListBackTestResult,
  ProblemDetails,
} from "@/types";
import { toast } from "sonner";
import { StatusBadge } from "@/components/StatusBadge";
import { NotFound } from "@/components/NotFound";
import { PayloadView } from "@/components/PayloadView";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import Pagination from "@/components/Pagination";
import { Check, X, ChevronLeft } from "lucide-react";
import Link from "next/link";
import { loginHref } from "@/lib/authRedirect";
import { API_BASE, unwrap } from "@/lib/api";
import { relationText } from "@/lib/resources";

import { apiFetch } from "@/lib/http";

import { LocalTime } from "@/components/LocalTime";
import { useTranslation } from "@/context/LocalizationContext";

const isActive = (status: string) =>
  status === "Pending" || status === "Running";

const BackTestDetail = () => {
  const t = useTranslation();
  const [resource, setResource] = useState<BackTestResource | null>(null);
  const backTest = resource?.backTest ?? null;
  const [loading, setLoading] = useState(true);
  const [notFound, setNotFound] = useState(false);

  const [results, setResults] = useState<BackTestResult[]>([]);
  const [resultsPageNumber, setResultsPageNumber] = useState(1);
  const [resultsTotalPages, setResultsTotalPages] = useState(1);
  const [resultsLoading, setResultsLoading] = useState(false);
  const [changedOnly, setChangedOnly] = useState(false);
  const [changedCount, setChangedCount] = useState<number | null>(null);
  const [expandedPayloads, setExpandedPayloads] = useState<Set<string>>(
    new Set()
  );

  const [pollError, setPollError] = useState(false);
  const intervalRef = useRef<ReturnType<typeof setInterval> | null>(null);

  const router = useRouter();
  const { id } = useParams();
  const pathname = usePathname();
  const searchParams = useSearchParams();

  const redirectUri = searchParams.toString()
    ? `${pathname}?${searchParams.toString()}`
    : pathname;

  const stopPolling = () => {
    if (intervalRef.current) {
      clearInterval(intervalRef.current);
      intervalRef.current = null;
    }
  };

  const fetchBackTest = useCallback(
    async (showLoading = false): Promise<BackTestResource | null> => {
      if (showLoading) setLoading(true);
      try {
        const response = await apiFetch(
          `${API_BASE}/backTests/${id}`,
          { credentials: "include" }
        );
        if (response.ok) {
          const data = await unwrap<BackTestResource>(response);
          setResource(data);
          setPollError(false);
          if (!isActive(data.backTest.status)) stopPolling();
          return data;
        } else if (response.status === 404) {
          setNotFound(true);
          stopPolling();
        } else if (response.status === 401) {
          toast.error(t("common.pleaseLoginAccessResource"), {
            id: "unauthorized",
          });
          router.push(loginHref(redirectUri));
          stopPolling();
        } else {
          const e = (await response.json()) as ProblemDetails;
          toast.error(e.detail || t("backTests.failedLoadBacktest"), {
            id: "error-fetching",
          });
          router.push("/");
          stopPolling();
        }
      } catch {
        setPollError(true);
        stopPolling();
      } finally {
        if (showLoading) setLoading(false);
      }
      return null;
    },
    [id, router, redirectUri]
  );

  const startPolling = useCallback(() => {
    setPollError(false);
    stopPolling();
    intervalRef.current = setInterval(async () => {
      try {
        const response = await apiFetch(
          `${API_BASE}/backTests/${id}`,
          { credentials: "include" }
        );
        if (response.ok) {
          const data = await unwrap<BackTestResource>(response);
          setResource(data);
          setPollError(false);
          if (!isActive(data.backTest.status)) {
            stopPolling();
            if (data.backTest.status === "Completed") {
              fetchResults(1, false);
              fetchChangedCount();
            }
          }
        } else {
          setPollError(true);
          stopPolling();
        }
      } catch {
        setPollError(true);
        stopPolling();
      }
    }, 4000);
  }, [id]);

  const fetchResults = useCallback(
    async (pageNumber: number, changedOnly: boolean) => {
      setResultsLoading(true);
      try {
        const params = new URLSearchParams({
          pageNumber: String(pageNumber),
          pageSize: "10",
        });
        if (changedOnly) params.set("changedOnly", "true");
        const response = await apiFetch(
          `${API_BASE}/backTests/${id}/results?${params}`,
          { credentials: "include" }
        );
        if (response.ok) {
          const data = (await response.json()) as ListBackTestResult;
          setResults(data.data);
          setResultsTotalPages(data.metadata.pageCount);
        } else if (response.status === 401) {
          toast.error(t("common.pleaseLoginAccessResource"), {
            id: "unauthorized",
          });
          router.push(loginHref(redirectUri));
        } else {
          const e = (await response.json()) as ProblemDetails;
          toast.error(e.detail || t("backTests.failedLoadResults"), {
            id: "error-results",
          });
        }
      } catch {
        toast.error(t("backTests.failedLoadResults"), { id: "error-results" });
      } finally {
        setResultsLoading(false);
      }
    },
    [id, router, redirectUri]
  );

  const fetchChangedCount = useCallback(async () => {
    try {
      const response = await apiFetch(
        `${API_BASE}/backTests/${id}/results?changedOnly=true&pageSize=1&pageNumber=1`,
        { credentials: "include" }
      );
      if (response.ok) {
        const data = (await response.json()) as ListBackTestResult;
        setChangedCount(data.metadata.totalCount);
      }
    } catch {
    }
  }, [id]);

  const togglePayload = (resultId: string) => {
    setExpandedPayloads((prev) => {
      const next = new Set(prev);
      if (next.has(resultId)) next.delete(resultId);
      else next.add(resultId);
      return next;
    });
  };

  useEffect(() => {
    const init = async () => {
      const data = await fetchBackTest(true);
      if (data && isActive(data.backTest.status)) {
        startPolling();
      } else if (data?.backTest.status === "Completed") {
        fetchResults(1, false);
        fetchChangedCount();
      }
    };
    init();
    return () => stopPolling();
  }, []);

  useEffect(() => {
    if (backTest?.status === "Completed") {
      fetchResults(resultsPageNumber, changedOnly);
    }
  }, [resultsPageNumber, changedOnly]);

  if (loading)
    return (
      <div className="space-y-4">
        <Skeleton className="h-8 w-48" />
        <Skeleton className="h-32 w-full" />
      </div>
    );
  if (notFound) return <NotFound
        title={t("backTests.backTestNotFound")}
        description={t("backTests.itMayHaveBeenDeleted")}
      />;
  if (!backTest) return null;

  const ruleId = backTest.ruleId;
  const ruleName = relationText(resource?.rule?.friendlyName, ruleId);

  return (
    <div className="space-y-6">
      <div>
        <Link
          href={`/rules/${ruleId}`}
          className="mb-3 inline-flex items-center gap-1 text-sm text-slate-500 hover:text-slate-900"
        >
          <ChevronLeft className="size-4" />
          {ruleName}
        </Link>
        <div className="flex items-start justify-between gap-4">
          <div className="min-w-0">
            <p className="font-mono text-xs uppercase tracking-widest text-slate-400">
              back-test
            </p>
            <h1 className="mt-1 font-mono text-xl font-medium tracking-tight text-slate-900">
              {ruleName}
            </h1>
          </div>
          <StatusBadge status={backTest.status} />
        </div>
        <div className="mt-2 flex flex-wrap gap-x-6 gap-y-1 font-mono text-xs text-slate-400">
          <span>created <LocalTime value={backTest.createdAt} /></span>
          <span>
            completed{" "}
            {backTest.completedAt
              ? <LocalTime value={backTest.completedAt} />
              : "—"}
          </span>
        </div>
      </div>

      {isActive(backTest.status) && (
        <div className="space-y-3 rounded-xl border border-slate-200 bg-card p-4">
          <span className="font-mono text-xs tabular-nums text-slate-500">
            {backTest.status === "Pending"
              ? "waiting to start…"
              : `${backTest.processedCount} / ${backTest.totalEvaluations} evaluated`}
          </span>
          <div className="w-full rounded-full bg-slate-100 h-2">
            <div
              className="h-2 rounded-full bg-brand-600 transition-all duration-500"
              style={{
                width:
                  backTest.totalEvaluations > 0
                    ? `${(backTest.processedCount / backTest.totalEvaluations) * 100}%`
                    : "0%",
              }}
            />
          </div>
          {pollError && (
            <div className="flex items-center gap-3">
              <span className="text-sm text-critical-700">{t("backTests.pollingFailed")}</span>
              <Button
                variant="outline"
                size="sm"
                onClick={() => {
                  fetchBackTest(false).then((data) => {
                    if (data && isActive(data.backTest.status)) startPolling();
                  });
                }}
              >
                {t("backTests.retry")}
              </Button>
            </div>
          )}
        </div>
      )}

      {backTest.status === "Failed" && (
        <div className="rounded-xl border border-critical-200 bg-critical-50 p-4">
          <p className="text-sm text-critical-700">
            This back-test failed before completing. Start another from the
            rule to try again.
          </p>
        </div>
      )}

      {backTest.status === "Completed" && (
        <div className="space-y-4">
          <div className="flex items-center justify-between">
            <div className="flex gap-6 text-sm text-slate-600">
              <span>
                <strong>{backTest.totalEvaluations}</strong> total evaluations
              </span>
              {changedCount !== null && (
                <>
                  <span>
                    <strong className="text-attention-700">{changedCount}</strong>{" "}
                    changed
                  </span>
                  <span>
                    <strong className="text-settled-700">
                      {backTest.totalEvaluations - changedCount}
                    </strong>{" "}
                    unchanged
                  </span>
                </>
              )}
            </div>
            <label className="flex cursor-pointer items-center gap-2 font-mono text-sm text-slate-600">
              <input
                type="checkbox"
                checked={changedOnly}
                onChange={(e) => {
                  setChangedOnly(e.target.checked);
                  setResultsPageNumber(1);
                }}
                className="h-4 w-4 rounded border-slate-300 accent-brand-800"
              />
              {t("backTests.showChangedOnly")}
            </label>
          </div>

          <div className="overflow-hidden rounded-xl border border-slate-200 bg-card">
            <Table>
              <TableHeader>
                <TableRow className="bg-slate-50 hover:bg-slate-50">
                  <TableHead>
                    {t("backTests.evaluationId")}
                  </TableHead>
                  <TableHead>
                    {t("backTests.payload")}
                  </TableHead>
                  <TableHead>
                    {t("backTests.original")}
                  </TableHead>
                  <TableHead>
                    {t("backTests.newResult")}
                  </TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {resultsLoading ? (
                  Array.from({ length: 5 }).map((_, i) => (
                    <TableRow key={i}>
                      {Array.from({ length: 4 }).map((_, j) => (
                        <TableCell key={j}>
                          <Skeleton className="h-4 w-full" />
                        </TableCell>
                      ))}
                    </TableRow>
                  ))
                ) : results.length === 0 ? (
                  <TableRow>
                    <TableCell
                      colSpan={4}
                      className="py-14 text-center font-mono text-xs uppercase tracking-widest text-slate-400"
                    >
                      {t("common.noResults")}
                    </TableCell>
                  </TableRow>
                ) : (
                  results.map((result) => (
                    <TableRow
                      key={result.backTestResultId}
                      className={
                        result.hasChanged
                          ? "bg-attention-50 hover:bg-attention-50/80"
                          : "hover:bg-slate-50/50"
                      }
                    >
                      <TableCell className="font-mono text-xs text-slate-600">
                        {result.ruleEvaluationId.slice(0, 8)}…
                      </TableCell>
                      <TableCell>
                        <Button
                          variant="ghost"
                          size="sm"
                          className="h-auto px-0 text-xs text-brand-800 hover:bg-transparent hover:text-brand-900"
                          onClick={() => togglePayload(result.backTestResultId)}
                        >
                          {expandedPayloads.has(result.backTestResultId)
                            ? "Hide"
                            : "Show"}
                        </Button>
                        {expandedPayloads.has(result.backTestResultId) && (
                          <div className="mt-2">
                            <PayloadView
                              payload={result.payload}
                              masked={result.payloadMasked}
                            />
                          </div>
                        )}
                      </TableCell>
                      <TableCell>
                        {result.originalMatched ? (
                          <Check className="size-4 text-settled-700" />
                        ) : (
                          <X className="size-4 text-critical-700" />
                        )}
                      </TableCell>
                      <TableCell>
                        {result.newMatched ? (
                          <Check className="size-4 text-settled-700" />
                        ) : (
                          <X className="size-4 text-critical-700" />
                        )}
                      </TableCell>
                    </TableRow>
                  ))
                )}
              </TableBody>
            </Table>
          </div>

          {results.length > 0 && (
            <Pagination
              handlePrevPage={() =>
                setResultsPageNumber((p) => Math.max(1, p - 1))
              }
              pageNumber={resultsPageNumber}
              totalPages={resultsTotalPages}
              handleNextPage={() =>
                setResultsPageNumber((p) => Math.min(resultsTotalPages, p + 1))
              }
            />
          )}
        </div>
      )}
    </div>
  );
};

export default BackTestDetail;
