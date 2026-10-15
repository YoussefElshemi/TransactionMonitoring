"use client";

import { TriangleAlert } from "lucide-react";

import type { CreditNote } from "@/types";
import { formatMoney } from "@/lib/currency";
import {
  AMOUNTS_ARE_POSITIVE,
  TAX_FOLLOWS_THE_INVOICE,
  creditNoteHeading,
  linesInOrder,
  totalsBalance,
} from "@/lib/creditNotes";
import { formatPercentage, formatQuantity } from "@/lib/pricing";
import { TAX_TREATMENT_HINTS, TAX_TREATMENT_LABELS } from "@/lib/tax";
import { LocalTime } from "@/components/LocalTime";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { useTranslation } from "@/context/LocalizationContext";

export function CreditNoteDetail({ creditNote }: { creditNote: CreditNote }) {
  const t = useTranslation();
  const lines = linesInOrder(creditNote.lines);
  const balances = totalsBalance(creditNote);

  return (
    <div className="space-y-4">
      <div className="rounded-xl border border-slate-200 bg-slate-50 p-4">
        <p className="font-mono text-lg text-slate-900">
          {creditNote.creditNoteNumber}
        </p>
        <p className="mt-1 text-sm text-slate-600">
          {creditNoteHeading(creditNote)}
        </p>
        <p className="mt-1 text-xs text-slate-500">
          {t("billing.issued")} <LocalTime value={creditNote.issuedAt} dateOnly />
        </p>
      </div>

      <div className="rounded-lg border border-slate-200 bg-card p-3">
        <p className="font-mono text-xs uppercase tracking-widest text-slate-500">
          {t("common.reason")}
        </p>
        <p className="mt-1 whitespace-pre-line text-sm text-slate-700">
          {creditNote.reason}
        </p>
      </div>

      <div className="overflow-hidden rounded-xl border border-slate-200 bg-card">
        <Table>
          <TableHeader>
            <TableRow className="bg-slate-50 hover:bg-slate-50">
              <TableHead>{t("billing.line")}</TableHead>
              <TableHead className="w-28 text-right">{t("billing.quantity")}</TableHead>
              <TableHead className="w-32 text-right">{t("billing.each")}</TableHead>
              <TableHead className="w-32 text-right">{t("billing.credited")}</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {lines.length === 0 ? (
              <TableRow>
                <TableCell colSpan={4} className="py-8 text-center">
                  <p className="text-sm text-slate-500">
                    This credit note has no lines — the whole of it is the
                    summary below.
                  </p>
                </TableCell>
              </TableRow>
            ) : (
              lines.map((line) => (
                <TableRow
                  key={line.creditNoteLineId}
                  className="hover:bg-slate-50/50"
                >
                  <TableCell className="text-sm text-slate-700">
                    {line.description}
                  </TableCell>
                  <TableCell className="text-right font-mono text-xs text-slate-600">
                    {formatQuantity(line.quantity)}
                  </TableCell>
                  <TableCell className="text-right font-mono text-xs text-slate-600">
                    {formatMoney(line.unitAmount, creditNote.currency)}
                  </TableCell>
                  <TableCell className="text-right font-mono text-sm text-slate-900">
                    {formatMoney(line.amount, creditNote.currency)}
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </div>

      <div className="rounded-xl border border-slate-200 bg-card p-4">
        <dl className="ml-auto max-w-xs space-y-1.5 text-sm">
          <div className="flex items-baseline justify-between gap-6">
            <dt className="text-slate-500">{t("billing.net")}</dt>
            <dd className="font-mono text-slate-900">
              {formatMoney(creditNote.netAmount, creditNote.currency)}
            </dd>
          </div>
          <div className="flex items-baseline justify-between gap-6">
            <dt className="text-slate-500">
              {t("billing.tax")}
              <span className="block text-xs text-slate-400">
                {TAX_TREATMENT_LABELS[creditNote.taxTreatment]}
                {creditNote.taxRate > 0 &&
                  ` at ${formatPercentage(creditNote.taxRate)}`}
              </span>
            </dt>
            <dd className="font-mono text-slate-900">
              {formatMoney(creditNote.taxAmount, creditNote.currency)}
            </dd>
          </div>
          <div className="flex items-baseline justify-between gap-6 border-t border-slate-200 pt-1.5">
            <dt className="font-medium text-slate-900">{t("billing.credited")}</dt>
            <dd className="font-mono text-base font-medium text-slate-900">
              {formatMoney(creditNote.amount, creditNote.currency)}
            </dd>
          </div>
        </dl>
      </div>

      {!balances && (
        <p className="flex items-start gap-2 rounded-lg border border-critical-200 bg-critical-50 p-3 text-xs text-critical-700">
          <TriangleAlert className="mt-0.5 size-3.5 flex-shrink-0" />
          <span>
            The net and the tax on this credit note do not come to its total.
            Quote the credit note number rather than any of the figures.
          </span>
        </p>
      )}

      <div className="space-y-1 text-xs text-slate-400">
        <p>{AMOUNTS_ARE_POSITIVE}</p>
        <p>{TAX_FOLLOWS_THE_INVOICE}</p>
        <p>{TAX_TREATMENT_HINTS[creditNote.taxTreatment]}</p>
      </div>
    </div>
  );
}
