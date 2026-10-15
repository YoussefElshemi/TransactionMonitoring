"use client";

import { useCallback, useEffect, useState } from "react";
import { toast } from "sonner";
import { Download, Undo2 } from "lucide-react";

import type { CreditNote, Invoice } from "@/types";
import { API_BASE, reportApiError, unwrap, unwrapPage } from "@/lib/api";
import { formatMoney } from "@/lib/currency";
import {
  AMOUNT_HINT,
  CANNOT_CREDIT_DRAFT_MESSAGE,
  CREDIT_EXCEEDS_INVOICE_MESSAGE,
  CREDIT_IS_IN_THE_INVOICE_CURRENCY,
  NO_CREDIT_NOTES_DETAIL,
  REASON_HINT,
  REASON_MAX_LENGTH,
  creditNotesNewestFirst,
  creditRefusal,
  exportFileName,
  isFullyCredited,
  remainingSentence,
  uncreditedAmount,
} from "@/lib/creditNotes";
import { canCredit } from "@/lib/invoiceDocument";
import { saveResponseAsFile } from "@/lib/download";
import { apiFetch } from "@/lib/http";
import { canWriteBilling } from "@/lib/permissions";
import { usePermissions } from "@/context/PermissionsContext";
import { useIdempotencyKey } from "@/lib/useIdempotencyKey";
import Pagination from "@/components/Pagination";
import { LocalTime } from "@/components/LocalTime";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import { Textarea } from "@/components/ui/textarea";
import { CreditNoteDetail } from "@/components/billing/CreditNoteDetail";
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
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { useTranslation } from "@/context/LocalizationContext";

