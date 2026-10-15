"use client";

import { useCallback, useEffect, useState } from "react";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import { format, parseISO } from "date-fns";
import type { DateRange } from "react-day-picker";
import { toast } from "sonner";
import { AlertTriangle, ArrowUpNarrowWide } from "lucide-react";

import {
  SUSPICIOUS_ACTIVITY_REPORT_STATUSES,
  SuspiciousActivityReportResource,
} from "@/types";
import { API_BASE, reportApiError, unwrapPage } from "@/lib/api";
import { loginHref } from "@/lib/authRedirect";
import {
  ANY_FILING_STATUS,
  FilingQuery,
  filingParams,
  filingSearch,
  filingStatusLabel,
  filingStatusText,
} from "@/lib/suspiciousActivityReports";
import { useOverdueFilingCount } from "@/lib/useSuspiciousActivityReports";
import { cn } from "@/lib/utils";
import { FilingTable } from "@/components/suspiciousActivityReports/FilingTable";
import { DateRangePicker } from "@/components/DateRangePicker";
import { PageHeader } from "@/components/PageHeader";
import Pagination from "@/components/Pagination";
import { TabBar } from "@/components/TabBar";
import { Button } from "@/components/ui/button";
import { PageSizeSelect } from "@/components/PageSizeSelect";

import { apiFetch } from "@/lib/http";
import { useTranslation } from "@/context/LocalizationContext";

const asDateParam = (date: Date): string => format(date, "yyyy-MM-dd");

const fromDateParam = (value: string | null): Date | undefined =>
  value ? parseISO(value) : undefined;

const STATUS_TABS = [
  ANY_FILING_STATUS,
  ...SUSPICIOUS_ACTIVITY_REPORT_STATUSES,
];

