"use client";

import { useCallback, useEffect, useState } from "react";
import { toast } from "sonner";
import { Plus, Receipt, Trash2 } from "lucide-react";

import type { CurrencyCode, InvoiceAdjustment } from "@/types";
import { API_BASE, reportApiError, unwrap, unwrapPage } from "@/lib/api";
import {
  ADJUSTMENT_DESCRIPTION_HINT,
  ADJUSTMENT_DIRECTION_HINTS,
  ADJUSTMENT_DIRECTION_LABELS,
  type AdjustmentDirection,
  APPLIED_HINT,
  DESCRIPTION_MAX_LENGTH,
  INCLUDE_APPLIED_HINT,
  MONTH_ALREADY_INVOICED,
  MONTH_NAMES,
  WAITING_HINT,
  adjustmentAmountRefusal,
  canDeleteAdjustment,
  directionOf,
  isApplied,
  isValidAdjustmentDescription,
  isValidMonth,
  isValidYear,
  periodLabel,
  signedAmount,
} from "@/lib/contractTerms";
import { formatAgreed } from "@/lib/currency";
import { apiFetch } from "@/lib/http";
import { useIdempotencyKey } from "@/lib/useIdempotencyKey";
import { cn } from "@/lib/utils";
import Pagination from "@/components/Pagination";
import { PolicySwitch } from "@/components/PolicySwitch";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import { Textarea } from "@/components/ui/textarea";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
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

const DIRECTIONS: AdjustmentDirection[] = ["credit", "charge"];

