"use client";

import { useCallback, useEffect, useState } from "react";
import { toast } from "sonner";
import { Percent, Plus, Trash2 } from "lucide-react";

import type { ContractDiscount, CurrencyCode, DiscountType } from "@/types";
import { API_BASE, reportApiError, unwrap, unwrapPage } from "@/lib/api";
import {
  DISCOUNTS_COMPOUND,
  DISCOUNT_DESCRIPTION_HINT,
  DISCOUNT_ENDS_HINT,
  DISCOUNT_STARTS_HINT,
  DISCOUNT_TYPE_HINTS,
  DISCOUNT_TYPE_LABELS,
  DESCRIPTION_MAX_LENGTH,
  INCLUDE_EXPIRED_HINT,
  TERM_STATE_LABELS,
  deleteDiscountEffect,
  discountAmount,
  discountValueRefusal,
  discountWindowRefusal,
  isValidDiscountDescription,
  termState,
} from "@/lib/contractTerms";
import { apiFetch } from "@/lib/http";
import { useIdempotencyKey } from "@/lib/useIdempotencyKey";
import { cn } from "@/lib/utils";
import Pagination from "@/components/Pagination";
import { LocalTime } from "@/components/LocalTime";
import { PolicySwitch } from "@/components/PolicySwitch";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { DateTimePicker } from "@/components/DateTimePicker";
import { Skeleton } from "@/components/ui/skeleton";
import { Textarea } from "@/components/ui/textarea";
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

const DISCOUNT_TYPES: DiscountType[] = ["Percentage", "FixedAmount"];

