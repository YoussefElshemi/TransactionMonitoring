"use client";

import { useCallback, useEffect, useState } from "react";
import { toast } from "sonner";
import { Download, Receipt } from "lucide-react";

import type { Payment } from "@/types";
import { API_BASE, reportApiError, unwrapPage } from "@/lib/api";
import { apiFetch } from "@/lib/http";
import { toCurrencyCode } from "@/lib/currency";
import { saveResponseAsFile } from "@/lib/download";
import {
  PAYMENT_STATUS_FILTERS,
  PAYMENT_STATUS_FILTER_LABELS,
  RECEIPT_ONLY_WHEN_SETTLED,
  type PaymentStatusFilter,
  canDownloadReceipt,
  currenciesOnPayments,
  decisiveDate,
  formatPaymentAmount,
  isReversed,
  paymentMethodKindLabelKey,
  paymentStatusHintKey,
  statusQueryValue,
} from "@/lib/payments";
import { usePermissions } from "@/context/PermissionsContext";
import { cn } from "@/lib/utils";
import Pagination from "@/components/Pagination";
import { LocalTime } from "@/components/LocalTime";
import { PaymentStatusBadge } from "@/components/payments/PaymentStatusBadge";
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
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { useTranslation } from "@/context/LocalizationContext";

export function PaymentsPanel({
  organizationId,
  onUnauthorized,
}: {
  organizationId: string;
  onUnauthorized: () => void;
}) {
  const t = useTranslation();
  const { holds, resolved } = usePermissions();

  const mayRead = holds("payments:read");

  const [payments, setPayments] = useState<Payment[]>([]);
  const [pageNumber, setPageNumber] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [loading, setLoading] = useState(true);
  const [opened, setOpened] = useState<Payment | null>(null);
  const [filter, setFilter] = useState<PaymentStatusFilter>("all");

  const fetchPayments = useCallback(async () => {
    setLoading(true);

    try {
      const status = statusQueryValue(filter);
      const query = new URLSearchParams({
        PageNumber: String(pageNumber),
        PageSize: "25",
      });
      if (status !== null) query.set("PaymentStatus", status);

      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationId}/payments?${query}`,
        {
          method: "GET",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
        }
      );

      if (response.ok) {
        const page = await unwrapPage<Payment>(response);
        setPayments(page.data);
        setTotalPages(page.metadata.pageCount);
      } else {
        await reportApiError(response, {
          fallback: t("payments.paymentsCouldNotBeRead"),
          onUnauthorized,
          toastId: "error-fetching-payments",
        });
      }
    } catch {
      toast.error(t("payments.paymentsCouldNotBeRead"), {
        id: "error-fetching-payments",
      });
    } finally {
      setLoading(false);
    }
  }, [organizationId, pageNumber, filter, onUnauthorized]);

  useEffect(() => {
    if (mayRead) fetchPayments();
    else setLoading(false);
  }, [mayRead, fetchPayments]);

  if (!resolved) return null;

  if (!mayRead) {
    return (
      <div className="rounded-xl border border-slate-200 bg-card p-6">
        <p className="text-sm text-slate-500">
          {t("billing.needs")}{" "}
          <span className="font-mono text-xs">payments:read</span>.
        </p>
      </div>
    );
  }

  const currencies = currenciesOnPayments(payments);

  return (
    <div className="space-y-3">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div className="flex items-start gap-2">
          <Receipt className="mt-0.5 size-3.5 flex-shrink-0 text-slate-400" />
          <div>
            <p className="font-mono text-xs uppercase tracking-widest text-slate-500">
              {t("payments.paymentHistory")}
            </p>
            <p className="mt-1 max-w-2xl text-sm text-slate-500">
              {t("payments.everyCollectionIncludingTheOnes")}
            </p>
            {currencies.length > 1 && (
              <p className="mt-1 text-xs text-slate-400">
                {t("payments.thisListHoldsCurrencies", currencies.join(", "))}
              </p>
            )}
          </div>
        </div>

        <div className="flex flex-wrap items-center gap-1 rounded-lg border border-slate-200 p-0.5">
          {PAYMENT_STATUS_FILTERS.map((option) => (
            <Button
              key={option}
              variant="ghost"
              size="sm"
              className={cn(
                "h-7 px-2.5 text-xs",
                filter === option && "bg-slate-100 text-slate-900"
              )}
              onClick={() => {
                setFilter(option);
                setPageNumber(1);
              }}
            >
              {t(PAYMENT_STATUS_FILTER_LABELS[option])}
            </Button>
          ))}
        </div>
      </div>

      <div className="overflow-hidden rounded-xl border border-slate-200 bg-card">
        <Table>
          <TableHeader>
            <TableRow className="bg-slate-50 hover:bg-slate-50">
              <TableHead className="w-40">{t("payments.when")}</TableHead>
              <TableHead className="w-32">{t("common.status")}</TableHead>
              <TableHead className="w-44">{t("payments.method")}</TableHead>
              <TableHead className="w-20">{t("common.currency")}</TableHead>
              <TableHead className="w-32 text-right">
                {t("common.amount")}
              </TableHead>
              <TableHead>{t("payments.reference")}</TableHead>
              <TableHead className="w-28">{t("common.actions")}</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {loading ? (
              Array.from({ length: 3 }).map((_, row) => (
                <TableRow key={row}>
                  {Array.from({ length: 7 }).map((_, cell) => (
                    <TableCell key={cell}>
                      <Skeleton className="h-4 w-full" />
                    </TableCell>
                  ))}
                </TableRow>
              ))
            ) : payments.length === 0 ? (
              <TableRow>
                <TableCell colSpan={7} className="py-10 text-center">
                  <p className="font-mono text-xs uppercase tracking-widest text-slate-400">
                    {filter === "all"
                      ? t("payments.noPaymentsYet")
                      : t("billing.nothingMatches")}
                  </p>
                  <p className="mx-auto mt-2 max-w-md text-sm text-slate-500">
                    {filter === "all"
                      ? t("payments.nothingHasBeenCollectedYet")
                      : t(
                          "payments.thereArePaymentsButNoneAre",
                          t(PAYMENT_STATUS_FILTER_LABELS[filter]).toLowerCase()
                        )}
                  </p>
                </TableCell>
              </TableRow>
            ) : (
              payments.map((payment) => {
                const dated = decisiveDate(payment);

                return (
                  <TableRow
                    key={payment.paymentId}
                    className="hover:bg-slate-50/50"
                  >
                    <TableCell className="text-sm text-slate-700">
                      <LocalTime value={dated.value} dateOnly />
                      <span className="block text-xs font-normal text-slate-400">
                        {t(dated.labelKey)}
                      </span>
                    </TableCell>

                    <TableCell>
                      <PaymentStatusBadge status={payment.paymentStatus} />
                    </TableCell>

                    <TableCell className="text-xs text-slate-600">
                      {payment.paymentMethodLabel ??
                        t(paymentMethodKindLabelKey(payment.paymentMethodKind))}
                      {payment.paymentMethodLabel !== null && (
                        <span className="block text-[11px] text-slate-400">
                          {t(
                            paymentMethodKindLabelKey(payment.paymentMethodKind)
                          )}
                        </span>
                      )}
                    </TableCell>

                    <TableCell className="font-mono text-xs text-slate-500">
                      {toCurrencyCode(payment.currency)}
                    </TableCell>

                    <TableCell
                      className={cn(
                        "text-right font-mono text-sm",
                        isReversed(payment)
                          ? "text-elevated-700"
                          : "text-slate-900"
                      )}
                    >
                      {formatPaymentAmount(payment)}
                    </TableCell>

                    <TableCell className="text-xs">
                      {payment.receiptNumber !== null ? (
                        <span className="font-mono text-slate-700">
                          {payment.receiptNumber}
                          {isReversed(payment) && (
                            <span className="block font-sans text-[11px] font-normal text-elevated-700">
                              {t("payments.receiptIssuedThenReversed")}
                            </span>
                          )}
                        </span>
                      ) : payment.failureReason !== null ? (
                        <span className="text-critical-700">
                          {payment.failureReason}
                        </span>
                      ) : payment.remittanceReference !== null ? (
                        <span className="font-mono text-slate-600">
                          {payment.remittanceReference}
                        </span>
                      ) : (
                        <span className="text-slate-400">
                          {t("payments.nothingToReferenceYet")}
                        </span>
                      )}
                    </TableCell>

                    <TableCell>
                      <Button
                        variant="ghost"
                        size="sm"
                        className="h-8 text-xs"
                        onClick={() => setOpened(payment)}
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

      <p className="text-xs text-slate-400">{t(RECEIPT_ONLY_WHEN_SETTLED)}</p>

      {payments.length > 0 && totalPages > 1 && (
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
        <DialogContent className="sm:max-w-2xl">
          <DialogHeader>
            <DialogTitle>{t("payments.payment")}</DialogTitle>
            <DialogDescription>
              {t("payments.oneMovementOfMoney")}
            </DialogDescription>
          </DialogHeader>

          <div className="max-h-[70vh] overflow-y-auto pr-1">
            {opened !== null && (
              <PaymentDetail
                payment={opened}
                organizationId={organizationId}
                onUnauthorized={onUnauthorized}
              />
            )}
          </div>
        </DialogContent>
      </Dialog>
    </div>
  );
}

function PaymentDetail({
  payment,
  organizationId,
  onUnauthorized,
}: {
  payment: Payment;
  organizationId: string;
  onUnauthorized: () => void;
}) {
  const t = useTranslation();
  const [downloading, setDownloading] = useState(false);
  const dated = decisiveDate(payment);

  const downloadReceipt = async () => {
    setDownloading(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationId}/payments/${payment.paymentId}/receipt`,
        { method: "POST", credentials: "include" },
        // The response is a PDF, and the API marks this exempt for that reason:
        // an idempotency record stores a JSON body, so a replay would hand back
        // an empty document in place of the receipt.
        { withoutIdempotencyKey: true }
      );

      if (!response.ok) {
        await reportApiError(response, {
          fallback: t("payments.receiptCouldNotBeExported"),
          onUnauthorized,
          toastId: `receipt-${payment.paymentId}`,
        });
        return;
      }

      await saveResponseAsFile(
        response,
        `${payment.receiptNumber ?? payment.paymentId}.pdf`
      );
    } catch {
      toast.error(t("payments.receiptCouldNotBeExported"), {
        id: `receipt-${payment.paymentId}`,
      });
    } finally {
      setDownloading(false);
    }
  };

  return (
    <div className="space-y-4">
      <div className="rounded-xl border border-slate-200 bg-slate-50 p-4">
        <div className="flex flex-wrap items-start justify-between gap-3">
          <div>
            <p className="font-mono text-2xl text-slate-900">
              {formatPaymentAmount(payment)}
            </p>
            <p className="mt-1 text-sm text-slate-600">
              {payment.paymentMethodLabel ??
                t(paymentMethodKindLabelKey(payment.paymentMethodKind))}
            </p>
          </div>
          <PaymentStatusBadge status={payment.paymentStatus} />
        </div>

        <p className="mt-2 max-w-2xl text-xs text-slate-500">
          {t(paymentStatusHintKey(payment.paymentStatus))}
        </p>
      </div>

      {payment.failureReason !== null && (
        <div className="rounded-xl border border-critical-200 bg-critical-50 p-4">
          <p className="font-mono text-xs uppercase tracking-widest text-critical-700">
            {t("payments.whyItFailed")}
          </p>
          <p className="mt-1 text-sm text-critical-700">
            {payment.failureReason}
          </p>
          <p className="mt-2 text-xs text-critical-700/80">
            {t("payments.inTheProvidersOwnWords")}
          </p>
        </div>
      )}

      <div className="grid gap-3 sm:grid-cols-3">
        <Fact label={t("payments.requested")}>
          <LocalTime value={payment.receivedAt} />
          <span className="block text-[11px] text-slate-400">
            {t("payments.thisRowIsDatedBy", t(dated.labelKey).toLowerCase())}
          </span>
        </Fact>

        <Fact label={t("payments.settled")}>
          {payment.settledAt === null ? (
            <span className="text-slate-400">
              {t("payments.notYetSettled")}
            </span>
          ) : (
            <LocalTime value={payment.settledAt} />
          )}
        </Fact>

        <Fact label={t("payments.reversed")}>
          {payment.returnedAt === null ? (
            <span className="text-slate-400">{t("payments.notReversed")}</span>
          ) : (
            <LocalTime value={payment.returnedAt} />
          )}
        </Fact>
      </div>

      <div className="grid gap-3 sm:grid-cols-2">
        <Fact label={t("payments.receiptNumber")}>
          {payment.receiptNumber === null ? (
            <span className="text-slate-400">
              {t("payments.noneOnlySettledPaymentsHave")}
            </span>
          ) : (
            <span className="font-mono">{payment.receiptNumber}</span>
          )}
        </Fact>

        <Fact label={t("payments.remittanceReference")}>
          {payment.remittanceReference === null ? (
            <span className="text-slate-400">{t("payments.none")}</span>
          ) : (
            <span className="font-mono">{payment.remittanceReference}</span>
          )}
        </Fact>
      </div>

      {payment.raisedForInvoiceId !== null && (
        <p className="text-xs text-slate-400">
          {t("payments.raisedForAnInvoiceNotAClaim")}
        </p>
      )}

      <div className="flex flex-wrap items-center gap-2 border-t border-slate-200 pt-4">
        {canDownloadReceipt(payment) ? (
          <Button
            variant="outline"
            size="sm"
            disabled={downloading}
            onClick={downloadReceipt}
          >
            <Download />
            {downloading
              ? t("billing.preparing")
              : t("payments.downloadReceipt")}
          </Button>
        ) : (
          <p className="text-xs text-slate-400">
            {t(RECEIPT_ONLY_WHEN_SETTLED)}
          </p>
        )}
      </div>
    </div>
  );
}

function Fact({
  label,
  children,
}: {
  label: string;
  children: React.ReactNode;
}) {
  return (
    <div className="rounded-lg border border-slate-200 bg-card p-3">
      <p className="font-mono text-xs uppercase tracking-widest text-slate-500">
        {label}
      </p>
      <p className="mt-1 text-sm text-slate-900">{children}</p>
    </div>
  );
}
