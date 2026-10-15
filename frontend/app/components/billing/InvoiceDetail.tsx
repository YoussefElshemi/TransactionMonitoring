"use client";

import { useState } from "react";
import { toast } from "sonner";
import { CreditCard, Download, TriangleAlert } from "lucide-react";

import type { DocumentParty, Invoice, InvoiceSettlement } from "@/types";
import { API_BASE, reportApiError, unwrap } from "@/lib/api";
import {
  CURRENCY_ON_THE_PLAN,
  conversionSentence,
  formatMoney,
  wasConverted,
} from "@/lib/currency";
import { saveResponseAsFile } from "@/lib/download";
import {
  COLLECTION_REFUSAL_HINTS,
  COLLECTION_REFUSAL_TITLES,
  DRAFT_HAS_NO_SETTLEMENT,
  OUTSTANDING_IS_FLOORED,
  SETTLED_IS_NOT_PAID,
  SETTLEMENT_ROUTE_HINTS,
  canCollect,
  readCollectionRefusal,
  settlementRoute,
} from "@/lib/payments";
import { apiFetch } from "@/lib/http";
import {
  ALREADY_ISSUED_MESSAGE,
  CANNOT_DELETE_ISSUED_MESSAGE,
  DRAFT_HAS_NO_NUMBER,
  DRAFT_IS_NOT_A_DOCUMENT,
  ISSUED_IS_IMMUTABLE,
  INVOICE_STATUS_HINTS,
  INVOICE_STATUS_LABELS,
  NO_PARTIES_DETAIL,
  NO_PARTIES_HEADLINE,
  NUMBERING_IS_GAP_FREE,
  NUMBER_COMES_FROM_THE_SERIES,
  TOTALS_DISAGREE,
  ZERO_TAX_IS_LOAD_BEARING,
  canDelete,
  canIssue,
  daysOverdue,
  exportFileName,
  isOverdue,
  isTaxLine,
  paymentTermsSentence,
  predatesParties,
  totalsBalance,
} from "@/lib/invoiceDocument";
import { invoiceSnapshotSentence, versionLabel } from "@/lib/plans";
import {
  BILLABLE_RESOURCE_LABELS,
  INVOICE_LINE_TYPE_HINTS,
  INVOICE_LINE_TYPE_LABELS,
  NO_LINES_DETAIL,
  NO_LINES_HEADLINE,
  TOTAL_DISAGREES,
  UNCLASSIFIED_HINT,
  ZERO_LINES_ARE_DELIBERATE,
  formatAmount,
  formatPercentage,
  formatQuantity,
  invoicePeriod,
  isCredit,
  linesInOrder,
  showsQuantity,
  totalOf,
  totalsAgree,
} from "@/lib/pricing";
import { formatRate } from "@/lib/plans";
import { usePermissions } from "@/context/PermissionsContext";
import { useIdempotencyKey } from "@/lib/useIdempotencyKey";
import {
  REVERSE_CHARGE_EXPLAINED,
  TAX_TREATMENT_HINTS,
  TAX_TREATMENT_LABELS,
  countryName,
} from "@/lib/tax";
import { cn } from "@/lib/utils";
import { LocalTime } from "@/components/LocalTime";
import { StatusBadge } from "@/components/StatusBadge";
import { Button } from "@/components/ui/button";
import { CreditNotesPanel } from "@/components/billing/CreditNotesPanel";
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

