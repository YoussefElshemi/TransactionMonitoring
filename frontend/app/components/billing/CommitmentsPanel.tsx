"use client";

import { useCallback, useEffect, useState } from "react";
import { toast } from "sonner";
import { Plus, Trash2, Wallet } from "lucide-react";

import type { Commitment, CurrencyCode } from "@/types";
import { API_BASE, reportApiError, unwrap, unwrapPage } from "@/lib/api";
import {
  COMMITMENT_IN_USE_MESSAGE,
  COMMITMENT_IS_NOT_INVOICED_HERE,
  COMMITMENT_OVERLAP_MESSAGE,
  DRAWDOWN_IS_DERIVED,
  EXHAUSTED_HINT,
  INCLUDE_EXPIRED_HINT,
  TERM_STATE_LABELS,
  canDeleteCommitment,
  commitmentAmountRefusal,
  commitmentWindowRefusal,
  drawdownSentence,
  drawnFraction,
  isExhausted,
  overlapsExisting,
  termState,
} from "@/lib/contractTerms";
import { formatAgreed } from "@/lib/currency";
import { apiFetch } from "@/lib/http";
import { useIdempotencyKey } from "@/lib/useIdempotencyKey";
import { cn } from "@/lib/utils";
import { LocalTime } from "@/components/LocalTime";
import { PolicySwitch } from "@/components/PolicySwitch";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { DateTimePicker } from "@/components/DateTimePicker";
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

