"use client";

import { useCallback, useEffect, useState } from "react";
import { toast } from "sonner";

import type {
  AccountBalance,
  Invoice,
  InvoiceResource,
  Payment,
} from "@/types";
import { API_BASE, reportApiError, unwrapPage } from "@/lib/api";
import { apiFetch } from "@/lib/http";
import { formatMoney, toCurrencyCode } from "@/lib/currency";
import {
  ALLOCATE_EVERYTHING_BY_DEFAULT,
  REFUND_REASON_IS_THE_RECORD,
  REFUND_RECORDS_IT_ONLY,
  formatBalance,
} from "@/lib/account";
import {
  COLLECTION_REFUSAL_HINTS,
  COLLECTION_REFUSAL_TITLES,
  formatPaymentAmount,
  readCollectionRefusal,
} from "@/lib/payments";
import { useIdempotencyKey } from "@/lib/useIdempotencyKey";
import { cn } from "@/lib/utils";
import { LocalTime } from "@/components/LocalTime";
import { PaymentStatusBadge } from "@/components/payments/PaymentStatusBadge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { useTranslation } from "@/context/LocalizationContext";

export function ReconcileDialog({
  balance,
  mayRefund,
  organizationName,
  onClose,
  onChanged,
  onUnauthorized,
}: {
  balance: AccountBalance | null;
  mayRefund: boolean;
  organizationName: string | null;
  onClose: () => void;
  onChanged: () => void;
  onUnauthorized: () => void;
}) {
  const t = useTranslation();
  const submission = useIdempotencyKey();

  const [payments, setPayments] = useState<Payment[]>([]);
  const [invoices, setInvoices] = useState<InvoiceResource[]>([]);
  const [loading, setLoading] = useState(true);

  const [allocatingTo, setAllocatingTo] = useState<string | null>(null);
  const [allocationAmount, setAllocationAmount] = useState("");
  const [saving, setSaving] = useState(false);

  const [refundOpen, setRefundOpen] = useState(false);
  const [refundAmount, setRefundAmount] = useState("");
  const [refundReason, setRefundReason] = useState("");

  const organizationId = balance?.organizationId ?? null;
  const currency = balance === null ? null : toCurrencyCode(balance.currency);

  const fetchContext = useCallback(async () => {
    if (organizationId === null) return;
    setLoading(true);

    try {
      const [paymentResponse, invoiceResponse] = await Promise.all([
        apiFetch(
          `${API_BASE}/organizations/${organizationId}/payments?PageNumber=1&PageSize=10`,
          {
            method: "GET",
            headers: { "Content-Type": "application/json" },
            credentials: "include",
          }
        ),
        apiFetch(
          `${API_BASE}/organizations/${organizationId}/invoices?PageNumber=1&PageSize=25`,
          {
            method: "GET",
            headers: { "Content-Type": "application/json" },
            credentials: "include",
          }
        ),
      ]);

      if (paymentResponse.ok) {
        setPayments((await unwrapPage<Payment>(paymentResponse)).data);
      }
      if (invoiceResponse.ok) {
        setInvoices((await unwrapPage<InvoiceResource>(invoiceResponse)).data);
      }
    } catch {
      toast.error(t("reconciliation.contextCouldNotBeRead"), {
        id: "error-fetching-reconcile-context",
      });
    } finally {
      setLoading(false);
    }
  }, [organizationId]);

  useEffect(() => {
    if (organizationId !== null) {
      setAllocatingTo(null);
      setAllocationAmount("");
      setRefundOpen(false);
      setRefundAmount("");
      setRefundReason("");
      fetchContext();
    }
  }, [organizationId, fetchContext]);

  if (balance === null || currency === null) return null;

  const applicable = invoices.filter(
    (resource) =>
      resource.invoice.invoiceStatus === "Issued" &&
      resource.invoice.currency === currency &&
      resource.settlement !== undefined &&
      !resource.settlement.isSettled &&
      resource.settlement.outstandingAmount > 0
  );

  const allocate = async (invoice: Invoice) => {
    setSaving(true);

    try {
      const typed = allocationAmount.trim();
      const response = await apiFetch(
        `${API_BASE}/organizations/${balance.organizationId}/account/allocations`,
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
          body: JSON.stringify({
            invoiceId: invoice.invoiceId,
            amount: typed === "" ? null : Number(typed),
          }),
        },
        { idempotencyKey: submission.current(`allocate-${invoice.invoiceId}`) }
      );

      if (response.ok) {
        submission.completed(`allocate-${invoice.invoiceId}`);
        toast.success(t("reconciliation.cashApplied"), {
          description: t("reconciliation.theLedgerNowShowsIt"),
        });

        setAllocatingTo(null);
        setAllocationAmount("");
        onChanged();
        onClose();
        return;
      }

      await reportRefusal(response, `allocate-${invoice.invoiceId}`);
    } catch {
      toast.error(t("reconciliation.cashCouldNotBeApplied"), {
        id: `allocate-${invoice.invoiceId}`,
      });
    } finally {
      setSaving(false);
    }
  };

  const refund = async () => {
    setSaving(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${balance.organizationId}/account/refunds`,
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
          body: JSON.stringify({
            currency,
            amount: Number(refundAmount),
            reason: refundReason,
          }),
        },
        { idempotencyKey: submission.current("refund") }
      );

      if (response.ok) {
        submission.completed("refund");
        toast.success(t("reconciliation.refundRecorded"), {
          description: t(REFUND_RECORDS_IT_ONLY),
        });

        setRefundOpen(false);
        onChanged();
        onClose();
        return;
      }

      await reportRefusal(response, "refund");
    } catch {
      toast.error(t("reconciliation.refundCouldNotBeRecorded"), {
        id: "refund",
      });
    } finally {
      setSaving(false);
    }
  };

  const reportRefusal = async (response: Response, toastId: string) => {
    if (response.status === 409) {
      const body = await response.json().catch(() => null);
      const refusal = readCollectionRefusal(body);

      if (refusal !== null) {
        toast.error(t(COLLECTION_REFUSAL_TITLES[refusal]), {
          id: toastId,
          description:
            (body as { detail?: string } | null)?.detail ??
            t(COLLECTION_REFUSAL_HINTS[refusal]),
        });
        return;
      }
    }

    await reportApiError(response, {
      fallback: t("reconciliation.theLedgerRefusedThat"),
      onUnauthorized,
      toastId,
    });
  };

  return (
    <Dialog open onOpenChange={(open) => !open && onClose()}>
      <DialogContent className="sm:max-w-3xl">
        <DialogHeader>
          <DialogTitle>
            {organizationName ?? balance.organizationId.slice(0, 8)}
          </DialogTitle>
          <DialogDescription>
            {t("reconciliation.whatArrivedAndWhatItMightBeFor")}
          </DialogDescription>
        </DialogHeader>

        <div className="max-h-[70vh] space-y-4 overflow-y-auto pr-1">
          <div className="rounded-xl border border-settled-200 bg-settled-50 p-4">
            <p className="font-mono text-xs uppercase tracking-widest text-slate-500">
              {t("reconciliation.onAccount")}
            </p>
            <p className="mt-1 font-mono text-2xl text-settled-700">
              {formatBalance(balance)}
            </p>
            <p className="mt-1 text-xs text-slate-500">
              {t("reconciliation.movementsBehindIt", balance.entryCount)}
            </p>
          </div>

          <div>
            <p className="font-mono text-xs uppercase tracking-widest text-slate-500">
              {t("reconciliation.recentPayments")}
            </p>

            {loading ? (
              <Skeleton className="mt-2 h-20 w-full rounded-lg" />
            ) : payments.length === 0 ? (
              <p className="mt-2 text-sm text-slate-500">
                {t("reconciliation.noPaymentsOnThisAccount")}
              </p>
            ) : (
              <ul className="mt-2 space-y-2">
                {payments.map((payment) => (
                  <li
                    key={payment.paymentId}
                    className="rounded-lg border border-slate-200 bg-card p-3"
                  >
                    <div className="flex flex-wrap items-start justify-between gap-2">
                      <div>
                        <p className="font-mono text-sm text-slate-900">
                          {payment.remittanceReference ??
                            t("reconciliation.noReferenceGiven")}
                        </p>
                        <p className="mt-0.5 text-xs text-slate-500">
                          {formatPaymentAmount(payment)}
                          {" · "}
                          <LocalTime value={payment.receivedAt} dateOnly />
                        </p>
                      </div>
                      <PaymentStatusBadge status={payment.paymentStatus} />
                    </div>
                  </li>
                ))}
              </ul>
            )}
          </div>

          <div>
            <p className="font-mono text-xs uppercase tracking-widest text-slate-500">
              {t("reconciliation.openInvoicesInThisCurrency")}
            </p>

            {loading ? (
              <Skeleton className="mt-2 h-20 w-full rounded-lg" />
            ) : applicable.length === 0 ? (
              <p className="mt-2 max-w-2xl text-sm text-slate-500">
                {t("reconciliation.nothingOpenToApplyItTo")}
              </p>
            ) : (
              <ul className="mt-2 space-y-2">
                {applicable.map(({ invoice, settlement }) => {
                  const open = allocatingTo === invoice.invoiceId;

                  return (
                    <li
                      key={invoice.invoiceId}
                      className={cn(
                        "rounded-lg border bg-card p-3",
                        open ? "border-slate-400" : "border-slate-200"
                      )}
                    >
                      <div className="flex flex-wrap items-start justify-between gap-2">
                        <div>
                          <p className="font-mono text-sm text-slate-900">
                            {invoice.invoiceNumber ??
                              invoice.invoiceId.slice(0, 8)}
                          </p>
                          <p className="mt-0.5 text-xs text-slate-500">
                            {t("billing.outstanding")}{" "}
                            <span className="font-mono text-slate-700">
                              {formatMoney(
                                settlement!.outstandingAmount,
                                invoice.currency
                              )}
                            </span>
                            {invoice.dueAt !== null && (
                              <>
                                {" · "}
                                <LocalTime value={invoice.dueAt} dateOnly />
                              </>
                            )}
                          </p>
                        </div>

                        <Button
                          variant={open ? "outline" : "ghost"}
                          size="sm"
                          className="h-8 text-xs"
                          onClick={() => {
                            setAllocatingTo(open ? null : invoice.invoiceId);
                            setAllocationAmount("");
                          }}
                        >
                          {open
                            ? t("common.cancel")
                            : t("reconciliation.apply")}
                        </Button>
                      </div>

                      {open && (
                        <div className="mt-3 space-y-2 border-t border-slate-100 pt-3">
                          <label className="block text-xs text-slate-500">
                            {t("reconciliation.amountOptional")}
                          </label>
                          <Input
                            value={allocationAmount}
                            onChange={(event) =>
                              setAllocationAmount(event.target.value)
                            }
                            inputMode="decimal"
                            placeholder="0.00"
                            className="max-w-40 font-mono"
                          />
                          <p className="text-xs text-slate-500">
                            {t(ALLOCATE_EVERYTHING_BY_DEFAULT)}
                          </p>

                          <Button
                            size="sm"
                            disabled={saving}
                            onClick={() => allocate(invoice)}
                          >
                            {saving
                              ? t("reconciliation.applying")
                              : t("reconciliation.applyToThisInvoice")}
                          </Button>
                        </div>
                      )}
                    </li>
                  );
                })}
              </ul>
            )}
          </div>

          {mayRefund && (
            <div className="rounded-xl border border-slate-200 bg-card p-4">
              <p className="font-mono text-xs uppercase tracking-widest text-slate-500">
                {t("reconciliation.refund")}
              </p>
              <p className="mt-1 max-w-2xl text-sm text-attention-700">
                {t(REFUND_RECORDS_IT_ONLY)}
              </p>

              {!refundOpen ? (
                <Button
                  variant="outline"
                  size="sm"
                  className="mt-3"
                  onClick={() => setRefundOpen(true)}
                >
                  {t("reconciliation.recordARefund")}
                </Button>
              ) : (
                <div className="mt-3 space-y-3 border-t border-slate-100 pt-3">
                  <div>
                    <label className="block text-xs text-slate-500">
                      {t("common.amount")} ({currency})
                    </label>
                    <Input
                      value={refundAmount}
                      onChange={(event) => setRefundAmount(event.target.value)}
                      inputMode="decimal"
                      placeholder="0.00"
                      className="mt-1 max-w-40 font-mono"
                    />
                    <p className="mt-1 text-xs text-slate-400">
                      {t("reconciliation.inTheBalancesOwnCurrency")}
                    </p>
                  </div>

                  <div>
                    <label className="block text-xs text-slate-500">
                      {t("reconciliation.reason")}
                    </label>
                    <Input
                      value={refundReason}
                      onChange={(event) => setRefundReason(event.target.value)}
                      className="mt-1"
                    />
                    <p className="mt-1 text-xs text-slate-400">
                      {t(REFUND_REASON_IS_THE_RECORD)}
                    </p>
                  </div>

                  <div className="flex items-center gap-2">
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => setRefundOpen(false)}
                      disabled={saving}
                    >
                      {t("common.cancel")}
                    </Button>
                    <Button
                      size="sm"
                      disabled={
                        saving ||
                        refundReason.trim() === "" ||
                        refundAmount.trim() === ""
                      }
                      onClick={refund}
                    >
                      {saving
                        ? t("reconciliation.recording")
                        : t("reconciliation.recordTheRefund")}
                    </Button>
                  </div>
                </div>
              )}
            </div>
          )}
        </div>

        <DialogFooter>
          <Button variant="ghost" onClick={onClose} disabled={saving}>
            {t("common.close")}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
