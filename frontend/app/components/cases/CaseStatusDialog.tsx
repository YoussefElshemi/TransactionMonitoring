"use client";

import { useEffect, useState } from "react";

import { CaseStatus } from "@/types";
import { cn } from "@/lib/utils";
import {
  DESK_STATUS_HINTS,
  allowedTransitionsFrom,
  caseStatusLabel,
} from "@/lib/cases";
import { Button } from "@/components/ui/button";
import { Textarea } from "@/components/ui/textarea";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { useTranslation } from "@/context/LocalizationContext";

export function CaseStatusDialog({
  open,
  onOpenChange,
  currentStatus,
  submitting,
  onSubmit,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  currentStatus: string;
  submitting: boolean;
  onSubmit: (status: CaseStatus, note: string | null) => void;
}) {
  const t = useTranslation();
  const [status, setStatus] = useState<CaseStatus | null>(null);
  const [note, setNote] = useState("");

  const targets = allowedTransitionsFrom(currentStatus);

  useEffect(() => {
    if (!open) return;
    setStatus(null);
    setNote("");
  }, [open]);

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>{t("cases.moveThisCase")}</DialogTitle>
          <DialogDescription>
            Where the work has got to. The payments stay held, new alerts about
            the same subject still join this case, and you can still close it
            from any of these.
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4">
          <div className="space-y-2">
            {targets.map((target) => (
              <button
                key={target}
                type="button"
                onClick={() => setStatus(target)}
                aria-pressed={status === target}
                className={cn(
                  "flex w-full flex-col gap-0.5 rounded-lg border px-3 py-2.5 text-left transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-600",
                  status === target
                    ? "border-inflight-500 bg-inflight-50 text-inflight-700"
                    : "border-slate-200 text-slate-600 hover:bg-slate-50"
                )}
              >
                <span className="text-sm font-medium">
                  {caseStatusLabel(target)}
                </span>
                <span className="text-xs opacity-80">
                  {DESK_STATUS_HINTS[target]}
                </span>
              </button>
            ))}
          </div>

          <div className="space-y-1.5">
            <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
              {t("cases.note")}
            </label>
            <Textarea
              value={note}
              onChange={(event) => setNote(event.target.value)}
              rows={3}
              placeholder={t("cases.optionalWhatYouAreWaiting")}
            />
            <p className="text-xs text-slate-500">
              Written as a case note, so it shows up in the case&apos;s notes
              afterwards.
            </p>
          </div>
        </div>

        <DialogFooter>
          <Button
            variant="outline"
            onClick={() => onOpenChange(false)}
            disabled={submitting}
          >
            {t("common.cancel")}
          </Button>
          <Button
            onClick={() =>
              status && onSubmit(status, note.trim().length > 0 ? note.trim() : null)
            }
            disabled={status === null || submitting}
          >
            {submitting ? "Moving..." : "Move case"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
