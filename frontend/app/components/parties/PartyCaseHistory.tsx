"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import Link from "next/link";
import { toast } from "sonner";
import { ArrowRight } from "lucide-react";

import { Case, CaseResource } from "@/types";
import { API_BASE, reportApiError, unwrapPage } from "@/lib/api";
import {
  CASE_STATUSES,
  DISPOSITIONS,
  caseStatusLabel,
  dispositionLabelKey,
  isTerminalCase,
} from "@/lib/cases";
import { DispositionBadge } from "@/components/DispositionBadge";
import { SeverityBadge } from "@/components/SeverityBadge";
import { StatusBadge } from "@/components/StatusBadge";
import { LocalTime } from "@/components/LocalTime";
import Pagination from "@/components/Pagination";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Skeleton } from "@/components/ui/skeleton";

import { apiFetch } from "@/lib/http";
import { useTranslation } from "@/context/LocalizationContext";

const ANY = "All";

interface Summary {
  total: number;
  closed: number;
  byDisposition: Map<string, number>;
}

const summarise = (cases: Case[]): Summary => {
  const byDisposition = new Map<string, number>();
  let closed = 0;

  for (const subject of cases) {
    if (isTerminalCase(subject.status)) closed += 1;
    if (subject.disposition) {
      byDisposition.set(
        subject.disposition,
        (byDisposition.get(subject.disposition) ?? 0) + 1
      );
    }
  }

  return { total: cases.length, closed, byDisposition };
};

