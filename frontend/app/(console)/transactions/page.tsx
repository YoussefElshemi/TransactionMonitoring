"use client";

import { useState, useEffect, useCallback } from "react";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import { format, parseISO } from "date-fns";
import type { DateRange } from "react-day-picker";
import { toast } from "sonner";
import { Eye } from "lucide-react";

import { ListTransaction, TransactionSummary } from "@/types";
import { API_BASE, reportApiError } from "@/lib/api";
import { loginHref } from "@/lib/authRedirect";
import Pagination from "@/components/Pagination";
import { PageHeader } from "@/components/PageHeader";
import { DateRangePicker } from "@/components/DateRangePicker";
import { TransactionReference } from "@/components/TransactionReference";
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

import { apiFetch } from "@/lib/http";

import { LocalTime } from "@/components/LocalTime";
import { PageSizeSelect } from "@/components/PageSizeSelect";
import { useTranslation } from "@/context/LocalizationContext";

const asDateParam = (date: Date): string => format(date, "yyyy-MM-dd");

/**
 * `parseISO`, not `new Date`: a bare `YYYY-MM-DD` handed to the constructor is
 * read as UTC midnight, which formats back a day early west of Greenwich and
 * would quietly walk the range backwards on every reload.
 */
const fromDateParam = (value: string | null): Date | undefined =>
  value ? parseISO(value) : undefined;

const Transactions = () => {
  const t = useTranslation();
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const redirectUri = searchParams.toString()
    ? `${pathname}?${searchParams.toString()}`
    : pathname;

  const [transactions, setTransactions] = useState<TransactionSummary[]>([]);
  const [pageNumber, setPageNumber] = useState(
    parseInt(searchParams.get("pageNumber") || "1")
  );
  const [pageSize, setPageSize] = useState(
    parseInt(searchParams.get("pageSize") || "10")
  );
  const [reference, setReference] = useState(
    searchParams.get("externalReference") || ""
  );
  const [friendlyName, setFriendlyName] = useState(
    searchParams.get("friendlyName") || ""
  );
  const [range, setRange] = useState<DateRange | undefined>(() => {
    const from = fromDateParam(searchParams.get("fromDate"));
    return from ? { from, to: fromDateParam(searchParams.get("toDate")) } : undefined;
  });
  const [totalPages, setTotalPages] = useState(1);
  const [loading, setLoading] = useState(true);

  const fromDate = range?.from ? asDateParam(range.from) : "";
  const toDate = range?.to ? asDateParam(range.to) : "";

  const goToLogin = useCallback(() => {
    router.push(loginHref(redirectUri));
  }, [router, redirectUri]);

  const fetchTransactions = useCallback(async () => {
    setLoading(true);

    const params = new URLSearchParams({
      pageNumber: String(pageNumber),
      pageSize: String(pageSize),
    });
    if (reference) params.set("externalReference", reference);
    if (friendlyName) params.set("friendlyName", friendlyName);
    if (fromDate) params.set("fromDate", fromDate);
    if (toDate) params.set("toDate", toDate);

    try {
      const response = await apiFetch(`${API_BASE}/transactions?${params}`, {
        method: "GET",
        headers: { "Content-Type": "application/json" },
        credentials: "include",
      });

      if (response.ok) {
        const data = (await response.json()) as ListTransaction;
        setTransactions(data.data);
        setTotalPages(data.metadata.pageCount);
      } else {
        setTransactions([]);
        await reportApiError(response, {
          fallback: t("transactions.errorOccurredFetchingTransactions"),
          onUnauthorized: goToLogin,
          toastId: "error-fetching-transactions",
        });
      }
    } catch {
      setTransactions([]);
      toast.error(t("transactions.errorOccurredFetchingTransactions"), {
        id: "error-fetching-transactions",
      });
    } finally {
      setLoading(false);
    }
  }, [
    pageNumber,
    pageSize,
    reference,
    friendlyName,
    fromDate,
    toDate,
    goToLogin,
  ]);

  useEffect(() => {
    fetchTransactions();
  }, [fetchTransactions]);

  useEffect(() => {
    const params = new URLSearchParams();
    params.set("pageNumber", pageNumber.toString());
    params.set("pageSize", pageSize.toString());
    if (reference) params.set("externalReference", reference);
    if (friendlyName) params.set("friendlyName", friendlyName);
    if (fromDate) params.set("fromDate", fromDate);
    if (toDate) params.set("toDate", toDate);
    router.replace(`?${params.toString()}`);
  }, [pageNumber, pageSize, reference, friendlyName, fromDate, toDate, router]);

  const filtered = Boolean(reference || friendlyName || range?.from);

  return (
    <div className="space-y-4">
      <PageHeader
        eyebrow={t("transactions.monitoring")}
        title={t("transactions.transactions")}
        description={t("transactions.everythingTheRulesHaveBeen")}
      />

      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="flex flex-wrap items-center gap-3">
          <Input
            value={reference}
            onChange={(e) => {
              setReference(e.target.value);
              setPageNumber(1);
            }}
            placeholder={t("transactions.searchByYourReference")}
            className="w-64"
          />
          <Input
            value={friendlyName}
            onChange={(e) => {
              setFriendlyName(e.target.value);
              setPageNumber(1);
            }}
            placeholder={t("transactions.txNumber")}
            className="w-40"
          />
        </div>
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

      <div className="overflow-hidden rounded-xl border border-slate-200 bg-card">
        <Table>
          <TableHeader>
            <TableRow className="bg-slate-50 hover:bg-slate-50">
              <TableHead>{t("transactions.reference")}</TableHead>
              <TableHead>{t("common.created")}</TableHead>
              <TableHead className="w-32">{t("common.actions")}</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {loading ? (
              Array.from({ length: 5 }).map((_, i) => (
                <TableRow key={i}>
                  {Array.from({ length: 3 }).map((_, j) => (
                    <TableCell key={j}>
                      <Skeleton className="h-4 w-full" />
                    </TableCell>
                  ))}
                </TableRow>
              ))
            ) : transactions.length === 0 ? (
              <TableRow>
                <TableCell
                  colSpan={3}
                  className="py-14 text-center font-mono text-xs uppercase tracking-widest text-slate-400"
                >
                  {filtered ? "No transaction matches that" : "No results found"}
                </TableCell>
              </TableRow>
            ) : (
              transactions.map((transaction) => (
                <TableRow
                  key={transaction.transactionId}
                  className="hover:bg-slate-50/50"
                >
                  <TableCell>
                    <TransactionReference transaction={transaction} />
                  </TableCell>
                  <TableCell className="text-sm text-slate-600">
                    <LocalTime value={transaction.createdAt} />
                  </TableCell>
                  <TableCell>
                    <Button
                      variant="outline"
                      size="sm"
                      className="gap-1.5"
                      onClick={() =>
                        router.push(`/transactions/${transaction.transactionId}`)
                      }
                    >
                      <Eye className="size-4" />
                      {t("common.view")}
                    </Button>
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </div>

      {transactions.length > 0 && (
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
};

export default Transactions;