export function InvoiceDetail({
  invoice,
  organizationId,
  settlement,
  planName = null,
  onUnauthorized,
  onIssued,
  onWithdrawn,
  onCollected,
  now = new Date(),
}: {
  invoice: Invoice;
  organizationId: string;
  settlement?: InvoiceSettlement;
  planName?: string | null;
  onUnauthorized: () => void;
  onIssued?: (invoice: Invoice) => void;
  onWithdrawn?: () => void;
  onCollected?: () => void;
  now?: Date;
}) {
  const t = useTranslation();
  const { holds } = usePermissions();
  const submission = useIdempotencyKey();

  const mayWrite = holds("invoices:write");
  const mayReadCreditNotes = holds("credit-notes:read");
  const mayCollect = holds("payments:collect");

  const [issuing, setIssuing] = useState(false);
  const [issueOpen, setIssueOpen] = useState(false);
  const [deleteOpen, setDeleteOpen] = useState(false);
  const [deleting, setDeleting] = useState(false);
  const [exporting, setExporting] = useState(false);
  const [collectOpen, setCollectOpen] = useState(false);
  const [collecting, setCollecting] = useState(false);
  const [needsPaymentMethod, setNeedsPaymentMethod] = useState(false);

  const lines = linesInOrder(invoice.lines);
  const bodyLines = lines.filter((line) => !isTaxLine(line));
  const taxLine = lines.find(isTaxLine) ?? null;

  const agrees = totalsAgree(invoice);
  const balances = totalsBalance(invoice);
  const overdue = isOverdue(invoice, now);

  const issue = async () => {
    setIssuing(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationId}/invoices/${invoice.invoiceId}/issue`,
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
        },
        { idempotencyKey: submission.current("issue") }
      );

      if (response.ok) {
        const issued = await unwrap<Invoice>(response);
        submission.completed("issue");

        toast.success(t("billing.issuedAs", issued.invoiceNumber ?? ""), {
          description: ISSUED_IS_IMMUTABLE,
        });

        setIssueOpen(false);
        onIssued?.(issued);
      } else if (response.status === 409) {
        toast.error(t("billing.alreadyIssued"), { description: ALREADY_ISSUED_MESSAGE });
      } else {
        await reportApiError(response, {
          fallback: t("billing.invoiceCouldNotIssued"),
          onUnauthorized,
          toastId: `issue-invoice-${invoice.invoiceId}`,
        });
      }
    } catch {
      toast.error(t("billing.invoiceCouldNotIssued"), {
        id: `issue-invoice-${invoice.invoiceId}`,
      });
    } finally {
      setIssuing(false);
    }
  };

  const withdraw = async () => {
    setDeleting(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationId}/invoices/${invoice.invoiceId}`,
        {
          method: "DELETE",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
        },
        { idempotencyKey: submission.current("delete") }
      );

      if (response.ok) {
        submission.completed("delete");
        toast.success(t("billing.draftWithdrawn"), {
          description:
            t("billing.nothingIssuedNobodySentGenerate"),
        });

        setDeleteOpen(false);
        onWithdrawn?.();
      } else if (response.status === 409) {
        toast.error(t("billing.invoiceHasBeenIssued"), {
          description: CANNOT_DELETE_ISSUED_MESSAGE,
        });
      } else {
        await reportApiError(response, {
          fallback: t("billing.draftCouldNotWithdrawn"),
          onUnauthorized,
          toastId: `delete-invoice-${invoice.invoiceId}`,
        });
      }
    } catch {
      toast.error(t("billing.draftCouldNotWithdrawn"), {
        id: `delete-invoice-${invoice.invoiceId}`,
      });
    } finally {
      setDeleting(false);
    }
  };

  const collect = async () => {
    setCollecting(true);
    setNeedsPaymentMethod(false);

    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationId}/invoices/${invoice.invoiceId}/collect`,
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
          body: JSON.stringify({ paymentMethodReference: null }),
        },
        { idempotencyKey: submission.current("collect") }
      );

      if (response.ok) {
        submission.completed("collect");

        toast.success(t("billing.paymentRequested"), {
          description: t("billing.collectionRaisedAgainstWhatever"),
        });

        setCollectOpen(false);
        onCollected?.();
        return;
      }

      if (response.status === 409) {
        const body = await response.json().catch(() => null);
        const refusal = readCollectionRefusal(body);

        if (refusal === "no-payment-method") {
          setNeedsPaymentMethod(true);
          return;
        }

        if (refusal !== null) {
          toast.error(t(COLLECTION_REFUSAL_TITLES[refusal]), {
            id: `collect-invoice-${invoice.invoiceId}`,
            description:
              (body as { detail?: string } | null)?.detail ??
              t(COLLECTION_REFUSAL_HINTS[refusal]),
          });

          if (refusal === "invoice-settled") {
            setCollectOpen(false);
            onCollected?.();
          }
          return;
        }

        toast.error(
          (body as { title?: string } | null)?.title ??
            t("billing.paymentCouldNotBeRaised"),
          {
            id: `collect-invoice-${invoice.invoiceId}`,
            description: (body as { detail?: string } | null)?.detail,
          }
        );
        return;
      }

      await reportApiError(response, {
        fallback: t("billing.paymentCouldNotBeRaised"),
        onUnauthorized,
        toastId: `collect-invoice-${invoice.invoiceId}`,
      });
    } catch {
      toast.error(t("billing.paymentCouldNotBeRaised"), {
        id: `collect-invoice-${invoice.invoiceId}`,
      });
    } finally {
      setCollecting(false);
    }
  };

  const exportInvoice = async () => {
    setExporting(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationId}/invoices/${invoice.invoiceId}/export`,
        { method: "POST", credentials: "include" }
      );

      if (!response.ok) {
        await reportApiError(response, {
          fallback: t("billing.invoiceCouldNotExported"),
          onUnauthorized,
          toastId: `export-invoice-${invoice.invoiceId}`,
        });
        return;
      }

      await saveResponseAsFile(response, exportFileName(invoice));
    } catch {
      toast.error(t("billing.invoiceCouldNotExported"), {
        id: `export-invoice-${invoice.invoiceId}`,
      });
    } finally {
      setExporting(false);
    }
  };

  return (
    <div className="space-y-4">
      <div className="rounded-xl border border-slate-200 bg-slate-50 p-4">
        <div className="flex flex-wrap items-start justify-between gap-3">
          <div>
            <div className="flex items-center gap-2">
              <p className="font-mono text-lg text-slate-900">
                {invoice.invoiceNumber ?? DRAFT_HAS_NO_NUMBER}
              </p>
              <StatusBadge
                status={invoice.invoiceStatus}
                label={INVOICE_STATUS_LABELS[invoice.invoiceStatus]}
              />
            </div>
            <p className="mt-1 text-sm text-slate-600">
              {invoicePeriod(invoice)}
            </p>
          </div>

          {overdue && (
            <span className="rounded-md border border-attention-200 bg-attention-50 px-2 py-1 font-mono text-[11px] text-attention-700 ring-1 ring-inset ring-attention-200">
              Overdue by {daysOverdue(invoice, now)} day
              {daysOverdue(invoice, now) === 1 ? "" : "s"}
            </span>
          )}
        </div>

        <p className="mt-2 max-w-2xl text-xs text-slate-500">
          {INVOICE_STATUS_HINTS[invoice.invoiceStatus]}
        </p>
      </div>

      {predatesParties(invoice) ? (
        <div className="rounded-xl border border-slate-200 bg-card p-4">
          <p className="font-mono text-xs uppercase tracking-widest text-slate-500">
            {NO_PARTIES_HEADLINE}
          </p>
          <p className="mt-2 max-w-2xl text-sm text-slate-500">
            {NO_PARTIES_DETAIL}
          </p>
        </div>
      ) : (
        <div className="grid gap-3 sm:grid-cols-2">
          <PartyBlock heading="From" party={invoice.supplier} />
          <PartyBlock heading="To" party={invoice.customer} />
        </div>
      )}

      <div className="grid gap-3 sm:grid-cols-3">
        <div className="rounded-lg border border-slate-200 bg-card p-3">
          <p className="font-mono text-xs uppercase tracking-widest text-slate-500">
            {t("billing.issued")}
          </p>
          <p className="mt-1 text-sm text-slate-900">
            {invoice.issuedAt === null ? (
              "Not yet issued"
            ) : (
              <LocalTime value={invoice.issuedAt} dateOnly />
            )}
          </p>
        </div>

        <div className="rounded-lg border border-slate-200 bg-card p-3">
          <p className="font-mono text-xs uppercase tracking-widest text-slate-500">
            {t("billing.due")}
          </p>
          <p
            className={cn(
              "mt-1 text-sm",
              overdue ? "text-attention-700" : "text-slate-900"
            )}
          >
            {invoice.dueAt === null ? (
              "Nothing to be late by yet"
            ) : (
              <LocalTime value={invoice.dueAt} dateOnly />
            )}
          </p>
          <p className="mt-0.5 text-xs text-slate-400">
            {paymentTermsSentence(invoice.paymentTerms)}
          </p>
        </div>

        <div className="rounded-lg border border-slate-200 bg-card p-3">
          <p className="font-mono text-xs uppercase tracking-widest text-slate-500">
            {t("billing.pricedFrom")}
          </p>
          <p className="mt-1 text-sm text-slate-900">
            {invoice.planVersionId === null
              ? "Predates plans"
              : versionLabel(planName, invoice.planVersionNumber)}
          </p>
          <p className="mt-0.5 text-xs text-slate-400">
            {invoice.isProcessed ? "Processed" : "Outstanding"}
          </p>
        </div>
      </div>

      {lines.length === 0 ? (
        <div className="rounded-xl border border-slate-200 bg-card p-6">
          <p className="font-mono text-xs uppercase tracking-widest text-slate-500">
            {NO_LINES_HEADLINE}
          </p>
          <p className="mt-2 max-w-xl text-sm text-slate-500">
            {NO_LINES_DETAIL}
          </p>
          <p className="mt-3 text-sm text-slate-600">
            {invoiceSnapshotSentence(invoice)}
          </p>
          <p className="mt-3 font-mono text-2xl text-slate-900">
            {formatAmount(invoice.amount, invoice.currency)}
          </p>
        </div>
      ) : (
        <>
          <div className="overflow-hidden rounded-xl border border-slate-200 bg-card">
            <Table>
              <TableHeader>
                <TableRow className="bg-slate-50 hover:bg-slate-50">
                  <TableHead>{t("billing.line")}</TableHead>
                  <TableHead className="w-32">{t("common.kind")}</TableHead>
                  <TableHead className="w-28 text-right">{t("billing.quantity")}</TableHead>
                  <TableHead className="w-32 text-right">{t("billing.each")}</TableHead>
                  <TableHead className="w-32 text-right">{t("common.amount")}</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {bodyLines.map((line) => {
                  const credit = isCredit(line);
                  const priced = showsQuantity(line);

                  return (
                    <TableRow
                      key={line.invoiceLineId}
                      className="hover:bg-slate-50/50"
                    >
                      <TableCell className="text-sm text-slate-700">
                        {line.description}
                        {line.billableResource === "Unclassified" && (
                          <span className="block text-xs text-slate-400">
                            {UNCLASSIFIED_HINT}
                          </span>
                        )}
                        {priced && line.amount === 0 && (
                          <span className="block text-xs text-slate-400">
                            {t("billing.meteredAndChargedAtNothing")}
                          </span>
                        )}
                      </TableCell>

                      <TableCell className="text-xs text-slate-500">
                        {line.billableResource !== null &&
                        line.invoiceLineType === "Usage"
                          ? BILLABLE_RESOURCE_LABELS[line.billableResource]
                          : INVOICE_LINE_TYPE_LABELS[line.invoiceLineType]}
                      </TableCell>

                      <TableCell className="text-right font-mono text-xs text-slate-600">
                        {priced ? formatQuantity(line.quantity) : ""}
                      </TableCell>

                      <TableCell className="text-right font-mono text-xs text-slate-600">
                        {priced
                          ? formatRate(line.unitAmount, invoice.currency)
                          : ""}
                      </TableCell>

                      <TableCell
                        className={cn(
                          "text-right font-mono text-sm",
                          credit ? "text-elevated-700" : "text-slate-900"
                        )}
                      >
                        {credit
                          ? `−${formatAmount(Math.abs(line.amount), invoice.currency)}`
                          : formatAmount(line.amount, invoice.currency)}
                      </TableCell>
                    </TableRow>
                  );
                })}
              </TableBody>
            </Table>
          </div>

          <div className="rounded-xl border border-slate-200 bg-card p-4">
            <dl className="ml-auto max-w-md space-y-2 text-sm">
              <div className="flex items-baseline justify-between gap-6">
                <dt className="text-slate-500">{t("billing.net")}</dt>
                <dd className="font-mono text-slate-900">
                  {formatMoney(invoice.netAmount, invoice.currency)}
                </dd>
              </div>

              <div className="flex items-baseline justify-between gap-6">
                <dt className="text-slate-500">
                  {t("billing.tax")}
                  <span className="block text-xs text-slate-400">
                    {TAX_TREATMENT_LABELS[invoice.taxTreatment]}
                    {invoice.taxRate > 0 &&
                      ` at ${formatPercentage(invoice.taxRate)}`}
                  </span>
                  {taxLine !== null && (
                    <span className="mt-1 block max-w-sm text-xs text-slate-500">
                      {taxLine.description}
                    </span>
                  )}
                </dt>
                <dd className="font-mono text-slate-900">
                  {formatMoney(invoice.taxAmount, invoice.currency)}
                </dd>
              </div>

              <div className="flex items-baseline justify-between gap-6 border-t border-slate-200 pt-2">
                <dt className="font-medium text-slate-900">{t("billing.total")}</dt>
                <dd className="font-mono text-base font-medium text-slate-900">
                  {formatMoney(invoice.amount, invoice.currency)}
                </dd>
              </div>
            </dl>

            {wasConverted(invoice) && invoice.conversionRate !== null && (
              <div className="mt-3 space-y-1 border-t border-slate-100 pt-3">
                <p className="text-xs text-slate-500">
                  {conversionSentence(
                    invoice.planCurrency,
                    invoice.currency,
                    invoice.conversionRate
                  )}
                </p>
                <p className="text-xs text-slate-400">{CURRENCY_ON_THE_PLAN}</p>
              </div>
            )}
          </div>

          {!agrees && (
            <p className="flex items-start gap-2 rounded-lg border border-critical-200 bg-critical-50 p-3 text-xs text-critical-700">
              <TriangleAlert className="mt-0.5 size-3.5 flex-shrink-0" />
              <span>
                {TOTAL_DISAGREES} The lines above come to{" "}
                <span className="font-mono">
                  {formatAmount(totalOf(lines), invoice.currency)}
                </span>
                ; the document states{" "}
                <span className="font-mono">
                  {formatAmount(invoice.amount, invoice.currency)}
                </span>
                .
              </span>
            </p>
          )}

          {!balances && (
            <p className="flex items-start gap-2 rounded-lg border border-critical-200 bg-critical-50 p-3 text-xs text-critical-700">
              <TriangleAlert className="mt-0.5 size-3.5 flex-shrink-0" />
              <span>{TOTALS_DISAGREE}</span>
            </p>
          )}

          <div className="space-y-1 text-xs text-slate-400">
            <p>{ZERO_LINES_ARE_DELIBERATE}</p>
            <p>{ZERO_TAX_IS_LOAD_BEARING}</p>
            {invoice.taxTreatment === "ReverseCharge" ? (
              <p>{REVERSE_CHARGE_EXPLAINED}</p>
            ) : (
              <p>{TAX_TREATMENT_HINTS[invoice.taxTreatment]}</p>
            )}
            <p>{INVOICE_LINE_TYPE_HINTS.Tax}</p>
          </div>
        </>
      )}

      <div className="rounded-xl border border-slate-200 bg-card p-4">
        <p className="font-mono text-xs uppercase tracking-widest text-slate-500">
          {t("billing.settlement")}
        </p>

        {settlement === undefined ? (
          <p className="mt-2 max-w-2xl text-sm text-slate-500">
            {t(DRAFT_HAS_NO_SETTLEMENT)}
          </p>
        ) : (
          <>
            <dl className="ml-auto mt-2 max-w-md space-y-2 text-sm">
              <div className="flex items-baseline justify-between gap-6">
                <dt className="text-slate-500">{t("billing.invoiced")}</dt>
                <dd className="font-mono text-slate-900">
                  {formatMoney(settlement.amount, invoice.currency)}
                </dd>
              </div>

              <div className="flex items-baseline justify-between gap-6">
                <dt className="text-slate-500">{t("billing.paid")}</dt>
                <dd className="font-mono text-slate-900">
                  {formatMoney(settlement.allocatedAmount, invoice.currency)}
                </dd>
              </div>

              <div className="flex items-baseline justify-between gap-6">
                <dt className="text-slate-500">
                  {t("billing.credited")}
                  <span className="block text-xs text-slate-400">
                    {t("billing.takenOffByCreditNotes")}
                  </span>
                </dt>
                <dd className="font-mono text-slate-900">
                  {formatMoney(settlement.creditedAmount, invoice.currency)}
                </dd>
              </div>

              <div className="flex items-baseline justify-between gap-6 border-t border-slate-200 pt-2">
                <dt className="font-medium text-slate-900">
                  {t("billing.outstanding")}
                </dt>
                <dd
                  className={cn(
                    "font-mono text-base font-medium",
                    settlement.isSettled
                      ? "text-settled-700"
                      : overdue
                        ? "text-attention-700"
                        : "text-slate-900"
                  )}
                >
                  {formatMoney(settlement.outstandingAmount, invoice.currency)}
                </dd>
              </div>
            </dl>

            <div className="mt-3 space-y-1 border-t border-slate-100 pt-3 text-xs text-slate-500">
              {settlement.isSettled && (
                <>
                  <p className="text-settled-700">
                    {t("billing.nothingOutstanding")} —{" "}
                    {t(SETTLEMENT_ROUTE_HINTS[settlementRoute(settlement)])}
                  </p>
                  <p className="text-slate-400">{t(SETTLED_IS_NOT_PAID)}</p>
                </>
              )}
              {settlement.outstandingAmount === 0 && !settlement.isSettled && (
                <p>{t(OUTSTANDING_IS_FLOORED)}</p>
              )}
              {settlement.allocatedAmount > settlement.amount && (
                <p>{t(OUTSTANDING_IS_FLOORED)}</p>
              )}
            </div>
          </>
        )}
      </div>

      {mayReadCreditNotes && (
        <div className="border-t border-slate-200 pt-4">
          <CreditNotesPanel
            organizationId={organizationId}
            invoice={invoice}
            onUnauthorized={onUnauthorized}
          />
        </div>
      )}

      <div className="flex flex-wrap items-center gap-2 border-t border-slate-200 pt-4">
        <Button
          variant="outline"
          size="sm"
          disabled={exporting}
          onClick={exportInvoice}
        >
          <Download />
          {exporting ? "Preparing..." : "Export PDF"}
        </Button>

        {mayWrite && canIssue(invoice) && (
          <Button size="sm" onClick={() => setIssueOpen(true)}>
            {t("billing.issue")}
          </Button>
        )}

        {mayCollect && canCollect(invoice, settlement) && (
          <Button
            size="sm"
            variant="outline"
            onClick={() => {
              setNeedsPaymentMethod(false);
              setCollectOpen(true);
            }}
          >
            <CreditCard />
            {t("billing.payNow")}
          </Button>
        )}

        {mayWrite && canDelete(invoice) && (
          <Button
            variant="ghost"
            size="sm"
            className="text-critical-700 hover:text-critical-700"
            onClick={() => setDeleteOpen(true)}
          >
            {t("billing.withdrawDraft")}
          </Button>
        )}

        {canDelete(invoice) && (
          <p className="w-full text-xs text-slate-400">
            {DRAFT_IS_NOT_A_DOCUMENT}
          </p>
        )}
      </div>

      <Dialog open={issueOpen} onOpenChange={setIssueOpen}>
        <DialogContent className="sm:max-w-lg">
          <DialogHeader>
            <DialogTitle>{t("billing.issueThisInvoice")}</DialogTitle>
            <DialogDescription>{ISSUED_IS_IMMUTABLE}</DialogDescription>
          </DialogHeader>

          <div className="space-y-2 text-sm text-slate-600">
            <p>{NUMBER_COMES_FROM_THE_SERIES}</p>
            <p className="text-xs text-slate-500">{NUMBERING_IS_GAP_FREE}</p>
            <p className="text-xs text-slate-500">
              {invoicePeriod(invoice)} comes to{" "}
              <span className="font-mono text-slate-800">
                {formatMoney(invoice.amount, invoice.currency)}
              </span>
              , and {paymentTermsSentence(invoice.paymentTerms).toLowerCase()}.
            </p>
          </div>

          <DialogFooter>
            <Button
              variant="ghost"
              onClick={() => setIssueOpen(false)}
              disabled={issuing}
            >
              {t("common.cancel")}
            </Button>
            <Button onClick={issue} disabled={issuing}>
              {issuing ? "Issuing..." : "Issue"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <Dialog
        open={collectOpen}
        onOpenChange={(open) => {
          setCollectOpen(open);
          if (!open) setNeedsPaymentMethod(false);
        }}
      >
        <DialogContent className="sm:max-w-lg">
          <DialogHeader>
            <DialogTitle>{t("billing.payThisInvoice")}</DialogTitle>
            <DialogDescription>
              {t("billing.collectsAgainstWhateverIsOnFile")}
            </DialogDescription>
          </DialogHeader>

          {needsPaymentMethod ? (
            <div className="space-y-2 rounded-lg border border-attention-200 bg-attention-50 p-3 text-sm text-attention-700">
              <p className="font-medium">
                {t(COLLECTION_REFUSAL_TITLES["no-payment-method"])}
              </p>
              <p className="text-xs">
                {t(COLLECTION_REFUSAL_HINTS["no-payment-method"])}
              </p>
            </div>
          ) : (
            <div className="space-y-2 text-sm text-slate-600">
              {settlement !== undefined && (
                <p>
                  {t("billing.thisWillCollect")}{" "}
                  <span className="font-mono text-slate-900">
                    {formatMoney(
                      settlement.outstandingAmount,
                      invoice.currency
                    )}
                  </span>
                  {invoice.invoiceNumber !== null && (
                    <>
                      {" "}
                      <span className="text-slate-500">
                        {invoice.invoiceNumber}
                      </span>
                    </>
                  )}
                  .
                </p>
              )}
              <p className="text-xs text-slate-500">
                {t("billing.aCardMaySettleInSeconds")}
              </p>
            </div>
          )}

          <DialogFooter>
            <Button
              variant="ghost"
              onClick={() => setCollectOpen(false)}
              disabled={collecting}
            >
              {needsPaymentMethod ? t("common.close") : t("common.cancel")}
            </Button>
            {!needsPaymentMethod && (
              <Button onClick={collect} disabled={collecting}>
                {collecting ? t("billing.requesting") : t("billing.payNow")}
              </Button>
            )}
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <Dialog open={deleteOpen} onOpenChange={setDeleteOpen}>
        <DialogContent className="sm:max-w-lg">
          <DialogHeader>
            <DialogTitle>{t("billing.withdrawThisDraft")}</DialogTitle>
            <DialogDescription>{DRAFT_IS_NOT_A_DOCUMENT}</DialogDescription>
          </DialogHeader>

          <p className="text-sm text-slate-600">
            Nothing was issued and nobody has been sent it, so there is nothing
            to correct and no number to leave a hole where. Generate{" "}
            {invoicePeriod(invoice)} again once whatever it was billed against
            is right.
          </p>

          <DialogFooter>
            <Button
              variant="ghost"
              onClick={() => setDeleteOpen(false)}
              disabled={deleting}
            >
              {t("common.cancel")}
            </Button>
            <Button
              variant="destructive"
              onClick={withdraw}
              disabled={deleting}
            >
              {deleting ? "Withdrawing..." : "Withdraw"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}

function PartyBlock({
  heading,
  party,
}: {
  heading: string;
  party: DocumentParty | null;
}) {
  const t = useTranslation();
  return (
    <div className="rounded-lg border border-slate-200 bg-card p-3">
      <p className="font-mono text-xs uppercase tracking-widest text-slate-500">
        {heading}
      </p>

      {party === null ? (
        <p className="mt-1 text-sm text-slate-400">{t("billing.notRecorded")}</p>
      ) : (
        <>
          <p className="mt-1 text-sm font-medium text-slate-900">
            {party.legalName}
          </p>
          <p className="mt-1 whitespace-pre-line text-xs text-slate-600">
            {party.address}
          </p>
          <p className="mt-1 text-xs text-slate-500">
            {countryName(party.countryCode)}
          </p>
          {party.taxIdentifier !== null && (
            <p className="mt-1 font-mono text-xs text-slate-600">
              {party.taxIdentifier}
            </p>
          )}
          {party.email !== null && (
            <p className="mt-1 text-xs text-slate-500">{party.email}</p>
          )}
        </>
      )}
    </div>
  );
}