export function PartyCaseHistory({
  partyId,
  excludeCaseId,
  onUnauthorized,
  pageSize = 25,
}: {
  partyId: string;
  excludeCaseId?: string;
  onUnauthorized: () => void;
  pageSize?: number;
}) {
  const t = useTranslation();
  const [cases, setCases] = useState<CaseResource[]>([]);
  const [status, setStatus] = useState(ANY);
  const [disposition, setDisposition] = useState(ANY);
  const [pageNumber, setPageNumber] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(true);

  const fetchCases = useCallback(async () => {
    setLoading(true);

    const params = new URLSearchParams({
      PartyId: partyId,
      PageNumber: String(pageNumber),
      PageSize: String(pageSize),
    });
    if (status !== ANY) params.set("Status", status);
    if (disposition !== ANY) params.set("Disposition", disposition);

    try {
      const response = await apiFetch(`${API_BASE}/cases?${params}`, {
        method: "GET",
        headers: { "Content-Type": "application/json" },
        credentials: "include",
      });

      if (response.ok) {
        const page = await unwrapPage<CaseResource>(response);
        setCases(page.data);
        setTotalPages(page.metadata.pageCount || 1);
        setTotalCount(page.metadata.totalCount);
      } else {
        setCases([]);
        await reportApiError(response, {
          fallback: t("parties.errorOccurredFetchingPartyS"),
          onUnauthorized,
          toastId: "error-party-case-history",
        });
      }
    } catch {
      setCases([]);
      toast.error(t("parties.errorOccurredFetchingPartyS"), {
        id: "error-party-case-history",
      });
    } finally {
      setLoading(false);
    }
  }, [partyId, status, disposition, pageNumber, pageSize, onUnauthorized]);

  useEffect(() => {
    fetchCases();
  }, [fetchCases]);

  const visible = useMemo(
    () =>
      excludeCaseId
        ? cases.filter((resource) => resource.case.caseId !== excludeCaseId)
        : cases,
    [cases, excludeCaseId]
  );

  const summary = useMemo(
    () => summarise(visible.map((resource) => resource.case)),
    [visible]
  );

  const partial = totalPages > 1;
  const filtered = status !== ANY || disposition !== ANY;

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <p className="max-w-prose text-sm text-slate-500">
          Every case opened over this party, closed ones included — what was
          concluded before is most of what makes this history worth reading.
        </p>
        <div className="flex flex-wrap items-center gap-2">
          <Select
            value={status}
            onValueChange={(value) => {
              setStatus(value);
              setPageNumber(1);
            }}
          >
            <SelectTrigger className="w-40" aria-label={t("parties.filterByStatus")}>
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value={ANY}>{t("filter.anyStatus")}</SelectItem>
              {CASE_STATUSES.map((candidate) => (
                <SelectItem key={candidate} value={candidate}>
                  {caseStatusLabel(candidate)}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
          <Select
            value={disposition}
            onValueChange={(value) => {
              setDisposition(value);
              setPageNumber(1);
            }}
          >
            <SelectTrigger className="w-44" aria-label={t("parties.filterByDisposition")}>
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value={ANY}>{t("parties.anyConclusion")}</SelectItem>
              {DISPOSITIONS.map((candidate) => (
                <SelectItem key={candidate} value={candidate}>
                  {t(dispositionLabelKey(candidate))}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
      </div>

      {!loading && visible.length > 0 && (
        <div className="rounded-xl border border-slate-200 bg-slate-50/50 p-4">
          <div className="flex flex-wrap items-baseline gap-x-6 gap-y-2">
            <span className="font-mono text-sm text-slate-900">
              <span className="text-lg font-medium">{totalCount}</span>{" "}
              <span className="text-xs uppercase tracking-widest text-slate-400">
                {totalCount === 1 ? "case" : "cases"}
              </span>
            </span>
            <span className="font-mono text-sm text-slate-900">
              <span className="text-lg font-medium">{summary.closed}</span>{" "}
              <span className="text-xs uppercase tracking-widest text-slate-400">
                closed
              </span>
            </span>
            <div className="flex flex-wrap items-center gap-2">
              {summary.byDisposition.size === 0 ? (
                <span className="text-xs text-slate-400">
                  {t("parties.nothingConcludedYet")}
                </span>
              ) : (
                [...summary.byDisposition.entries()]
                  .sort((left, right) => right[1] - left[1])
                  .map(([name, count]) => (
                    <span
                      key={name}
                      className="inline-flex items-center gap-1.5"
                    >
                      <DispositionBadge disposition={name} />
                      <span className="font-mono text-xs text-slate-500">
                        ×{count}
                      </span>
                    </span>
                  ))
              )}
            </div>
          </div>
          {partial && (
            <p className="mt-2 text-xs text-slate-400">
              The count is every case; the closures and conclusions are this
              page of {visible.length}. Filter by conclusion to count across all
              of them.
            </p>
          )}
        </div>
      )}

      <div className="space-y-2">
        {loading ? (
          Array.from({ length: 3 }).map((_, index) => (
            <Skeleton key={index} className="h-16 w-full" />
          ))
        ) : visible.length === 0 ? (
          <div className="rounded-xl border border-slate-200 bg-card py-12 text-center">
            <p className="text-sm text-slate-500">
              {filtered
                ? "No case matches that"
                : excludeCaseId
                ? "No other case has been opened over this party"
                : "No case has been opened over this party"}
            </p>
            {!filtered && (
              <p className="mt-1 text-xs text-slate-400">
                {t("parties.thisIsTheFirstTime")}
              </p>
            )}
          </div>
        ) : (
          visible.map(({ case: subject }) => (
            <Link
              key={subject.caseId}
              href={`/cases/${subject.caseId}`}
              className="flex flex-wrap items-center justify-between gap-x-4 gap-y-2 rounded-md border border-slate-200 bg-card p-4 transition-colors hover:border-slate-300 hover:bg-slate-50/50"
            >
              <div className="min-w-0 flex-1">
                <span className="text-sm font-medium text-slate-900">
                  {subject.friendlyName}
                </span>
                <p className="mt-0.5 text-xs text-slate-400">
                  opened <LocalTime value={subject.createdAt} />
                  {subject.resolvedAt && (
                    <>
                      {" · "}
                      {subject.status === "Expired" ? "expired " : "closed "}
                      <LocalTime value={subject.resolvedAt} />
                    </>
                  )}
                </p>
              </div>
              <div className="flex flex-shrink-0 flex-wrap items-center gap-2">
                <SeverityBadge severity={subject.severity} />
                <StatusBadge status={subject.status} />
                {subject.disposition ? (
                  <DispositionBadge disposition={subject.disposition} />
                ) : (
                  isTerminalCase(subject.status) && (
                    <span className="text-[11px] text-slate-400">
                      no conclusion recorded
                    </span>
                  )
                )}
                <ArrowRight className="size-4 text-slate-300" />
              </div>
            </Link>
          ))
        )}
      </div>

      {visible.length > 0 && totalPages > 1 && (
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
