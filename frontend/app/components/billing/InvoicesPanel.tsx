"use client";

import { useCallback, useEffect, useState } from "react";
import { toast } from "sonner";
import { FileText } from "lucide-react";

import type { Invoice, InvoiceResource, InvoiceSettlement } from "@/types";
import { API_BASE, reportApiError, unwrapPage } from "@/lib/api";
import { formatMoney } from "@/lib/currency";
import { versionLabel } from "@/lib/plans";
import {
  DRAFT_HAS_NO_NUMBER,
  DRAFT_IS_NOT_A_DOCUMENT,
  INVOICE_STATUS_FILTER_LABELS,
  INVOICE_STATUS_LABELS,
  type InvoiceStatusFilter,
  currenciesPresent,
  daysOverdue,
  invoicesInWorkingOrder,
  isOverdue,
  matchesStatusFilter,
} from "@/lib/invoiceDocument";
import { invoicePeriod, linesInOrder, totalsAgree } from "@/lib/pricing";
import { apiFetch } from "@/lib/http";
import { usePermissions } from "@/context/PermissionsContext";
import { cn } from "@/lib/utils";
import Pagination from "@/components/Pagination";
import { LocalTime } from "@/components/LocalTime";
import { StatusBadge } from "@/components/StatusBadge";
import { Skeleton } from "@/components/ui/skeleton";
import { Button } from "@/components/ui/button";
import { InvoiceDetail } from "@/components/billing/InvoiceDetail";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { useTranslation } from "@/context/LocalizationContext";

