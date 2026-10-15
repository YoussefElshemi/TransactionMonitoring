"use client";

import { useCallback, useEffect, useState } from "react";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import { format, parseISO } from "date-fns";
import type { DateRange } from "react-day-picker";
import { toast } from "sonner";

import { APPROVAL_STATUSES, APPROVAL_SUBJECTS, ApprovalResource } from "@/types";
import { API_BASE, reportApiError, unwrapPage } from "@/lib/api";
import { loginHref } from "@/lib/authRedirect";
import {
  ANY_APPROVAL_STATUS,
  ANY_APPROVAL_SUBJECT,
  ApprovalQuery,
  approvalParams,
  approvalSearch,
  approvalStatusLabel,
  approvalSubjectLabel,
} from "@/lib/approvals";
import { ApprovalTable } from "@/components/approvals/ApprovalTable";
import { DateRangePicker } from "@/components/DateRangePicker";
import { PageHeader } from "@/components/PageHeader";
import Pagination from "@/components/Pagination";
import { TabBar } from "@/components/TabBar";
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

const asDateParam = (date: Date): string => format(date, "yyyy-MM-dd");

const fromDateParam = (value: string | null): Date | undefined =>
  value ? parseISO(value) : undefined;

const STATUS_TABS = [ANY_APPROVAL_STATUS, ...APPROVAL_STATUSES];

export function ApprovalsScreen({
  endpoint,
  eyebrow,
  title,
  description,
  showRequester = true,
  emptyMessage,
}: {
  endpoint: string;
  eyebrow: string;
  title: string;
  description: string;
  showRequester?: boolean;
  emptyMessage: (status: string) => React.ReactNode;
}) {
  const t = useTranslation();
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();

  const redirectUri = searchParams.toString()
    ? `${pathname}?${searchParams.toString()}`
    : pathname;

  const [approvals, setApprovals] = useState<ApprovalResource[]>([]);
  const [pageNumber, setPageNumber] = useState(
    parseInt(searchParams.get("pageNumber") || "1")
  );
  const [pageSize, setPageSize] = useState(
    parseInt(searchParams.get("pageSize") || "10")
  );
  const [status, setStatus] = useState(
    searchParams.get("status") || "Pending"
  );
  const [subject, setSubject] = useState(
    searchParams.get("subject") || ANY_APPROVAL_SUBJECT
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

  const query: ApprovalQuery = {
    pageNumber,
    pageSize,
    status,
    subject,
    fromDate,
    toDate,
  };

  const goToLogin = useCallback(() => {
    router.push(loginHref(redirectUri));
  }, [router, redirectUri]);

  const fetchApprovals = useCallback(async () => {
    setLoading(true);

    const params = approvalParams({
      pageNumber,
      pageSize,
      status,
      subject,
      fromDate,
      toDate,
    });

    try {
      const response = await apiFetch(`${API_BASE}${endpoint}?${params}`, {
        method: "GET",
        headers: { "Content-Type": "application/json" },
        credentials: "include",
      });

      if (response.ok) {
        const page = await unwrapPage<ApprovalResource>(response);
        setApprovals(page.data);
        setTotalPages(page.metadata.pageCount || 1);
        setTotalCount(page.metadata.totalCount);
      } else {
        setApprovals([]);
        await reportApiError(response, {
          fallback: t("approvals.errorOccurredFetchingApprovals"),
          onUnauthorized: goToLogin,
          toastId: "error-fetching-approvals",
        });
      }
    } catch {
      setApprovals([]);
      toast.error(t("approvals.errorOccurredFetchingApprovals"), {
        id: "error-fetching-approvals",
      });
    } finally {
      setLoading(false);
    }
  }, [
    endpoint,
    pageNumber,
    pageSize,
    status,
    subject,
    fromDate,
    toDate,
    goToLogin,
  ]);

  useEffect(() => {
    fetchApprovals();
  }, [fetchApprovals]);

  useEffect(() => {
    router.replace(`?${approvalSearch(query)}`);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [pageNumber, pageSize, status, subject, fromDate, toDate, router]);

  return (
    <div className="space-y-4">
      <PageHeader eyebrow={eyebrow} title={title} description={description} />

      <TabBar
        tabs={STATUS_TABS}
        activeTab={status || ANY_APPROVAL_STATUS}
        label={(tab) =>
          tab === ANY_APPROVAL_STATUS ? tab : approvalStatusLabel(tab)
        }
        onChange={(tab) => {
          setStatus(tab);
          setPageNumber(1);
        }}
      />

      {status === "Withdrawn" && (
        <p className="rounded-md border border-slate-200 bg-slate-50 px-3 py-2 text-xs text-slate-600">
          Nobody turned these down. Each was taken back by whoever raised it,
          usually before a reviewer opened it — which is not the same outcome as
          a rejection and must never be counted as one.
        </p>
      )}

      <div className="flex flex-wrap items-center justify-between gap-3">
        <Select
          value={subject}
          onValueChange={(value) => {
            setSubject(value);
            setPageNumber(1);
          }}
        >
          <SelectTrigger className="w-52">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value={ANY_APPROVAL_SUBJECT}>{t("approvals.anything")}</SelectItem>
            {APPROVAL_SUBJECTS.map((candidate) => (
              <SelectItem key={candidate} value={candidate}>
                {approvalSubjectLabel(candidate)}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>

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

      {!loading && approvals.length > 0 && (
        <p className="font-mono text-xs uppercase tracking-widest text-slate-400">
          {totalCount} {totalCount === 1 ? "request" : "requests"}
          {status !== ANY_APPROVAL_STATUS &&
            ` · ${approvalStatusLabel(status).toLowerCase()}`}
          {" · newest first"}
        </p>
      )}

      <ApprovalTable
        approvals={approvals}
        loading={loading}
        showRequester={showRequester}
        emptyMessage={emptyMessage(status)}
      />

      {approvals.length > 0 && (
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