export function CommitmentsPanel({
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

  const [commitments, setCommitments] = useState<Commitment[]>([]);
  const [includeExpired, setIncludeExpired] = useState(false);
  const [loading, setLoading] = useState(true);

  const [createOpen, setCreateOpen] = useState(false);
  const [saving, setSaving] = useState(false);

  const [amount, setAmount] = useState("");
  const [effectiveFrom, setEffectiveFrom] = useState("");
  const [effectiveTo, setEffectiveTo] = useState("");

  const [deleteTarget, setDeleteTarget] = useState<Commitment | null>(null);
  const [deleting, setDeleting] = useState(false);

  const path = `${API_BASE}/organizations/${organizationId}/commitments`;

  const fetchCommitments = useCallback(async () => {
    setLoading(true);

    try {
      const query = new URLSearchParams({
        PageNumber: "1",
        PageSize: "50",
      });
      if (includeExpired) query.set("IncludeExpired", "true");

      const response = await apiFetch(`${path}?${query}`, {
        method: "GET",
        headers: { "Content-Type": "application/json" },
        credentials: "include",
      });

      if (response.ok) {
        const page = await unwrapPage<Commitment>(response);
        setCommitments(page.data);
      } else {
        await reportApiError(response, {
          fallback: t("common.errorOccurredFetchingCommitments"),
          onUnauthorized,
          toastId: "error-fetching-commitments",
        });
      }
    } catch {
      toast.error(t("common.errorOccurredFetchingCommitments"), {
        id: "error-fetching-commitments",
      });
    } finally {
      setLoading(false);
    }
  }, [path, includeExpired, onUnauthorized]);

  useEffect(() => {
    fetchCommitments();
  }, [fetchCommitments]);

  const resetForm = () => {
    setAmount("");
    setEffectiveFrom("");
    setEffectiveTo("");
  };

  const amountRefusal =
    amount.trim().length === 0 ? null : commitmentAmountRefusal(Number(amount));

  const windowRefusal =
    effectiveTo.trim().length === 0
      ? null
      : commitmentWindowRefusal(effectiveFrom, effectiveTo);

  const overlaps =
    effectiveTo.trim().length > 0 &&
    overlapsExisting(
      {
        effectiveFrom:
          effectiveFrom.trim().length > 0
            ? new Date(effectiveFrom).toISOString()
            : new Date().toISOString(),
        effectiveTo: new Date(effectiveTo).toISOString(),
      },
      commitments
    );

  const createBlocked =
    saving ||
    amount.trim().length === 0 ||
    amountRefusal !== null ||
    effectiveTo.trim().length === 0 ||
    windowRefusal !== null ||
    overlaps;

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
            amount: Number(amount),
            ...(effectiveFrom.length > 0
              ? { effectiveFrom: new Date(effectiveFrom).toISOString() }
              : {}),
            effectiveTo: new Date(effectiveTo).toISOString(),
          }),
        },
        { idempotencyKey: submission.current("create") }
      );

      if (response.ok) {
        await unwrap<Commitment>(response);
        submission.completed("create");

        toast.success(t("billing.commitmentRecorded"), {
          description: COMMITMENT_IS_NOT_INVOICED_HERE,
        });

        setCreateOpen(false);
        resetForm();
        await fetchCommitments();
      } else if (response.status === 409) {
        toast.error(t("billing.termOverlapsExistingCommitment"), {
          description: COMMITMENT_OVERLAP_MESSAGE,
        });
      } else {
        await reportApiError(response, {
          fallback: t("billing.failedRecordCommitment"),
          onUnauthorized,
          fieldLabels: {
            "Body.Amount": "Amount",
            "Body.EffectiveFrom": "Starts",
            "Body.EffectiveTo": "Ends",
          },
        });
      }
    } catch {
      toast.error(t("billing.failedRecordCommitment"));
    } finally {
      setSaving(false);
    }
  };

  const handleDelete = async () => {
    if (!deleteTarget) return;

    setDeleting(true);

    try {
      const response = await apiFetch(
        `${path}/${deleteTarget.commitmentId}`,
        {
          method: "DELETE",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
        },
        { idempotencyKey: submission.current(deleteTarget.commitmentId) }
      );

      if (response.ok) {
        submission.completed(deleteTarget.commitmentId);
        toast.success(t("common.commitmentRemoved"));
        setDeleteTarget(null);
        await fetchCommitments();
      } else if (response.status === 409) {
        toast.error(t("billing.commitmentHasBeenDrawnAgainst"), {
          description: COMMITMENT_IN_USE_MESSAGE,
        });
        setDeleteTarget(null);
        await fetchCommitments();
      } else {
        await reportApiError(response, {
          fallback: t("billing.failedRemoveCommitment"),
          onUnauthorized,
        });
      }
    } catch {
      toast.error(t("billing.failedRemoveCommitment"));
    } finally {
      setDeleting(false);
    }
  };

  return (
    <div className="space-y-3">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div className="flex items-start gap-2">
          <Wallet className="mt-0.5 size-3.5 flex-shrink-0 text-slate-400" />
          <div>
            <p className="font-mono text-xs uppercase tracking-widest text-slate-500">
              {t("billing.commitments")}
            </p>
            <p className="mt-1 max-w-2xl text-sm text-slate-500">
              {COMMITMENT_IS_NOT_INVOICED_HERE}
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
            {t("billing.commitment")}
          </Button>
        )}
      </div>

      <PolicySwitch
        label={t("billing.showExpiredTerms")}
        checked={includeExpired}
        onChange={setIncludeExpired}
      >
        {INCLUDE_EXPIRED_HINT}
      </PolicySwitch>

      {loading ? (
        <div className="space-y-2">
          <Skeleton className="h-24 w-full rounded-xl" />
        </div>
      ) : commitments.length === 0 ? (
        <div className="rounded-xl border border-slate-200 bg-card py-10 text-center">
          <p className="font-mono text-xs uppercase tracking-widest text-slate-400">
            {t("billing.noCommitments")}
          </p>
          <p className="mx-auto mt-2 max-w-md text-sm text-slate-500">
            This customer has promised nothing up front, so every month is
            invoiced in full.
          </p>
        </div>
      ) : (
        <div className="space-y-2">
          {commitments.map((commitment) => {
            const state = termState({
              effectiveFrom: commitment.effectiveFrom,
              effectiveTo: commitment.effectiveTo,
            });
            const fraction = drawnFraction(commitment);
            const exhausted = isExhausted(commitment);

            return (
              <div
                key={commitment.commitmentId}
                className={cn(
                  "rounded-xl border border-slate-200 bg-card p-4",
                  state === "expired" && "opacity-70"
                )}
              >
                <div className="flex flex-wrap items-start justify-between gap-3">
                  <div>
                    <p className="font-mono text-lg text-slate-900">
                      {formatAgreed(commitment.amount, currency)}
                    </p>
                    <p className="mt-0.5 text-xs text-slate-500">
                      <LocalTime value={commitment.effectiveFrom} dateOnly /> to{" "}
                      <LocalTime value={commitment.effectiveTo} dateOnly />
                    </p>
                  </div>

                  <div className="flex items-center gap-2">
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

                    {canWrite &&
                      (canDeleteCommitment(commitment) ? (
                        <Button
                          variant="ghost"
                          size="icon"
                          className="h-8 w-8 text-slate-400 hover:text-critical-700"
                          onClick={() => setDeleteTarget(commitment)}
                        >
                          <Trash2 className="size-4" />
                        </Button>
                      ) : (
                        <span
                          className="font-mono text-[11px] uppercase tracking-widest text-slate-300"
                          title={COMMITMENT_IN_USE_MESSAGE}
                        >
                          drawn on
                        </span>
                      ))}
                  </div>
                </div>

                <div className="mt-3">
                  <div className="h-2 w-full overflow-hidden rounded-full bg-slate-100">
                    <div
                      className={cn(
                        "h-full rounded-full transition-all",
                        exhausted ? "bg-attention-500" : "bg-elevated-500"
                      )}
                      style={{ width: `${Math.round(fraction * 100)}%` }}
                    />
                  </div>
                  <p className="mt-1.5 font-mono text-xs text-slate-600">
                    {drawdownSentence(commitment, currency)}
                  </p>
                  {exhausted && (
                    <p className="mt-1 text-xs text-attention-700">
                      {EXHAUSTED_HINT}
                    </p>
                  )}
                </div>
              </div>
            );
          })}

          <p className="text-xs text-slate-400">{DRAWDOWN_IS_DERIVED}</p>
        </div>
      )}

      <Dialog open={createOpen} onOpenChange={setCreateOpen}>
        <DialogContent className="sm:max-w-lg">
          <DialogHeader>
            <DialogTitle>{t("billing.recordACommitment")}</DialogTitle>
            <DialogDescription>
              {COMMITMENT_IS_NOT_INVOICED_HERE}
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-4">
            <div className="space-y-1.5">
              <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
                {t("billing.amountPromised")}
              </label>
              <Input
                value={amount}
                onChange={(event) => setAmount(event.target.value)}
                inputMode="decimal"
                className="w-40 font-mono"
                placeholder="0.00"
              />
              {amountRefusal !== null && (
                <p className="text-xs text-critical-500">{amountRefusal}</p>
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
                  aria-label={t("billing.commitmentStarts")}
                />
                <p className="text-xs text-slate-500">
                  {t("billing.leftBlankItStartsNow")}
                </p>
              </div>

              <div className="space-y-1.5">
                <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
                  {t("billing.ends")}
                </label>
                <DateTimePicker
                  value={effectiveTo}
                  onChange={setEffectiveTo}
                  placeholder={t("billing.selectEndDateAndTime")}
                  aria-label={t("billing.commitmentEnds")}
                />
                <p className="text-xs text-slate-500">
                  {t("billing.requiredTheTermIsWhat")}
                </p>
              </div>
            </div>

            {windowRefusal !== null && (
              <p className="text-xs text-critical-500">{windowRefusal}</p>
            )}

            {overlaps && (
              <p className="rounded-lg border border-critical-200 bg-critical-50 p-3 text-xs text-critical-700">
                {COMMITMENT_OVERLAP_MESSAGE}
              </p>
            )}

            <p className="rounded-lg border border-slate-200 bg-slate-50 p-3 text-xs text-slate-500">
              {DRAWDOWN_IS_DERIVED}
            </p>
          </div>

          <DialogFooter>
            <Button variant="outline" onClick={() => setCreateOpen(false)}>
              {t("common.cancel")}
            </Button>
            <Button onClick={handleCreate} disabled={createBlocked}>
              {saving ? "Recording..." : "Record it"}
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
            <DialogTitle>{t("billing.removeThisCommitment")}</DialogTitle>
            <DialogDescription>
              Nothing has drawn against it, so it can be removed outright — no
              invoice depends on it. Once a month inside the term is invoiced,
              this is refused instead.
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
              {deleting ? "Removing..." : "Remove"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
