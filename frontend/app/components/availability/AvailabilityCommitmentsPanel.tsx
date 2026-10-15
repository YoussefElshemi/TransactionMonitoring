"use client";

import { useCallback, useEffect, useState } from "react";
import { toast } from "sonner";
import { Plus, Trash2 } from "lucide-react";

import type { AvailabilityCommitment } from "@/types";
import { API_BASE, reportApiError, unwrap, unwrapPage } from "@/lib/api";
import { apiFetch } from "@/lib/http";
import { canOperateStatus } from "@/lib/permissions";
import { usePermissions } from "@/context/PermissionsContext";
import { useIdempotencyKey } from "@/lib/useIdempotencyKey";
import {
  CREATING_CLOSES_THE_OPEN_TERM,
  EMPTY_SCHEDULE_IS_VALID,
  LATENCY_TARGET_MAX_MS,
  PERCENTILE_HINT,
  TARGET_HINT,
  bandSentence,
  bandsWorstFirst,
  commitmentBody,
  commitmentRefusal,
  emptyBand,
  formatUptime,
  termState,
  type CreditBandDraft,
} from "@/lib/availability";
import { ToneBadge } from "@/components/status/StatusChips";
import { LocalTime } from "@/components/LocalTime";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
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

export function AvailabilityCommitmentsPanel({
  organizationId,
  onUnauthorized,
}: {
  organizationId: string;
  onUnauthorized: () => void;
}) {
  const t = useTranslation();
  const { grants, holds } = usePermissions();

  const canRead = holds("availability-commitments:read");
  const canWrite = canOperateStatus(grants, "availability-commitments:write");

  const [commitments, setCommitments] = useState<AvailabilityCommitment[]>([]);
  const [loading, setLoading] = useState(true);
  const [includeSuperseded, setIncludeSuperseded] = useState(false);

  const [formOpen, setFormOpen] = useState(false);
  const [deleting, setDeleting] = useState<AvailabilityCommitment | null>(null);
  const [saving, setSaving] = useState(false);

  const [availabilityTarget, setAvailabilityTarget] = useState("99.9");
  const [latencyTarget, setLatencyTarget] = useState("500");
  const [percentile, setPercentile] = useState("99");
  const [effectiveFrom, setEffectiveFrom] = useState("");
  const [effectiveTo, setEffectiveTo] = useState("");
  const [bands, setBands] = useState<CreditBandDraft[]>([emptyBand()]);

  const submission = useIdempotencyKey();

  const fetchCommitments = useCallback(async () => {
    setLoading(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationId}/availability/commitments?PageNumber=1&PageSize=50&IncludeSuperseded=${includeSuperseded}`,
        {
          method: "GET",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
        }
      );

      if (response.ok) {
        const page = await unwrapPage<AvailabilityCommitment>(response);
        setCommitments(page.data);
      } else if (response.status !== 403) {
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
  }, [organizationId, includeSuperseded, onUnauthorized]);

  useEffect(() => {
    if (canRead) fetchCommitments();
  }, [canRead, fetchCommitments]);

  const refusal = commitmentRefusal({
    availabilityTarget,
    latencyTargetMilliseconds: latencyTarget,
    latencyPercentile: percentile,
    effectiveFrom,
    effectiveTo,
    creditBands: bands.filter(
      (band) =>
        band.belowUptimePercentage.trim().length > 0 ||
        band.creditPercentage.trim().length > 0
    ),
  });

  const save = async () => {
    setSaving(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationId}/availability/commitments`,
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
          body: JSON.stringify(
            commitmentBody({
              availabilityTarget,
              latencyTargetMilliseconds: latencyTarget,
              latencyPercentile: percentile,
              effectiveFrom,
              effectiveTo,
              creditBands: bands.filter(
                (band) =>
                  band.belowUptimePercentage.trim().length > 0 ||
                  band.creditPercentage.trim().length > 0
              ),
            })
          ),
        },
        { idempotencyKey: submission.current("create") }
      );

      if (response.ok) {
        const created = await unwrap<AvailabilityCommitment>(response);
        submission.completed("create");
        toast.success(t("availability.commitmentAgreed"), {
          description: t(
            "availability.measuredAgainstFromNow",
            formatUptime(created.availabilityTarget)
          ),
        });
        setFormOpen(false);
        setBands([emptyBand()]);
        setEffectiveFrom("");
        setEffectiveTo("");
        fetchCommitments();
      } else {
        await reportApiError(response, {
          fallback: t("availability.errorOccurredSavingCommitment"),
          onUnauthorized,
          toastId: "error-saving-commitment",
        });
      }
    } catch {
      toast.error(t("availability.errorOccurredSavingCommitment"), {
        id: "error-saving-commitment",
      });
    } finally {
      setSaving(false);
    }
  };

  const remove = async () => {
    if (deleting === null) return;

    setSaving(true);
    const intent = `delete-${deleting.availabilityCommitmentId}`;

    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationId}/availability/commitments/${deleting.availabilityCommitmentId}`,
        {
          method: "DELETE",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
        },
        { idempotencyKey: submission.current(intent) }
      );

      if (response.ok) {
        submission.completed(intent);
        toast.success(t("common.commitmentRemoved"));
        setDeleting(null);
        fetchCommitments();
      } else {
        await reportApiError(response, {
          fallback: t("availability.errorOccurredRemovingCommitment"),
          onUnauthorized,
          toastId: "error-deleting-commitment",
        });
      }
    } catch {
      toast.error(t("availability.errorOccurredRemovingCommitment"), {
        id: "error-deleting-commitment",
      });
    } finally {
      setSaving(false);
    }
  };

  if (!canRead) {
    return (
      <Card>
        <CardContent className="p-6">
          <p className="text-sm leading-relaxed text-slate-500">
            Reading agreed targets needs{" "}
            <code>availability-commitments:read</code>.
          </p>
        </CardContent>
      </Card>
    );
  }

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <label className="flex items-center gap-2 text-sm text-slate-600">
          <input
            type="checkbox"
            checked={includeSuperseded}
            onChange={(event) => setIncludeSuperseded(event.target.checked)}
            className="size-4 rounded border-slate-300 accent-brand-800"
          />
          {t("availability.includeSupersededTerms")}
        </label>
        {canWrite && (
          <Button onClick={() => setFormOpen(true)} className="gap-1.5">
            <Plus className="size-4" />
            {t("availability.agreeATarget")}
          </Button>
        )}
      </div>

      {loading ? (
        <Skeleton className="h-56 w-full rounded-lg" />
      ) : commitments.length === 0 ? (
        <Card>
          <CardContent className="p-6">
            <p className="text-sm leading-relaxed text-slate-500">
              No availability target has been agreed with this organisation.
              That is the ordinary state — a target is something negotiated, not
              a default — and it means their availability is measured and
              reported without any promise attached to it.
            </p>
          </CardContent>
        </Card>
      ) : (
        <div className="space-y-4">
          {commitments.map((commitment) => (
            <CommitmentCard
              key={commitment.availabilityCommitmentId}
              commitment={commitment}
              canWrite={canWrite}
              onDelete={() => setDeleting(commitment)}
            />
          ))}
        </div>
      )}

      <Dialog open={formOpen} onOpenChange={setFormOpen}>
        <DialogContent className="max-w-2xl">
          <DialogHeader>
            <DialogTitle>{t("availability.agreeAnAvailabilityTarget")}</DialogTitle>
            <DialogDescription>
              {CREATING_CLOSES_THE_OPEN_TERM}
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-4">
            <div className="flex flex-wrap gap-4">
              <div>
                <label className="font-mono text-xs uppercase tracking-widest text-slate-400">
                  {t("availability.uptimeTarget")}
                </label>
                <Input
                  value={availabilityTarget}
                  onChange={(event) =>
                    setAvailabilityTarget(event.target.value)
                  }
                  inputMode="decimal"
                  className="mt-1.5 w-40"
                />
              </div>
              <div>
                <label className="font-mono text-xs uppercase tracking-widest text-slate-400">
                  {t("availability.latencyTargetMs")}
                </label>
                <Input
                  value={latencyTarget}
                  onChange={(event) => setLatencyTarget(event.target.value)}
                  inputMode="numeric"
                  max={LATENCY_TARGET_MAX_MS}
                  className="mt-1.5 w-44"
                />
              </div>
              <div>
                <label className="font-mono text-xs uppercase tracking-widest text-slate-400">
                  {t("availability.percentile")}
                </label>
                <Input
                  value={percentile}
                  onChange={(event) => setPercentile(event.target.value)}
                  inputMode="numeric"
                  className="mt-1.5 w-28"
                />
              </div>
            </div>

            <p className="text-xs leading-relaxed text-slate-500">
              {TARGET_HINT}
            </p>
            <p className="text-xs leading-relaxed text-slate-500">
              {PERCENTILE_HINT}
            </p>

            <div className="flex flex-wrap gap-4">
              <div>
                <label className="font-mono text-xs uppercase tracking-widest text-slate-400">
                  {t("availability.effectiveFrom")}
                </label>
                <DateTimePicker
                  value={effectiveFrom}
                  onChange={setEffectiveFrom}
                  placeholder={t("availability.startsNow")}
                  aria-label={t("availability.availabilityCommitmentEffectiveFrom")}
                  className="mt-1.5 w-60"
                />
                <p className="mt-1 text-xs text-slate-400">
                  {t("availability.blankMeansNow")}
                </p>
              </div>
              <div>
                <label className="font-mono text-xs uppercase tracking-widest text-slate-400">
                  {t("availability.effectiveTo")}
                </label>
                <DateTimePicker
                  value={effectiveTo}
                  onChange={setEffectiveTo}
                  placeholder={t("availability.openEnded")}
                  aria-label={t("availability.availabilityCommitmentEffectiveTo")}
                  className="mt-1.5 w-60"
                />
                <p className="mt-1 text-xs text-slate-400">
                  {t("availability.blankLeavesTheTermOpen")}
                </p>
              </div>
            </div>

            <CreditBandEditor bands={bands} onChange={setBands} />

            {refusal !== null && (
              <p className="text-sm leading-relaxed text-critical-700">
                {refusal}
              </p>
            )}
          </div>

          <DialogFooter>
            <Button variant="outline" onClick={() => setFormOpen(false)}>
              {t("common.cancel")}
            </Button>
            <Button onClick={save} disabled={refusal !== null || saving}>
              {saving ? "Saving…" : "Agree target"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <Dialog
        open={deleting !== null}
        onOpenChange={(open) => !open && setDeleting(null)}
      >
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{t("availability.removeThisCommitment")}</DialogTitle>
            <DialogDescription>
              The customer stops being measured against any target from this
              term onward. Service credits already issued against it are not
              affected — they are a record of a month that has already happened.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={() => setDeleting(null)}>
              {t("common.keepIt")}
            </Button>
            <Button variant="destructive" onClick={remove} disabled={saving}>
              {saving ? "Removing…" : "Remove"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}

function CommitmentCard({
  commitment,
  canWrite,
  onDelete,
}: {
  commitment: AvailabilityCommitment;
  canWrite: boolean;
  onDelete: () => void;
}) {
  const t = useTranslation();
  const state = termState(commitment);

  return (
    <Card>
      <CardContent className="space-y-4 p-6">
        <div className="flex flex-wrap items-start justify-between gap-x-4 gap-y-2">
          <div className="flex flex-wrap items-center gap-2">
            <p className="font-mono text-lg tracking-tight text-slate-900">
              {formatUptime(commitment.availabilityTarget)}
            </p>
            <ToneBadge
              tone={
                state === "current"
                  ? "settled"
                  : state === "scheduled"
                    ? "inflight"
                    : "inert"
              }
            >
              {state === "current"
                ? "In force"
                : state === "scheduled"
                  ? "Scheduled"
                  : "Superseded"}
            </ToneBadge>
          </div>
          {canWrite && (
            <Button
              variant="ghost"
              size="sm"
              aria-label={t("availability.removeCommitment")}
              onClick={onDelete}
            >
              <Trash2 className="size-4 text-slate-400" />
            </Button>
          )}
        </div>

        <div className="flex flex-wrap gap-x-6 gap-y-1 font-mono text-[11px] uppercase tracking-widest text-slate-400">
          <span>
            p{commitment.latencyPercentile} under{" "}
            {commitment.latencyTargetMilliseconds} ms
          </span>
          <span>
            {t("common.from")} <LocalTime value={commitment.effectiveFrom} dateOnly />
          </span>
          <span>
            {commitment.effectiveTo === null ? (
              "open-ended"
            ) : (
              <>
                until <LocalTime value={commitment.effectiveTo} dateOnly />
              </>
            )}
          </span>
        </div>

        <div>
          <p className="font-mono text-xs uppercase tracking-widest text-slate-400">
            {t("availability.creditSchedule")}
          </p>
          {commitment.creditBands.length === 0 ? (
            <p className="mt-2 text-xs leading-relaxed text-slate-500">
              {EMPTY_SCHEDULE_IS_VALID}
            </p>
          ) : (
            <ul className="mt-2 space-y-1.5">
              {bandsWorstFirst(commitment.creditBands).map((band) => (
                <li
                  key={band.serviceCreditBandId}
                  className="text-xs leading-relaxed text-slate-600"
                >
                  {bandSentence(band)}
                </li>
              ))}
            </ul>
          )}
        </div>
      </CardContent>
    </Card>
  );
}

function CreditBandEditor({
  bands,
  onChange,
}: {
  bands: CreditBandDraft[];
  onChange: (bands: CreditBandDraft[]) => void;
}) {
  const t = useTranslation();
  const update = (index: number, patch: Partial<CreditBandDraft>) => {
    onChange(
      bands.map((band, position) =>
        position === index ? { ...band, ...patch } : band
      )
    );
  };

  return (
    <div className="space-y-2">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <label className="font-mono text-xs uppercase tracking-widest text-slate-400">
          {t("availability.creditSchedule")}
        </label>
        <Button
          type="button"
          variant="outline"
          size="sm"
          className="gap-1.5"
          onClick={() => onChange([...bands, emptyBand()])}
        >
          <Plus className="size-3.5" />
          {t("availability.addBand")}
        </Button>
      </div>

      {bands.length === 0 ? (
        <p className="text-xs leading-relaxed text-slate-500">
          {EMPTY_SCHEDULE_IS_VALID}
        </p>
      ) : (
        <div className="space-y-2">
          {bands.map((band, index) => (
            <div key={index} className="flex flex-wrap items-end gap-2">
              <div>
                <label className="font-mono text-[11px] uppercase tracking-widest text-slate-400">
                  {t("availability.belowUptime")}
                </label>
                <Input
                  value={band.belowUptimePercentage}
                  onChange={(event) =>
                    update(index, { belowUptimePercentage: event.target.value })
                  }
                  inputMode="decimal"
                  placeholder="99.9"
                  className="mt-1 w-32"
                />
              </div>
              <div>
                <label className="font-mono text-[11px] uppercase tracking-widest text-slate-400">
                  {t("availability.credit")}
                </label>
                <Input
                  value={band.creditPercentage}
                  onChange={(event) =>
                    update(index, { creditPercentage: event.target.value })
                  }
                  inputMode="decimal"
                  placeholder="10"
                  className="mt-1 w-28"
                />
              </div>
              <Button
                type="button"
                variant="ghost"
                size="sm"
                aria-label={t("availability.removeBand")}
                onClick={() =>
                  onChange(bands.filter((_, position) => position !== index))
                }
              >
                <Trash2 className="size-4 text-slate-400" />
              </Button>
            </div>
          ))}
        </div>
      )}

      <p className="text-xs leading-relaxed text-slate-500">
        A threshold is exclusive: a month measuring exactly at the figure is
        above the band and owes nothing. {EMPTY_SCHEDULE_IS_VALID}
      </p>
    </div>
  );
}