export function InvoicesPanel({
  organizationId,
  onUnauthorized,
}: {
  organizationId: string;
  onUnauthorized: () => void;
}) {
  const t = useTranslation();
  const { holds, resolved } = usePermissions();
  const mayRead = holds("invoices:read");

  const [invoices, setInvoices] = useState<Invoice[]>([]);
  const [settlements, setSettlements] = useState<Map<string, InvoiceSettlement>>(
    new Map()
  );
  const [pageNumber, setPageNumber] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [loading, setLoading] = useState(true);
  const [opened, setOpened] = useState<Invoice | null>(null);
  const [filter, setFilter] = useState<InvoiceStatusFilter>("all");

  const now = new Date();

  const fetchInvoices = useCallback(async () => {
    setLoading(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationId}/invoices?PageNumber=${pageNumber}&PageSize=25`,
        {
          method: "GET",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
        }
      );

      if (response.ok) {
        const page = await unwrapPage<InvoiceResource>(response);
        setInvoices(page.data.map((resource) => resource.invoice));
        setSettlements(
          new Map(
            page.data
              .filter((resource) => resource.settlement !== undefined)
              .map((resource) => [
                resource.invoice.invoiceId,
                resource.settlement!,
              ])
          )
        );
        setTotalPages(page.metadata.pageCount);
      } else {
        await reportApiError(response, {
          fallback: t("billing.errorOccurredFetchingInvoices"),
          onUnauthorized,
          toastId: "error-fetching-invoices",
        });
      }
    } catch {
      toast.error(t("billing.errorOccurredFetchingInvoices"), {
        id: "error-fetching-invoices",
      });
    } finally {
      setLoading(false);
    }
  }, [organizationId, pageNumber, onUnauthorized]);

  useEffect(() => {
    if (mayRead) fetchInvoices();
    else setLoading(false);
  }, [mayRead, fetchInvoices]);

  if (!resolved) return null;

  if (!mayRead) {
    return (
      <div className="rounded-xl border border-slate-200 bg-card p-6">
        <p className="text-sm text-slate-500">
          {t("billing.needs")} <span className="font-mono text-xs">invoices:read</span>.
        </p>
      </div>
    );
  }

  const visible = invoicesInWorkingOrder(invoices).filter((invoice) =>
    matchesStatusFilter(invoice, filter)
  );

  const drafts = invoices.filter(
    (invoice) => invoice.invoiceStatus === "Draft"
  ).length;

  const currencies = currenciesPresent(invoices);

  return (
    <div className="space-y-3">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div className="flex items-start gap-2">
          <FileText className="mt-0.5 size-3.5 flex-shrink-0 text-slate-400" />
          <div>
            <p className="font-mono text-xs uppercase tracking-widest text-slate-500">
              {t("billing.invoices")}
            </p>
            <p className="mt-1 max-w-2xl text-sm text-slate-500">
              One per calendar month. Each names the plan version it was priced
              from and carries the arithmetic that reached its total — open one
              to read the working, issue it, or credit it.
            </p>
            {drafts > 0 && (
              <p className="mt-1 text-xs text-attention-700">
                {drafts} draft{drafts === 1 ? "" : "s"} not yet issued.{" "}
                {DRAFT_IS_NOT_A_DOCUMENT}
              </p>
            )}
            {currencies.length > 1 && (
              <p className="mt-1 text-xs text-slate-400">
                This list holds invoices in {currencies.join(", ")} — each total
                is in its own document&apos;s currency, so the column does not
                add up.
              </p>
            )}
          </div>
        </div>

        <div className="flex items-center gap-1 rounded-lg border border-slate-200 p-0.5">
          {(["all", "Draft", "Issued"] as InvoiceStatusFilter[]).map(
            (option) => (
              <Button
                key={option}
                variant="ghost"
                size="sm"
                className={cn(
                  "h-7 px-2.5 text-xs",
                  filter === option && "bg-slate-100 text-slate-900"
                )}
                onClick={() => setFilter(option)}
              >
                {INVOICE_STATUS_FILTER_LABELS[option]}
              </Button>
            )
          )}
        </div>
      </div>

      <div className="overflow-hidden rounded-xl border border-slate-200 bg-card">
        <Table>
          <TableHeader>
            <TableRow className="bg-slate-50 hover:bg-slate-50">
              <TableHead className="w-44">{t("billing.number")}</TableHead>
              <TableHead className="w-36">{t("billing.period")}</TableHead>
              <TableHead className="w-28">{t("common.status")}</TableHead>
              <TableHead className="w-32">{t("billing.pricedFrom")}</TableHead>
              <TableHead className="w-20 text-right">{t("billing.lines")}</TableHead>
              <TableHead className="w-20">{t("common.currency")}</TableHead>
              <TableHead className="w-32 text-right">{t("billing.total")}</TableHead>
              <TableHead className="w-36 text-right">
                {t("billing.outstanding")}
              </TableHead>
              <TableHead className="w-36">{t("billing.due")}</TableHead>
              <TableHead className="w-24">{t("common.actions")}</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {loading ? (
              Array.from({ length: 3 }).map((_, row) => (
                <TableRow key={row}>
                  {Array.from({ length: 10 }).map((_, cell) => (
                    <TableCell key={cell}>
                      <Skeleton className="h-4 w-full" />
                    </TableCell>
                  ))}
                </TableRow>
              ))
            ) : visible.length === 0 ? (
              <TableRow>
                <TableCell colSpan={10} className="py-10 text-center">
                  <p className="font-mono text-xs uppercase tracking-widest text-slate-400">
                    {invoices.length === 0 ? "No invoices" : "Nothing matches"}
                  </p>
                  <p className="mx-auto mt-2 max-w-md text-sm text-slate-500">
                    {invoices.length === 0 ? (
                      <>
                        Nothing has been billed. An organisation on no plan is
                        skipped by the monthly job without complaining, and one
                        with no billing profile is refused outright — check both
                        panels above.
                      </>
                    ) : (
                      <>
                        There are invoices here, but none of them are{" "}
                        {INVOICE_STATUS_FILTER_LABELS[filter].toLowerCase()}.
                      </>
                    )}
                  </p>
                </TableCell>
              </TableRow>
            ) : (
              visible.map((invoice) => {
                const lines = linesInOrder(invoice.lines);
                const agrees = totalsAgree(invoice);
                const overdue = isOverdue(invoice, now);
                const settlement = settlements.get(invoice.invoiceId);

                return (
                  <TableRow
                    key={invoice.invoiceId}
                    className="hover:bg-slate-50/50"
                  >
                    <TableCell
                      className={cn(
                        "font-mono text-sm",
                        invoice.invoiceNumber === null
                          ? "text-slate-400"
                          : "text-slate-900"
                      )}
                    >
                      {invoice.invoiceNumber ?? DRAFT_HAS_NO_NUMBER}
                    </TableCell>

                    <TableCell className="text-sm text-slate-700">
                      {invoicePeriod(invoice)}
                      <span className="block text-xs font-normal text-slate-400">
                        raised <LocalTime value={invoice.createdAt} dateOnly />
                      </span>
                    </TableCell>

                    <TableCell>
                      <StatusBadge
                        status={invoice.invoiceStatus}
                        label={INVOICE_STATUS_LABELS[invoice.invoiceStatus]}
                      />
                    </TableCell>

                    <TableCell className="text-xs text-slate-600">
                      {invoice.planVersionId === null
                        ? "Predates plans"
                        : versionLabel(null, invoice.planVersionNumber)}
                    </TableCell>

                    <TableCell className="text-right font-mono text-xs text-slate-500">
                      {lines.length}
                    </TableCell>

                    <TableCell className="font-mono text-xs text-slate-500">
                      {invoice.currency}
                      {invoice.planCurrency !== invoice.currency && (
                        <span className="block text-[11px] text-slate-400">
                          from {invoice.planCurrency}
                        </span>
                      )}
                    </TableCell>

                    <TableCell
                      className={cn(
                        "text-right font-mono text-sm",
                        agrees ? "text-slate-900" : "text-critical-700"
                      )}
                    >
                      {formatMoney(invoice.amount, invoice.currency)}
                      {!agrees && (
                        <span className="block text-[11px] font-normal">
                          disagrees with its lines
                        </span>
                      )}
                    </TableCell>

                    <TableCell className="text-right font-mono text-sm">
                      {settlement === undefined ? (
                        <span className="font-sans text-xs font-normal text-slate-400">
                          {t("billing.notIssuedNothingToSettle")}
                        </span>
                      ) : settlement.isSettled ? (
                        <span className="font-sans text-xs font-normal text-settled-700">
                          {t("billing.nothingOutstanding")}
                        </span>
                      ) : (
                        <span
                          className={
                            overdue ? "text-attention-700" : "text-slate-900"
                          }
                        >
                          {formatMoney(
                            settlement.outstandingAmount,
                            invoice.currency
                          )}
                        </span>
                      )}
                    </TableCell>

                    <TableCell className="text-xs">
                      {invoice.isProcessed ? (
                        <span className="text-slate-500">
                          Processed{" "}
                          {invoice.processedAt !== null && (
                            <LocalTime value={invoice.processedAt} dateOnly />
                          )}
                        </span>
                      ) : invoice.dueAt === null ? (
                        <span className="text-slate-400">
                          {t("billing.notIssuedNothingToBe")}
                        </span>
                      ) : overdue ? (
                        <span className="text-attention-700">
                          Overdue by {daysOverdue(invoice, now)} day
                          {daysOverdue(invoice, now) === 1 ? "" : "s"}
                        </span>
                      ) : (
                        <span className="text-slate-500">
                          <LocalTime value={invoice.dueAt} dateOnly />
                        </span>
                      )}
                    </TableCell>

                    <TableCell>
                      <Button
                        variant="ghost"
                        size="sm"
                        className="h-8 text-xs"
                        onClick={() => setOpened(invoice)}
                      >
                        {t("billing.open")}
                      </Button>
                    </TableCell>
                  </TableRow>
                );
              })
            )}
          </TableBody>
        </Table>
      </div>

      {visible.length > 0 && totalPages > 1 && (
        <Pagination
          handlePrevPage={() => setPageNumber((page) => Math.max(1, page - 1))}
          pageNumber={pageNumber}
          totalPages={totalPages}
          handleNextPage={() =>
            setPageNumber((page) => Math.min(totalPages, page + 1))
          }
        />
      )}

      <Dialog
        open={opened !== null}
        onOpenChange={(open) => !open && setOpened(null)}
      >
        <DialogContent className="sm:max-w-3xl">
          <DialogHeader>
            <DialogTitle>
              {opened !== null &&
                `${opened.invoiceNumber ?? DRAFT_HAS_NO_NUMBER} · ${invoicePeriod(opened)}`}
            </DialogTitle>
            <DialogDescription>
              The document, and the working behind its total in the order the
              arithmetic happened. The total is the sum of those lines and is
              not reachable any other way.
            </DialogDescription>
          </DialogHeader>

          <div className="max-h-[70vh] overflow-y-auto pr-1">
            {opened !== null && (
              <InvoiceDetail
                invoice={opened}
                organizationId={organizationId}
                settlement={settlements.get(opened.invoiceId)}
                onUnauthorized={onUnauthorized}
                now={now}
                onCollected={fetchInvoices}
                onIssued={(issued) => {
                  setOpened(issued);
                  fetchInvoices();
                }}
                onWithdrawn={() => {
                  setOpened(null);
                  fetchInvoices();
                }}
              />
            )}
          </div>
        </DialogContent>
      </Dialog>
    </div>
  );
}