export function ContractDiscountsPanel({
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

  const [discounts, setDiscounts] = useState<ContractDiscount[]>([]);
  const [includeExpired, setIncludeExpired] = useState(false);
  const [pageNumber, setPageNumber] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [loading, setLoading] = useState(true);

  const [createOpen, setCreateOpen] = useState(false);
  const [saving, setSaving] = useState(false);

  const [discountType, setDiscountType] = useState<DiscountType>("Percentage");
  const [value, setValue] = useState("");
  const [description, setDescription] = useState("");
  const [effectiveFrom, setEffectiveFrom] = useState("");
  const [effectiveTo, setEffectiveTo] = useState("");

  const [deleteTarget, setDeleteTarget] = useState<ContractDiscount | null>(
    null
  );
  const [deleting, setDeleting] = useState(false);

  const path = `${API_BASE}/organizations/${organizationId}/contract-discounts`;

  const fetchDiscounts = useCallback(async () => {
    setLoading(true);

    try {
      const query = new URLSearchParams({
        PageNumber: String(pageNumber),
        PageSize: "25",
      });
      if (includeExpired) query.set("IncludeExpired", "true");

      const response = await apiFetch(`${path}?${query}`, {
        method: "GET",
        headers: { "Content-Type": "application/json" },
        credentials: "include",
      });

      if (response.ok) {
        const page = await unwrapPage<ContractDiscount>(response);
        setDiscounts(page.data);
        setTotalPages(page.metadata.pageCount);
      } else {
        await reportApiError(response, {
          fallback: t("billing.errorOccurredFetchingDiscounts"),
          onUnauthorized,
          toastId: "error-fetching-discounts",
        });
      }
    } catch {
      toast.error(t("billing.errorOccurredFetchingDiscounts"), {
        id: "error-fetching-discounts",
      });
    } finally {
      setLoading(false);
    }
  }, [path, pageNumber, includeExpired, onUnauthorized]);

  useEffect(() => {
    fetchDiscounts();
  }, [fetchDiscounts]);

  const resetForm = () => {
    setDiscountType("Percentage");
    setValue("");
    setDescription("");
    setEffectiveFrom("");
    setEffectiveTo("");
  };

  const valueRefusal =
    value.trim().length === 0
      ? null
      : discountValueRefusal(discountType, Number(value));

  const windowRefusal = discountWindowRefusal(effectiveFrom, effectiveTo);
  const descriptionValid = isValidDiscountDescription(description);

  const createBlocked =
    saving ||
    value.trim().length === 0 ||
    valueRefusal !== null ||
    windowRefusal !== null ||
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
            discountType,
            value: Number(value),
            description: description.trim(),
            ...(effectiveFrom.length > 0
              ? { effectiveFrom: new Date(effectiveFrom).toISOString() }
              : {}),
            ...(effectiveTo.length > 0
              ? { effectiveTo: new Date(effectiveTo).toISOString() }
              : {}),
          }),
        },
        { idempotencyKey: submission.current("create") }
      );

      if (response.ok) {
        await unwrap<ContractDiscount>(response);
        submission.completed("create");

        toast.success(t("billing.discountAgreed"), { description: DISCOUNTS_COMPOUND });

        setCreateOpen(false);
        resetForm();
        await fetchDiscounts();
      } else {
        await reportApiError(response, {
          fallback: t("billing.failedRecordDiscount"),
          onUnauthorized,
          fieldLabels: {
            "Body.DiscountType": "Kind",
            "Body.Value": "Amount",
            "Body.Description": "Reason",
            "Body.EffectiveFrom": "Starts",
            "Body.EffectiveTo": "Ends",
          },
        });
      }
    } catch {
      toast.error(t("billing.failedRecordDiscount"));
    } finally {
      setSaving(false);
    }
  };

  const handleDelete = async () => {
    if (!deleteTarget) return;

    const effect = deleteDiscountEffect(deleteTarget);
    setDeleting(true);

    try {
      const response = await apiFetch(
        `${path}/${deleteTarget.contractDiscountId}`,
        {
          method: "DELETE",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
        },
        { idempotencyKey: submission.current(deleteTarget.contractDiscountId) }
      );

      if (response.ok) {
        submission.completed(deleteTarget.contractDiscountId);

        toast.success(effect.removes ? t("billing.discountRemoved") : t("billing.discountEnded"), {
          description: effect.removes
            ? undefined
            : t("billing.staysListEndDateBecause"),
        });

        setDeleteTarget(null);
        await fetchDiscounts();
      } else {
        await reportApiError(response, {
          fallback: t("billing.failedEndDiscount"),
          onUnauthorized,
        });
      }
    } catch {
      toast.error(t("billing.failedEndDiscount"));
    } finally {
      setDeleting(false);
    }
  };

  const live = discounts.filter(
    (discount) => termState(discount) === "inForce"
  ).length;

  return (
    <div className="space-y-3">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div className="flex items-start gap-2">
          <Percent className="mt-0.5 size-3.5 flex-shrink-0 text-slate-400" />
          <div>
            <p className="font-mono text-xs uppercase tracking-widest text-slate-500">
              {t("billing.discounts")}
            </p>
            <p className="mt-1 max-w-2xl text-sm text-slate-500">
              A standing reduction agreed with this customer, dated. Kept off
              the plan because two customers on the same published price can
              have been given different discounts.{" "}
              {live > 1 && DISCOUNTS_COMPOUND}
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
            {t("billing.discount")}
          </Button>
        )}
      </div>

      <PolicySwitch
        label={t("billing.showExpiredDiscounts")}
        checked={includeExpired}
        onChange={(next) => {
          setIncludeExpired(next);
          setPageNumber(1);
        }}
      >
        {INCLUDE_EXPIRED_HINT}
      </PolicySwitch>

      <div className="overflow-hidden rounded-xl border border-slate-200 bg-card">
        <Table>
          <TableHeader>
            <TableRow className="bg-slate-50 hover:bg-slate-50">
              <TableHead className="w-32">{t("common.amount")}</TableHead>
              <TableHead>{t("common.reason")}</TableHead>
              <TableHead className="w-32">{t("common.state")}</TableHead>
              <TableHead className="w-56">{t("billing.runs")}</TableHead>
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
            ) : discounts.length === 0 ? (
              <TableRow>
                <TableCell
                  colSpan={canWrite ? 5 : 4}
                  className="py-10 text-center"
                >
                  <p className="font-mono text-xs uppercase tracking-widest text-slate-400">
                    {t("billing.noDiscounts")}
                  </p>
                  <p className="mx-auto mt-2 max-w-md text-sm text-slate-500">
                    This customer pays the published price of whatever plan they
                    are on.
                  </p>
                </TableCell>
              </TableRow>
            ) : (
              discounts.map((discount) => {
                const state = termState(discount);

                return (
                  <TableRow
                    key={discount.contractDiscountId}
                    className={cn(
                      "hover:bg-slate-50/50",
                      state === "expired" && "text-slate-400"
                    )}
                  >
                    <TableCell className="font-mono text-sm text-slate-900">
                      {discountAmount(discount, currency)}
                      <span className="block text-xs text-slate-400">
                        {DISCOUNT_TYPE_LABELS[discount.discountType]}
                      </span>
                    </TableCell>

                    <TableCell className="max-w-md text-sm text-slate-600">
                      {discount.description}
                    </TableCell>

                    <TableCell>
                      <span
                        className={cn(
                          "rounded-full border px-2 py-0.5 font-mono text-xs",
                          state === "inForce"
                            ? "border-elevated-200 bg-elevated-50 text-elevated-700"
                            : "border-slate-200 bg-slate-50 text-slate-600"
                        )}
                      >
                        {TERM_STATE_LABELS[state]}
                      </span>
                    </TableCell>

                    <TableCell className="text-xs text-slate-500">
                      <LocalTime value={discount.effectiveFrom} dateOnly />
                      {discount.effectiveTo === null ? (
                        <span className="block text-slate-400">
                          until it is ended
                        </span>
                      ) : (
                        <span className="block text-slate-400">
                          until{" "}
                          <LocalTime value={discount.effectiveTo} dateOnly />
                        </span>
                      )}
                    </TableCell>

                    {canWrite && (
                      <TableCell>
                        <Button
                          variant="ghost"
                          size="icon"
                          className="h-8 w-8 text-slate-400 hover:text-critical-700"
                          onClick={() => setDeleteTarget(discount)}
                        >
                          <Trash2 className="size-4" />
                        </Button>
                      </TableCell>
                    )}
                  </TableRow>
                );
              })
            )}
          </TableBody>
        </Table>
      </div>

      {discounts.length > 0 && totalPages > 1 && (
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
            <DialogTitle>{t("billing.agreeADiscount")}</DialogTitle>
            <DialogDescription>
              It comes off every invoice raised while it is in force, after the
              plan&apos;s own arithmetic and before any commitment is drawn on.
            </DialogDescription>
          </DialogHeader>

          <div className="max-h-[60vh] space-y-4 overflow-y-auto pr-1">
            <div className="space-y-1.5">
              <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
                {t("common.kind")}
              </label>
              <div className="flex flex-wrap gap-1.5">
                {DISCOUNT_TYPES.map((type) => (
                  <button
                    key={type}
                    type="button"
                    onClick={() => setDiscountType(type)}
                    className={cn(
                      "rounded-full border px-3 py-1 font-mono text-xs transition-colors",
                      discountType === type
                        ? "border-brand-800 bg-brand-50 text-brand-900"
                        : "border-slate-200 bg-card text-slate-600 hover:bg-slate-50"
                    )}
                  >
                    {DISCOUNT_TYPE_LABELS[type]}
                  </button>
                ))}
              </div>
              <p className="text-xs text-slate-500">
                {DISCOUNT_TYPE_HINTS[discountType]}
              </p>
            </div>

            <div className="space-y-1.5">
              <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
                {discountType === "Percentage" ? "Per cent" : "Amount"}
              </label>
              <Input
                value={value}
                onChange={(event) => setValue(event.target.value)}
                inputMode="decimal"
                className="w-40 font-mono"
              />
              {valueRefusal !== null && (
                <p className="text-xs text-critical-500">{valueRefusal}</p>
              )}
            </div>

            <div className="space-y-1.5">
              <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
                {t("common.reason")}
              </label>
              <Textarea
                value={description}
                onChange={(event) => setDescription(event.target.value)}
                maxLength={DESCRIPTION_MAX_LENGTH}
                className="min-h-[64px]"
                placeholder={t("billing.agreedWithTheCustomerOn")}
              />
              <p className="text-xs text-slate-500">
                {DISCOUNT_DESCRIPTION_HINT}
              </p>
              {description.length > 0 && !descriptionValid && (
                <p className="text-xs text-critical-500">
                  At most {DESCRIPTION_MAX_LENGTH} characters.
                </p>
              )}
            </div>

            <div className="grid gap-3 sm:grid-cols-2">
              <div className="space-y-1.5">
                <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
                  {t("common.starts")}
                </label>
                <DateTimePicker
                  value={effectiveFrom}
                  onChange={setEffectiveFrom}
                  placeholder={t("billing.startsNow")}
                  aria-label={t("billing.discountStarts")}
                />
                <p className="text-xs text-slate-500">
                  {DISCOUNT_STARTS_HINT}
                </p>
              </div>

              <div className="space-y-1.5">
                <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
                  {t("billing.ends")}
                </label>
                <DateTimePicker
                  value={effectiveTo}
                  onChange={setEffectiveTo}
                  placeholder={t("billing.noEndDate")}
                  aria-label={t("billing.discountEnds")}
                />
                <p className="text-xs text-slate-500">{DISCOUNT_ENDS_HINT}</p>
              </div>
            </div>

            {windowRefusal !== null && (
              <p className="text-xs text-critical-500">{windowRefusal}</p>
            )}
          </div>

          <DialogFooter>
            <Button variant="outline" onClick={() => setCreateOpen(false)}>
              {t("common.cancel")}
            </Button>
            <Button onClick={handleCreate} disabled={createBlocked}>
              {saving ? "Saving..." : "Agree it"}
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
            <DialogTitle>
              {deleteTarget && deleteDiscountEffect(deleteTarget).removes
                ? "Remove this discount?"
                : "End this discount?"}
            </DialogTitle>
            <DialogDescription>
              {deleteTarget && deleteDiscountEffect(deleteTarget).message}
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
              {deleting
                ? "Working..."
                : deleteTarget && deleteDiscountEffect(deleteTarget).removes
                  ? "Remove"
                  : "End it"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
