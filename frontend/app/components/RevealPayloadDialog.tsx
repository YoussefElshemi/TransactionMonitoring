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

const MIN_REASON = 3;
const MAX_REASON = 512;

export function RevealPayloadDialog({
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

  const trimmed = reason.trim();

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>{t("revealPayloadDialog.revealThisPayload")}</DialogTitle>
          <DialogDescription>
            Shows the payment as it was sent, with nothing withheld, for as long
            as this screen stays open.
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4">
          <div className="flex gap-2 rounded-md border border-attention-200 bg-attention-50 px-3 py-2">
            <TriangleAlert className="mt-0.5 size-4 flex-shrink-0 text-attention-700" />
            <div className="text-xs text-attention-700">
              <p className="font-medium">{t("revealPayloadDialog.thisIsRecorded")}</p>
              <p className="mt-0.5">
                Your name, the time and the reason below are written to this
                payment&apos;s history and cannot be edited or withdrawn. What
                you go on to read is not recorded.
              </p>
            </div>
          </div>

          <div className="space-y-1.5">
            <label
              htmlFor="reveal-reason"
              className="flex items-center gap-1 font-mono text-xs uppercase tracking-widest text-slate-500"
            >
              {t("common.reason")}
              <span className="text-critical-700">*</span>
            </label>
            <Textarea
              id="reveal-reason"
              value={reason}
              onChange={(event) => setReason(event.target.value)}
              rows={3}
              maxLength={MAX_REASON}
              placeholder={t("revealPayloadDialog.whyThisTransactionHasTo")}
            />
            <p className="text-xs text-slate-500">
              Required. Write it for whoever reads this trail without you in the
              room — a case reference, or what you are checking and why.
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
            onClick={() => onSubmit(trimmed)}
            disabled={trimmed.length < MIN_REASON || submitting}
          >
            {submitting ? "Revealing..." : "Reveal payload"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