export function InvoiceAdjustmentsPanel({
  organizationId,
  currency,
  canWrite,
  onUnauthorized,
}: {
  organizationId: string;
  currency: CurrencyCode | null;
  canWrite: boolean;
  onUnauthorized: () => void;
}) {
  const t = useTranslation();
  const submission = useIdempotencyKey();
  const now = new Date();

  const [adjustments, setAdjustments] = useState<InvoiceAdjustment[]>([]);
  const [includeApplied, setIncludeApplied] = useState(false);
  const [pageNumber, setPageNumber] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [loading, setLoading] = useState(true);

  const [createOpen, setCreateOpen] = useState(false);
  const [saving, setSaving] = useState(false);

  const [direction, setDirection] = useState<AdjustmentDirection>("credit");
  const [amount, setAmount] = useState("");
  const [description, setDescription] = useState("");
  const [month, setMonth] = useState(String(now.getMonth() + 1));
  const [year, setYear] = useState(String(now.getFullYear()));

  const [deleteTarget, setDeleteTarget] = useState<InvoiceAdjustment | null>(
    null
  );
  const [deleting, setDeleting] = useState(false);

  const path = `${API_BASE}/organizations/${organizationId}/invoice-adjustments`;

  const fetchAdjustments = useCallback(async () => {
    setLoading(true);

    try {
      const query = new URLSearchParams({
        PageNumber: String(pageNumber),
        PageSize: "25",
      });
      if (includeApplied) query.set("IncludeApplied", "true");

      const response = await apiFetch(`${path}?${query}`, {
        method: "GET",
        headers: { "Content-Type": "application/json" },
        credentials: "include",
      });

      if (response.ok) {
        const page = await unwrapPage<InvoiceAdjustment>(response);
        setAdjustments(page.data);
        setTotalPages(page.metadata.pageCount);
      } else {
        await reportApiError(response, {
          fallback: t("billing.errorOccurredFetchingAdjustments"),
          onUnauthorized,
          toastId: "error-fetching-adjustments",
        });
      }
    } catch {
      toast.error(t("billing.errorOccurredFetchingAdjustments"), {
        id: "error-fetching-adjustments",
      });
    } finally {
      setLoading(false);
    }
  }, [path, pageNumber, includeApplied, onUnauthorized]);

  useEffect(() => {
    fetchAdjustments();
  }, [fetchAdjustments]);

  const resetForm = () => {
    setDirection("credit");
    setAmount("");
    setDescription("");
    setMonth(String(now.getMonth() + 1));
    setYear(String(now.getFullYear()));
  };

  const amountRefusal =
    amount.trim().length === 0
      ? null
      : adjustmentAmountRefusal(Number(amount));

  const periodValid =
    isValidMonth(Number(month)) && isValidYear(Number(year), now);

  const descriptionValid = isValidAdjustmentDescription(description);

  const createBlocked =
    saving ||
    amount.trim().length === 0 ||
    amountRefusal !== null ||
    !periodValid ||
    !descriptionValid;

  const handleCreate = async () => {
    setSaving(true);

    try {
      const response = await apiFetch(
        path,
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
          body: JSON.stringify({
            month: Number(month),
            year: Number(year),
            description: description.trim(),
            amount: signedAmount(direction, Number(amount)),
          }),
        },
        { idempotencyKey: submission.current("create") }
      );

      if (response.ok) {
        await unwrap<InvoiceAdjustment>(response);
        submission.completed("create");

        toast.success(
          t(
            "billing.adjustmentRaisedFor",
            ADJUSTMENT_DIRECTION_LABELS[direction],
            periodLabel({ month: Number(month), year: Number(year) })
          ),
          { description: WAITING_HINT }
        );

        setCreateOpen(false);
        resetForm();
        await fetchAdjustments();
      } else if (response.status === 409) {
        toast.error(t("billing.monthHasAlreadyBeenInvoiced"), {
          description: MONTH_ALREADY_INVOICED,
        });
      } else {
        await reportApiError(response, {
          fallback: t("billing.failedRaiseAdjustment"),
          onUnauthorized,
          fieldLabels: {
            "Body.Month": "Month",
            "Body.Year": "Year",
            "Body.Description": "What it is for",
            "Body.Amount": "Amount",
          },
        });
      }
    } catch {
      toast.error(t("billing.failedRaiseAdjustment"));
    } finally {
      setSaving(false);
    }
  };

  const handleDelete = async () => {
    if (!deleteTarget) return;

    setDeleting(true);

    try {
      const response = await apiFetch(
        `${path}/${deleteTarget.invoiceAdjustmentId}`,
        {
          method: "DELETE",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
        },
        { idempotencyKey: submission.current(deleteTarget.invoiceAdjustmentId) }
      );

      if (response.ok) {
        submission.completed(deleteTarget.invoiceAdjustmentId);
        toast.success(t("billing.adjustmentWithdrawn"));
        setDeleteTarget(null);
        await fetchAdjustments();
      } else if (response.status === 409) {
        toast.error(t("billing.adjustmentAlreadyInvoice"), {
          description: APPLIED_HINT,
        });
        setDeleteTarget(null);
        await fetchAdjustments();
      } else {
        await reportApiError(response, {
          fallback: t("billing.failedWithdrawAdjustment"),
          onUnauthorized,
        });
      }
    } catch {
      toast.error(t("billing.failedWithdrawAdjustment"));
    } finally {
      setDeleting(false);
    }
  };

  return (
    <div className="space-y-3">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div className="flex items-start gap-2">
          <Receipt className="mt-0.5 size-3.5 flex-shrink-0 text-slate-400" />
          <div>
            <p className="font-mono text-xs uppercase tracking-widest text-slate-500">
              {t("billing.adjustments")}
            </p>
            <p className="mt-1 max-w-2xl text-sm text-slate-500">
              One-offs agreed outside the plan, against one month. They land
              last on the invoice — after any commitment is drawn on — so a
              goodwill credit reduces what the customer pays rather than
              spending their prepaid balance.
            </p>
          </div>
        </div>

        {canWrite && (
          <Button
            size="sm"
            variant="outline"
            className="gap-1.5"
            onClick={() => {
              resetForm();
              setCreateOpen(true);
            }}
          >
            <Plus className="size-4" />
            {t("billing.adjustment")}
          </Button>
        )}
      </div>

      <PolicySwitch
        label={t("billing.showAppliedAdjustments")}
        checked={includeApplied}
        onChange={(next) => {
          setIncludeApplied(next);
          setPageNumber(1);
        }}
      >
        {INCLUDE_APPLIED_HINT}
      </PolicySwitch>

      <div className="overflow-hidden rounded-xl border border-slate-200 bg-card">
        <Table>
          <TableHeader>
            <TableRow className="bg-slate-50 hover:bg-slate-50">
              <TableHead className="w-36">{t("common.amount")}</TableHead>
              <TableHead>{t("billing.whatItIsFor")}</TableHead>
              <TableHead className="w-36">{t("billing.month")}</TableHead>
              <TableHead className="w-44">{t("common.state")}</TableHead>
              {canWrite && <TableHead className="w-20">{t("common.actions")}</TableHead>}
            </TableRow>
          </TableHeader>
          <TableBody>
            {loading ? (
              Array.from({ length: 2 }).map((_, row) => (
                <TableRow key={row}>
                  {Array.from({ length: canWrite ? 5 : 4 }).map((_, cell) => (
                    <TableCell key={cell}>
                      <Skeleton className="h-4 w-full" />
                    </TableCell>
                  ))}
                </TableRow>
              ))
            ) : adjustments.length === 0 ? (
              <TableRow>
                <TableCell
                  colSpan={canWrite ? 5 : 4}
                  className="py-10 text-center"
                >
                  <p className="font-mono text-xs uppercase tracking-widest text-slate-400">
                    {includeApplied ? "No adjustments" : "None waiting"}
                  </p>
                  <p className="mx-auto mt-2 max-w-md text-sm text-slate-500">
                    {includeApplied
                      ? "Nothing has been agreed with this customer outside their plan."
                      : "Nothing is waiting to be applied. Adjustments already on an invoice are hidden — turn the switch above on to see them."}
                  </p>
                </TableCell>
              </TableRow>
            ) : (
              adjustments.map((adjustment) => {
                const applied = isApplied(adjustment);
                const credit = directionOf(adjustment) === "credit";

                return (
                  <TableRow
                    key={adjustment.invoiceAdjustmentId}
                    className="hover:bg-slate-50/50"
                  >
                    <TableCell
                      className={cn(
                        "font-mono text-sm",
                        credit ? "text-elevated-700" : "text-slate-900"
                      )}
                    >
                      {credit ? "−" : "+"}
                      {formatAgreed(Math.abs(adjustment.amount), currency)}
                      <span className="block text-xs text-slate-400">
                        {credit ? "Credit" : "Charge"}
                      </span>
                    </TableCell>

                    <TableCell className="max-w-md text-sm text-slate-600">
                      {adjustment.description}
                    </TableCell>

                    <TableCell className="text-sm text-slate-600">
                      {periodLabel(adjustment)}
                    </TableCell>

                    <TableCell className="text-xs">
                      {applied ? (
                        <span
                          className="text-slate-500"
                          title={APPLIED_HINT}
                        >
                          {t("billing.onAnInvoice")}
                        </span>
                      ) : (
                        <span className="text-slate-400">
                          {t("billing.waitingForTheMonthTo")}
                        </span>
                      )}
                    </TableCell>

                    {canWrite && (
                      <TableCell>
                        {canDeleteAdjustment(adjustment) ? (
                          <Button
                            variant="ghost"
                            size="icon"
                            className="h-8 w-8 text-slate-400 hover:text-critical-700"
                            onClick={() => setDeleteTarget(adjustment)}
                          >
                            <Trash2 className="size-4" />
                          </Button>
                        ) : (
                          <span
                            className="font-mono text-[11px] uppercase tracking-widest text-slate-300"
                            title={APPLIED_HINT}
                          >
                            issued
                          </span>
                        )}
                      </TableCell>
                    )}
                  </TableRow>
                );
              })
            )}
          </TableBody>
        </Table>
      </div>

      {adjustments.length > 0 && totalPages > 1 && (
        <Pagination
          handlePrevPage={() => setPageNumber((page) => Math.max(1, page - 1))}
          pageNumber={pageNumber}
          totalPages={totalPages}
          handleNextPage={() =>
            setPageNumber((page) => Math.min(totalPages, page + 1))
          }
        />
      )}

      <Dialog open={createOpen} onOpenChange={setCreateOpen}>
        <DialogContent className="sm:max-w-lg">
          <DialogHeader>
            <DialogTitle>{t("billing.raiseAnAdjustment")}</DialogTitle>
            <DialogDescription>
              Applied when that month is invoiced, as its own line on the
              customer&apos;s document.
            </DialogDescription>
          </DialogHeader>

          <div className="max-h-[60vh] space-y-4 overflow-y-auto pr-1">
            <div className="space-y-1.5">
              <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
                {t("billing.whichWay")}
              </label>
              <div className="flex flex-wrap gap-1.5">
                {DIRECTIONS.map((option) => (
                  <button
                    key={option}
                    type="button"
                    onClick={() => setDirection(option)}
                    className={cn(
                      "rounded-full border px-3 py-1 font-mono text-xs transition-colors",
                      direction === option
                        ? "border-brand-800 bg-brand-50 text-brand-900"
                        : "border-slate-200 bg-card text-slate-600 hover:bg-slate-50"
                    )}
                  >
                    {ADJUSTMENT_DIRECTION_LABELS[option]}
                  </button>
                ))}
              </div>
              <p className="text-xs text-slate-500">
                {ADJUSTMENT_DIRECTION_HINTS[direction]}
              </p>
            </div>

            <div className="space-y-1.5">
              <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
                {t("common.amount")}
              </label>
              <Input
                value={amount}
                onChange={(event) => setAmount(event.target.value)}
                inputMode="decimal"
                className="w-40 font-mono"
                placeholder="0.00"
              />
              {amountRefusal !== null ? (
                <p className="text-xs text-critical-500">{amountRefusal}</p>
              ) : (
                amount.trim().length > 0 && (
                  <p className="text-xs text-slate-500">
                    The customer&apos;s invoice will move by{" "}
                    <span className="font-mono">
                      {formatAgreed(signedAmount(direction, Number(amount)), currency)}
                    </span>
                    .
                  </p>
                )
              )}
            </div>

            <div className="grid gap-3 sm:grid-cols-2">
              <div className="space-y-1.5">
                <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
                  {t("billing.month")}
                </label>
                <Select value={month} onValueChange={setMonth}>
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {MONTH_NAMES.map((name, index) => (
                      <SelectItem key={name} value={String(index + 1)}>
                        {name}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>

              <div className="space-y-1.5">
                <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
                  {t("billing.year")}
                </label>
                <Input
                  value={year}
                  onChange={(event) => setYear(event.target.value)}
                  inputMode="numeric"
                  className="font-mono"
                />
                {!isValidYear(Number(year), now) && (
                  <p className="text-xs text-critical-500">
                    Between 2000 and {now.getFullYear() + 1}.
                  </p>
                )}
              </div>
            </div>

            <p className="rounded-lg border border-attention-200 bg-attention-50 p-3 text-xs text-attention-700">
              A month that has already been invoiced will be refused.{" "}
              {MONTH_ALREADY_INVOICED}
            </p>

            <div className="space-y-1.5">
              <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
                {t("billing.whatItIsFor")}
              </label>
              <Textarea
                value={description}
                onChange={(event) => setDescription(event.target.value)}
                maxLength={DESCRIPTION_MAX_LENGTH}
                className="min-h-[64px]"
                placeholder={t("billing.goodwillCreditForTheOutage")}
              />
              <p className="text-xs text-slate-500">
                {ADJUSTMENT_DESCRIPTION_HINT}
              </p>
            </div>
          </div>

          <DialogFooter>
            <Button variant="outline" onClick={() => setCreateOpen(false)}>
              {t("common.cancel")}
            </Button>
            <Button onClick={handleCreate} disabled={createBlocked}>
              {saving
                ? "Raising..."
                : `Raise the ${ADJUSTMENT_DIRECTION_LABELS[
                    direction
                  ].toLowerCase()}`}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <Dialog
        open={deleteTarget !== null}
        onOpenChange={(open) => !open && setDeleteTarget(null)}
      >
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>{t("billing.withdrawThisAdjustment")}</DialogTitle>
            <DialogDescription>
              It has not been applied to an invoice yet, so it can be removed
              outright. Nothing the customer holds changes.
            </DialogDescription>
          </DialogHeader>

          <DialogFooter>
            <Button variant="outline" onClick={() => setDeleteTarget(null)}>
              {t("common.keepIt")}
            </Button>
            <Button
              variant="destructive"
              onClick={handleDelete}
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