export function CreditNotesPanel({
  organizationId,
  invoice = null,
  onUnauthorized,
  onChanged,
}: {
  organizationId: string;
  invoice?: Invoice | null;
  onUnauthorized: () => void;
  onChanged?: () => void;
}) {
  const t = useTranslation();
  const { grants, holds, resolved } = usePermissions();
  const submission = useIdempotencyKey();

  const mayRead = holds("credit-notes:read");
  const canWrite = canWriteBilling(grants, "credit-notes:write");

  const [creditNotes, setCreditNotes] = useState<CreditNote[]>([]);
  const [pageNumber, setPageNumber] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [loading, setLoading] = useState(true);

  const [opened, setOpened] = useState<CreditNote | null>(null);
  const [exporting, setExporting] = useState<string | null>(null);

  const [raiseOpen, setRaiseOpen] = useState(false);
  const [raising, setRaising] = useState(false);
  const [reason, setReason] = useState("");
  const [amount, setAmount] = useState("");

  const invoiceId = invoice?.invoiceId ?? null;

  const fetchCreditNotes = useCallback(async () => {
    setLoading(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationId}/credit-notes?PageNumber=${pageNumber}&PageSize=25` +
          (invoiceId === null ? "" : `&InvoiceId=${invoiceId}`),
        {
          method: "GET",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
        }
      );

      if (response.ok) {
        const page = await unwrapPage<CreditNote>(response);
        setCreditNotes(page.data);
        setTotalPages(page.metadata.pageCount);
      } else {
        await reportApiError(response, {
          fallback: t("billing.errorOccurredFetchingCreditNotes"),
          onUnauthorized,
          toastId: "error-fetching-credit-notes",
        });
      }
    } catch {
      toast.error(t("billing.errorOccurredFetchingCreditNotes"), {
        id: "error-fetching-credit-notes",
      });
    } finally {
      setLoading(false);
    }
  }, [organizationId, invoiceId, pageNumber, onUnauthorized]);

  useEffect(() => {
    if (mayRead) fetchCreditNotes();
    else setLoading(false);
  }, [mayRead, fetchCreditNotes]);

  const remaining =
    invoice === null ? null : uncreditedAmount(invoice, creditNotes);

  const refusal =
    invoice === null
      ? null
      : creditRefusal(reason, amount, remaining ?? undefined);

  const raise = async () => {
    if (invoice === null) return;

    setRaising(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationId}/invoices/${invoice.invoiceId}/credit-notes`,
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
          body: JSON.stringify({
            reason: reason.trim(),
            ...(amount.trim().length > 0
              ? { netAmount: Number(amount.trim()) }
              : {}),
          }),
        },
        { idempotencyKey: submission.current("raise") }
      );

      if (response.ok) {
        const created = await unwrap<CreditNote>(response);
        submission.completed("raise");

        toast.success(t("billing.creditNoteRaised", created.creditNoteNumber), {
          description: t(
            "billing.creditedAgainst",
            formatMoney(created.amount, created.currency),
            created.invoiceNumber ?? t("billing.thisInvoice")
          ),
        });

        setRaiseOpen(false);
        setReason("");
        setAmount("");
        fetchCreditNotes();
        onChanged?.();
      } else if (response.status === 409) {
        toast.error(
          canCredit(invoice) ? t("billing.moreThanLeft") : t("billing.nothingCredit"),
          {
            description: canCredit(invoice)
              ? CREDIT_EXCEEDS_INVOICE_MESSAGE
              : CANNOT_CREDIT_DRAFT_MESSAGE,
          }
        );
      } else {
        await reportApiError(response, {
          fallback: t("billing.creditNoteCouldNotRaised"),
          onUnauthorized,
          toastId: "error-raising-credit-note",
        });
      }
    } catch {
      toast.error(t("billing.creditNoteCouldNotRaised"), {
        id: "error-raising-credit-note",
      });
    } finally {
      setRaising(false);
    }
  };

  const exportCreditNote = async (creditNote: CreditNote) => {
    setExporting(creditNote.creditNoteId);

    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationId}/credit-notes/${creditNote.creditNoteId}/export`,
        { method: "POST", credentials: "include" }
      );

      if (!response.ok) {
        await reportApiError(response, {
          fallback: t("billing.creditNoteCouldNotExported"),
          onUnauthorized,
          toastId: `export-credit-note-${creditNote.creditNoteId}`,
        });
        return;
      }

      await saveResponseAsFile(response, exportFileName(creditNote));
    } catch {
      toast.error(t("billing.creditNoteCouldNotExported"), {
        id: `export-credit-note-${creditNote.creditNoteId}`,
      });
    } finally {
      setExporting(null);
    }
  };

  if (!resolved) return null;

  if (!mayRead) {
    return (
      <div className="rounded-xl border border-slate-200 bg-card p-4">
        <p className="text-sm text-slate-500">
          {t("billing.needs")} <span className="font-mono text-xs">credit-notes:read</span>.
        </p>
      </div>
    );
  }

  const ordered = creditNotesNewestFirst(creditNotes);

  return (
    <div className="space-y-3">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div className="flex items-start gap-2">
          <Undo2 className="mt-0.5 size-3.5 flex-shrink-0 text-slate-400" />
          <div>
            <p className="font-mono text-xs uppercase tracking-widest text-slate-500">
              {t("billing.creditNotes")}
            </p>
            <p className="mt-1 max-w-2xl text-sm text-slate-500">
              {invoice === null
                ? "Everything credited back to this organisation, newest first. Each names the invoice it was raised against — a credit note read on its own says nothing."
                : remainingSentence(invoice, creditNotes)}
            </p>
          </div>
        </div>

        {invoice !== null && canWrite && canCredit(invoice) && (
          <Button
            variant="outline"
            size="sm"
            className="h-8 text-xs"
            disabled={isFullyCredited(invoice, creditNotes)}
            onClick={() => setRaiseOpen(true)}
          >
            {t("billing.raiseCreditNote")}
          </Button>
        )}
      </div>

      <div className="overflow-hidden rounded-xl border border-slate-200 bg-card">
        <Table>
          <TableHeader>
            <TableRow className="bg-slate-50 hover:bg-slate-50">
              <TableHead className="w-40">{t("billing.number")}</TableHead>
              {invoice === null && (
                <TableHead className="w-40">{t("billing.against")}</TableHead>
              )}
              <TableHead>{t("common.reason")}</TableHead>
              <TableHead className="w-32 text-right">{t("billing.credited")}</TableHead>
              <TableHead className="w-32">{t("billing.issued")}</TableHead>
              <TableHead className="w-32">{t("common.actions")}</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {loading ? (
              Array.from({ length: 2 }).map((_, row) => (
                <TableRow key={row}>
                  {Array.from({ length: invoice === null ? 6 : 5 }).map(
                    (_, cell) => (
                      <TableCell key={cell}>
                        <Skeleton className="h-4 w-full" />
                      </TableCell>
                    )
                  )}
                </TableRow>
              ))
            ) : ordered.length === 0 ? (
              <TableRow>
                <TableCell
                  colSpan={invoice === null ? 6 : 5}
                  className="py-8 text-center"
                >
                  <p className="font-mono text-xs uppercase tracking-widest text-slate-400">
                    {t("billing.nothingCredited")}
                  </p>
                  <p className="mx-auto mt-2 max-w-md text-sm text-slate-500">
                    {NO_CREDIT_NOTES_DETAIL}
                  </p>
                </TableCell>
              </TableRow>
            ) : (
              ordered.map((creditNote) => (
                <TableRow
                  key={creditNote.creditNoteId}
                  className="hover:bg-slate-50/50"
                >
                  <TableCell className="font-mono text-sm text-slate-900">
                    {creditNote.creditNoteNumber}
                  </TableCell>

                  {invoice === null && (
                    <TableCell className="font-mono text-xs text-slate-600">
                      {creditNote.invoiceNumber ?? "Predates numbering"}
                    </TableCell>
                  )}

                  <TableCell className="max-w-md truncate text-sm text-slate-600">
                    {creditNote.reason}
                  </TableCell>

                  <TableCell className="text-right font-mono text-sm text-slate-900">
                    {formatMoney(creditNote.amount, creditNote.currency)}
                  </TableCell>

                  <TableCell className="text-xs text-slate-500">
                    <LocalTime value={creditNote.issuedAt} dateOnly />
                  </TableCell>

                  <TableCell>
                    <div className="flex items-center gap-1">
                      <Button
                        variant="ghost"
                        size="sm"
                        className="h-8 text-xs"
                        onClick={() => setOpened(creditNote)}
                      >
                        {t("billing.open")}
                      </Button>
                      <Button
                        variant="ghost"
                        size="sm"
                        className="h-8 px-2 text-xs"
                        disabled={exporting === creditNote.creditNoteId}
                        onClick={() => exportCreditNote(creditNote)}
                      >
                        <Download className="size-3.5" />
                      </Button>
                    </div>
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </div>

      {ordered.length > 0 && totalPages > 1 && (
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
            <DialogTitle>{opened?.creditNoteNumber}</DialogTitle>
            <DialogDescription>
              {CREDIT_IS_IN_THE_INVOICE_CURRENCY}
            </DialogDescription>
          </DialogHeader>

          <div className="max-h-[70vh] overflow-y-auto pr-1">
            {opened !== null && <CreditNoteDetail creditNote={opened} />}
          </div>
        </DialogContent>
      </Dialog>

      <Dialog open={raiseOpen} onOpenChange={setRaiseOpen}>
        <DialogContent className="sm:max-w-lg">
          <DialogHeader>
            <DialogTitle>{t("billing.raiseACreditNote")}</DialogTitle>
            <DialogDescription>
              {invoice !== null && remainingSentence(invoice, creditNotes)}
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-4">
            <div className="space-y-1.5">
              <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
                {t("common.reason")}
              </label>
              <Textarea
                value={reason}
                onChange={(event) => setReason(event.target.value)}
                rows={3}
                maxLength={REASON_MAX_LENGTH}
                disabled={raising}
              />
              <p className="text-xs text-slate-500">{REASON_HINT}</p>
            </div>

            <div className="space-y-1.5">
              <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
                {t("billing.amountNet")}
              </label>
              <Input
                value={amount}
                onChange={(event) => setAmount(event.target.value)}
                inputMode="decimal"
                placeholder={
                  remaining === null || invoice === null
                    ? t("billing.everythingUncredited")
                    : `Everything uncredited — ${formatMoney(remaining, invoice.currency)}`
                }
                className="font-mono"
                disabled={raising}
              />
              <p className="text-xs text-slate-500">{AMOUNT_HINT}</p>
            </div>

            {refusal !== null && reason.trim().length > 0 && (
              <p className="text-xs text-critical-500">{refusal}</p>
            )}

            <p className="text-xs text-slate-400">
              {CREDIT_IS_IN_THE_INVOICE_CURRENCY}
            </p>
          </div>

          <DialogFooter>
            <Button
              variant="ghost"
              onClick={() => setRaiseOpen(false)}
              disabled={raising}
            >
              {t("common.cancel")}
            </Button>
            <Button onClick={raise} disabled={raising || refusal !== null}>
              {raising ? "Raising..." : "Raise credit note"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
