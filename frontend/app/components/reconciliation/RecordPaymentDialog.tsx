"use client";

import { useCallback, useEffect, useState } from "react";
import { toast } from "sonner";

import type { InvoiceResource } from "@/types";
import type { CurrencyCode } from "@/types";
import { API_BASE, reportApiError, unwrapPage } from "@/lib/api";
import { apiFetch } from "@/lib/http";
import { CURRENCY_CODES, CURRENCY_LABELS, formatMoney } from "@/lib/currency";
import {
  COLLECTION_REFUSAL_HINTS,
  COLLECTION_REFUSAL_TITLES,
  readCollectionRefusal,
} from "@/lib/payments";
import { useIdempotencyKey } from "@/lib/useIdempotencyKey";
import { cn } from "@/lib/utils";
import { LocalTime } from "@/components/LocalTime";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { useTranslation } from "@/context/LocalizationContext";

export function RecordPaymentDialog({
  organizationId,
  open,
  onClose,
  onRecorded,
  onUnauthorized,
}: {
  organizationId: string;
  open: boolean;
  onClose: () => void;
  onRecorded: () => void;
  onUnauthorized: () => void;
}) {
  const t = useTranslation();
  const submission = useIdempotencyKey();

  const [amount, setAmount] = useState("");
  const [currency, setCurrency] = useState<CurrencyCode>("GBP");
  const [receivedAt, setReceivedAt] = useState("");
  const [remittanceReference, setRemittanceReference] = useState("");
  const [invoiceId, setInvoiceId] = useState<string | null>(null);
  const [invoices, setInvoices] = useState<InvoiceResource[]>([]);
  const [saving, setSaving] = useState(false);

  const today = new Date().toISOString().slice(0, 10);

  const fetchInvoices = useCallback(async () => {
    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationId}/invoices?PageNumber=1&PageSize=25`,
        {
          method: "GET",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
        }
      );

      if (response.ok) {
        setInvoices((await unwrapPage<InvoiceResource>(response)).data);
      }
    } catch {
    }
  }, [organizationId]);

  useEffect(() => {
    if (open) {
      setAmount("");
      setReceivedAt(today);
      setRemittanceReference("");
      setInvoiceId(null);
      fetchInvoices();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open, fetchInvoices]);

  if (!open) return null;

  const attributable = invoices.filter(
    (resource) =>
      resource.invoice.invoiceStatus === "Issued" &&
      resource.invoice.currency === currency &&
      resource.settlement !== undefined &&
      !resource.settlement.isSettled
  );

  const inTheFuture = receivedAt > today;
  const canSave =
    amount.trim() !== "" &&
    Number(amount) > 0 &&
    receivedAt !== "" &&
    !inTheFuture;

  const record = async () => {
    setSaving(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationId}/payments`,
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
          body: JSON.stringify({
            amount: Number(amount),
            currency,
            // Midnight UTC on the stated day. The API takes a
            // `DateTimeOffset`, and the day is the only part anybody knows.
            receivedAt: new Date(`${receivedAt}T00:00:00Z`).toISOString(),
            remittanceReference:
              remittanceReference.trim() === ""
                ? null
                : remittanceReference.trim(),
            invoiceId,
          }),
        },
        { idempotencyKey: submission.current("record-payment") }
      );

      if (response.ok) {
        submission.completed("record-payment");

        toast.success(t("reconciliation.paymentRecorded"), {
          description:
            invoiceId === null
              ? t("reconciliation.itIsOnTheAccountAsUnapplied")
              : t("reconciliation.itHasBeenAppliedToThatInvoice"),
        });

        onRecorded();
        onClose();
        return;
      }

      if (response.status === 409) {
        const body = await response.json().catch(() => null);
        const refusal = readCollectionRefusal(body);

        if (refusal !== null) {
          toast.error(t(COLLECTION_REFUSAL_TITLES[refusal]), {
            id: "record-payment",
            description:
              (body as { detail?: string } | null)?.detail ??
              t(COLLECTION_REFUSAL_HINTS[refusal]),
          });
          return;
        }
      }

      await reportApiError(response, {
        fallback: t("reconciliation.paymentCouldNotBeRecorded"),
        onUnauthorized,
        toastId: "record-payment",
      });
    } catch {
      toast.error(t("reconciliation.paymentCouldNotBeRecorded"), {
        id: "record-payment",
      });
    } finally {
      setSaving(false);
    }
  };

  return (
    <Dialog open onOpenChange={(next) => !next && onClose()}>
      <DialogContent className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>{t("reconciliation.recordATransfer")}</DialogTitle>
          <DialogDescription>
            {t("reconciliation.moneyThatArrivedInTheBank")}
          </DialogDescription>
        </DialogHeader>

        <div className="max-h-[60vh] space-y-4 overflow-y-auto pr-1">
          <div className="grid gap-3 sm:grid-cols-2">
            <div>
              <label className="block text-xs text-slate-500">
                {t("common.amount")}
              </label>
              <Input
                value={amount}
                onChange={(event) => setAmount(event.target.value)}
                inputMode="decimal"
                placeholder="0.00"
                className="mt-1 font-mono"
              />
            </div>

            <div>
              <label className="block text-xs text-slate-500">
                {t("common.currency")}
              </label>
              <select
                value={currency}
                onChange={(event) => {
                  setCurrency(event.target.value as CurrencyCode);
                  setInvoiceId(null);
                }}
                className="mt-1 h-9 w-full rounded-md border border-slate-200 bg-card px-2 text-sm"
              >
                {CURRENCY_CODES.map((code) => (
                  <option key={code} value={code}>
                    {CURRENCY_LABELS[code]}
                  </option>
                ))}
              </select>
            </div>
          </div>

          <div>
            <label className="block text-xs text-slate-500">
              {t("reconciliation.dateItLanded")}
            </label>
            <Input
              type="date"
              value={receivedAt}
              max={today}
              onChange={(event) => setReceivedAt(event.target.value)}
              className="mt-1 max-w-48 font-mono"
            />
            <p className="mt-1 text-xs text-slate-400">
              {t("reconciliation.theDayItLandedNotToday")}
            </p>
            {inTheFuture && (
              <p className="mt-1 text-xs text-critical-700">
                {t("reconciliation.cannotBeInTheFuture")}
              </p>
            )}
          </div>

          <div>
            <label className="block text-xs text-slate-500">
              {t("reconciliation.remittanceReference")}
            </label>
            <Input
              value={remittanceReference}
              onChange={(event) => setRemittanceReference(event.target.value)}
              className="mt-1 font-mono"
            />
            <p className="mt-1 text-xs text-slate-400">
              {t("reconciliation.whateverThePayerWroteOnIt")}
            </p>
          </div>

          <div>
            <label className="block text-xs text-slate-500">
              {t("reconciliation.whatWasItFor")}
            </label>

            <div className="mt-2 space-y-1">
              <button
                type="button"
                onClick={() => setInvoiceId(null)}
                className={cn(
                  "w-full rounded-lg border p-3 text-left",
                  invoiceId === null
                    ? "border-slate-900 bg-slate-50"
                    : "border-slate-200"
                )}
              >
                <span className="block text-sm font-medium text-slate-900">
                  {t("reconciliation.dontKnowYet")}
                </span>
                <span className="mt-0.5 block text-xs text-slate-500">
                  {t("reconciliation.itStaysAsUnappliedCash")}
                </span>
              </button>

              {attributable.map(({ invoice, settlement }) => (
                <button
                  key={invoice.invoiceId}
                  type="button"
                  onClick={() => setInvoiceId(invoice.invoiceId)}
                  className={cn(
                    "w-full rounded-lg border p-3 text-left",
                    invoiceId === invoice.invoiceId
                      ? "border-slate-900 bg-slate-50"
                      : "border-slate-200"
                  )}
                >
                  <span className="block font-mono text-sm text-slate-900">
                    {invoice.invoiceNumber ?? invoice.invoiceId.slice(0, 8)}
                  </span>
                  <span className="mt-0.5 block text-xs text-slate-500">
                    {t("billing.outstanding")}{" "}
                    {formatMoney(
                      settlement!.outstandingAmount,
                      invoice.currency
                    )}
                    {invoice.dueAt !== null && (
                      <>
                        {" · "}
                        <LocalTime value={invoice.dueAt} dateOnly />
                      </>
                    )}
                  </span>
                </button>
              ))}
            </div>
          </div>
        </div>

        <DialogFooter>
          <Button variant="ghost" onClick={onClose} disabled={saving}>
            {t("common.cancel")}
          </Button>
          <Button onClick={record} disabled={saving || !canSave}>
            {saving
              ? t("reconciliation.recording")
              : t("reconciliation.recordIt")}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
