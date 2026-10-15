"use client";

import { useEffect, useState } from "react";
import { TriangleAlert } from "lucide-react";

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

export function ReopenCaseDialog({
  open,
  onOpenChange,
  submitting,
  onSubmit,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  submitting: boolean;
  onSubmit: (reason: string) => void;
}) {
  const t = useTranslation();
  const [reason, setReason] = useState("");

  useEffect(() => {
    if (!open) return;
    setReason("");
  }, [open]);

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>{t("cases.reopenThisCase")}</DialogTitle>
          <DialogDescription>
            The case goes back to In review and its alerts reopen, so further
            payments about the same subject join it again.
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4">
          <div className="flex gap-2 rounded-md border border-attention-200 bg-attention-50 px-3 py-2">
            <TriangleAlert className="mt-0.5 size-4 flex-shrink-0 text-attention-700" />
            <div className="text-xs text-attention-700">
              <p className="font-medium">{t("cases.noTransactionIsTouched")}</p>
              <p className="mt-0.5">
                Everything under this case was released or blocked when it
                closed and the provider has acted on it. Reopening puts the work
                back in front of an analyst — it does not recall the money.
              </p>
            </div>
          </div>

          <div className="space-y-1.5">
            <label className="flex items-center gap-1 font-mono text-xs uppercase tracking-widest text-slate-500">
              {t("common.reason")}
              <span className="text-critical-700">*</span>
            </label>
            <Textarea
              value={reason}
              onChange={(event) => setReason(event.target.value)}
              rows={3}
              placeholder={t("cases.whyThisNeedsLookingAt")}
            />
            <p className="text-xs text-slate-500">
              Required. The disposition and reason code recorded on this case
              are cleared — the previous answer, and who gave it, survive only
              in the history.
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
            onClick={() => onSubmit(reason.trim())}
            disabled={reason.trim().length === 0 || submitting}
          >
            {submitting ? "Reopening..." : "Reopen case"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