const SuspiciousActivityReports = () => {
  const t = useTranslation();
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();

  const redirectUri = searchParams.toString()
    ? `${pathname}?${searchParams.toString()}`
    : pathname;

  const [reports, setReports] = useState<SuspiciousActivityReportResource[]>(
    []
  );
  const [pageNumber, setPageNumber] = useState(
    parseInt(searchParams.get("pageNumber") || "1")
  );
  const [pageSize, setPageSize] = useState(
    parseInt(searchParams.get("pageSize") || "25")
  );
  const [status, setStatus] = useState(
    searchParams.get("status") || ANY_FILING_STATUS
  );
  const [overdueOnly, setOverdueOnly] = useState(
    searchParams.get("isOverdue") === "true"
  );
  const [range, setRange] = useState<DateRange | undefined>(() => {
    const from = fromDateParam(searchParams.get("fromDate"));
    return from
      ? { from, to: fromDateParam(searchParams.get("toDate")) }
      : undefined;
  });
  const [totalPages, setTotalPages] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(true);

  const fromDate = range?.from ? asDateParam(range.from) : "";
  const toDate = range?.to ? asDateParam(range.to) : "";

  const query: FilingQuery = {
    pageNumber,
    pageSize,
    status,
    overdueOnly,
    fromDate,
    toDate,
  };

  const overdueCount = useOverdueFilingCount(true);

  const goToLogin = useCallback(() => {
    router.push(loginHref(redirectUri));
  }, [router, redirectUri]);

  const fetchReports = useCallback(async () => {
    setLoading(true);

    const params = filingParams({
      pageNumber,
      pageSize,
      status,
      overdueOnly,
      fromDate,
      toDate,
    });

    try {
      const response = await apiFetch(
        `${API_BASE}/suspicious-activity-reports?${params}`,
        {
          method: "GET",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
        }
      );

      if (response.ok) {
        const page = await unwrapPage<SuspiciousActivityReportResource>(
          response
        );
        setReports(page.data);
        setTotalPages(page.metadata.pageCount || 1);
        setTotalCount(page.metadata.totalCount);
      } else {
        setReports([]);
        await reportApiError(response, {
          fallback: t("suspiciousActivityReports.errorOccurredFetchingFilings"),
          onUnauthorized: goToLogin,
          toastId: "error-fetching-filings",
        });
      }
    } catch {
      setReports([]);
      toast.error(t("suspiciousActivityReports.errorOccurredFetchingFilings"), {
        id: "error-fetching-filings",
      });
    } finally {
      setLoading(false);
    }
  }, [pageNumber, pageSize, status, overdueOnly, fromDate, toDate, goToLogin]);

  useEffect(() => {
    fetchReports();
  }, [fetchReports]);

  useEffect(() => {
    router.replace(`?${filingSearch(query)}`);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [pageNumber, pageSize, status, overdueOnly, fromDate, toDate, router]);

  const statusMeaning =
    status === ANY_FILING_STATUS ? null : filingStatusText(status);

  return (
    <div className="space-y-4">
      <PageHeader
        eyebrow={t("suspiciousActivityReports.filings")}
        title={t("suspiciousActivityReports.suspiciousActivityReports")}
        description={t("suspiciousActivityReports.oneReportPerCaseOpened")}
      />

      <OverdueSummary
        count={overdueCount}
        active={overdueOnly}
        onShow={() => {
          setOverdueOnly(true);
          setStatus(ANY_FILING_STATUS);
          setPageNumber(1);
        }}
        onClear={() => {
          setOverdueOnly(false);
          setPageNumber(1);
        }}
      />

      <TabBar
        tabs={STATUS_TABS}
        activeTab={status}
        label={(tab) =>
          tab === ANY_FILING_STATUS ? "All" : filingStatusLabel(tab)
        }
        onChange={(tab) => {
          setStatus(tab);
          setPageNumber(1);
        }}
      />

      {statusMeaning && (
        <p className="rounded-md border border-slate-200 bg-slate-50 px-3 py-2 text-xs text-slate-600">
          {statusMeaning}
        </p>
      )}

      <div className="flex flex-wrap items-center justify-between gap-3">
        <p
          className="inline-flex items-center gap-1.5 font-mono text-xs uppercase tracking-widest text-slate-400"
          title={t("suspiciousActivityReports.filingsWhoseDeadlineIsStill")}
        >
          <ArrowUpNarrowWide className="size-3.5" />
          {t("suspiciousActivityReports.stillRunningFirstSoonestDeadline")}
        </p>

        <div className="flex items-center gap-3">
          <DateRangePicker
            value={range}
            onChange={(next) => {
              setRange(next);
              setPageNumber(1);
            }}
          />
          <PageSizeSelect
            value={pageSize}
            onChange={(next) => {
              setPageSize(next);
              setPageNumber(1);
            }}
          />
        </div>
      </div>

      {!loading && reports.length > 0 && (
        <p className="font-mono text-xs uppercase tracking-widest text-slate-400">
          {totalCount} {totalCount === 1 ? "filing" : "filings"}
          {status !== ANY_FILING_STATUS &&
            ` · ${filingStatusLabel(status).toLowerCase()}`}
          {overdueOnly && " · overdue only"}
        </p>
      )}

      <FilingTable
        reports={reports}
        loading={loading}
        emptyMessage={
          overdueOnly
            ?
              t("suspiciousActivityReports.nothingPastDeadline")
            : status === ANY_FILING_STATUS
              ? t("suspiciousActivityReports.noFilingsHaveBeenOpened")
              : `No filings are ${filingStatusLabel(status).toLowerCase()}`
        }
      />

      {reports.length > 0 && (
        <Pagination
          handlePrevPage={() => setPageNumber((page) => Math.max(1, page - 1))}
          pageNumber={pageNumber}
          totalPages={totalPages}
          handleNextPage={() =>
            setPageNumber((page) => Math.min(totalPages, page + 1))
          }
        />
      )}
    </div>
  );
};

function OverdueSummary({
  count,
  active,
  onShow,
  onClear,
}: {
  count: number | null;
  active: boolean;
  onShow: () => void;
  onClear: () => void;
}) {
  const t = useTranslation();
  if (count === null) return null;

  if (count === 0) {
    return (
      <div className="flex flex-wrap items-center gap-3 rounded-lg border border-settled-200 bg-settled-50 px-4 py-3">
        <p className="text-sm text-settled-700">
          {t("suspiciousActivityReports.nothingIsPastItsFiling")}
        </p>
        {active && (
          <Button variant="outline" size="sm" onClick={onClear}>
            {t("suspiciousActivityReports.showEveryFiling")}
          </Button>
        )}
      </div>
    );
  }

  return (
    <div
      className={cn(
        "flex flex-wrap items-center gap-x-4 gap-y-2 rounded-lg border p-4",
        "border-critical-200 bg-critical-50"
      )}
    >
      <span className="inline-flex items-center gap-2 rounded-md border border-critical-solid bg-critical-solid px-2.5 py-1 font-mono text-sm font-medium text-critical-solid-foreground">
        <AlertTriangle className="size-4 flex-shrink-0" />
        {count}
      </span>
      <p className="max-w-prose text-sm text-critical-700">
        {count === 1 ? "One filing is" : `${count} filings are`} past the
        deadline for reporting to the financial intelligence unit. Each one is
        either a report that has to go out or a decision not to file that has to
        be recorded.
      </p>
      {active ? (
        <Button
          variant="outline"
          size="sm"
          className="ml-auto"
          onClick={onClear}
        >
          {t("suspiciousActivityReports.showEveryFiling")}
        </Button>
      ) : (
        <Button size="sm" className="ml-auto" onClick={onShow}>
          {t("suspiciousActivityReports.showOnlyThese")}
        </Button>
      )}
    </div>
  );
}

export default SuspiciousActivityReports;
