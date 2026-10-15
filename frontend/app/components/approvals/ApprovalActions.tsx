"use client";

import { useEffect, useState } from "react";
import { toast } from "sonner";
import { Check, Undo2, X } from "lucide-react";

import { ApprovalResource, ValidationProblemDetails } from "@/types";
import { API_BASE, reportApiError, unwrap } from "@/lib/api";
import {
  ApprovalPermissions,
  REVIEW_NOTE_MAX,
  approvalOutcomeTextKey,
} from "@/lib/approvals";
import { cn } from "@/lib/utils";
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

import { apiFetch } from "@/lib/http";
import { useTranslation } from "@/context/LocalizationContext";

type ApprovalAction = "approve" | "reject" | "withdraw";

const APPROVED_OUTCOME: Record<string, string> = {
  RuleDeployment: "approvals.outcomeRolloutStarted",
  CaseResolution: "approvals.outcomeCaseClosed",
  SuspiciousActivityReport: "approvals.outcomeReportReadyToFile",
};

const NOTE_ERROR_KEY = "Body.Note";

interface ActionCopy {
  titleKey: string;
  descriptionKey: (subject: string) => string;
  noteRequired: boolean;
  notePromptKey: string;
  noteHintKey: string;
  confirmKey: string;
  pendingKey: string;
  successKey: string;
}

const COPY: Record<ApprovalAction, ActionCopy> = {
  approve: {
    titleKey: "approvals.approveThisRequest",
    descriptionKey: approvalOutcomeTextKey,
    noteRequired: false,
    notePromptKey: "approvals.approveNotePrompt",
    noteHintKey: "approvals.approveNoteHint",
    confirmKey: "approvals.approve",
    pendingKey: "approvals.approving",
    successKey: "approvals.approved",
  },
  reject: {
    titleKey: "approvals.rejectThisRequest",
    descriptionKey: (subject) => {
      switch (subject) {
        case "RuleDeployment":
          return "approvals.rejectRollout";
        case "SuspiciousActivityReport":
          return "approvals.rejectFiling";
        default:
          return "approvals.rejectCase";
      }
    },
    noteRequired: true,
    notePromptKey: "approvals.rejectNotePrompt",
    noteHintKey: "approvals.rejectNoteHint",
    confirmKey: "approvals.reject",
    pendingKey: "approvals.rejecting",
    successKey: "approvals.rejected",
  },
  withdraw: {
    titleKey: "approvals.withdrawThisRequest",
    descriptionKey: (subject) => {
      switch (subject) {
        case "RuleDeployment":
          return "approvals.withdrawRollout";
        case "SuspiciousActivityReport":
          return "approvals.withdrawFiling";
        default:
          return "approvals.withdrawCase";
      }
    },
    noteRequired: false,
    notePromptKey: "approvals.withdrawNotePrompt",
    noteHintKey: "approvals.withdrawNoteHint",
    confirmKey: "approvals.withdraw",
    pendingKey: "approvals.withdrawing",
    successKey: "approvals.withdrawn",
  },
};

export function ApprovalActions({
  resource,
  permissions,
  onSettled,
  onConflict,
  onUnauthorized,
}: {
  resource: ApprovalResource;
  permissions: ApprovalPermissions;
  onSettled: (updated: ApprovalResource) => void;
  onConflict: () => void;
  onUnauthorized: () => void;
}) {
  const t = useTranslation();
  const [action, setAction] = useState<ApprovalAction | null>(null);

  const failedActionKey =
    action === "approve"
      ? "approvals.failedApproveRequest"
      : action === "reject"
        ? "approvals.failedRejectRequest"
        : "approvals.failedWithdrawRequest";
  const [note, setNote] = useState("");
  const [noteError, setNoteError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const { approval } = resource;
  const copy = action ? COPY[action] : null;

  useEffect(() => {
    if (!action) return;
    setNote("");
    setNoteError(null);
  }, [action]);

  const submit = async () => {
    if (!action || !copy) return;

    const trimmed = note.trim();
    if (copy.noteRequired && trimmed.length === 0) {
      setNoteError("A note is required to reject a request.");
      return;
    }

    setSubmitting(true);
    setNoteError(null);

    try {
      const response = await apiFetch(
        `${API_BASE}/approvals/${approval.approvalId}/${action}`,
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
          body: JSON.stringify(trimmed.length > 0 ? { note: trimmed } : {}),
        }
      );

      if (response.ok) {
        const updated = await unwrap<ApprovalResource>(response);
        toast.success(t(copy.successKey), {
          description:
            action === "approve"
              ? APPROVED_OUTCOME[approval.subject]
                ? t(APPROVED_OUTCOME[approval.subject])
                : t("approvals.actionRequestHoldingHasGone")
              : undefined,
        });
        setAction(null);
        onSettled(updated);
        return;
      }

      if (response.status === 400) {
        const body = (await response.json().catch(() => null)) as
          | ValidationProblemDetails
          | null;
        const messages = body?.errors?.[NOTE_ERROR_KEY];
        if (messages?.length) {
          setNoteError(messages.join("\n"));
          return;
        }
        toast.error(body?.detail || t("approvals.requestRefused"));
        return;
      }

      if (response.status === 409) {
        const body = (await response.json().catch(() => null)) as {
          detail?: string;
        } | null;
        toast.info(
          body?.detail || t("approvals.requestHasAlreadyBeenAnswered")
        );
        setAction(null);
        onConflict();
        return;
      }

      await reportApiError(response, {
        fallback: t(failedActionKey),
        onUnauthorized,
      });
    } catch {
      toast.error(t(failedActionKey));
    } finally {
      setSubmitting(false);
    }
  };

  const blocked =
    submitting ||
    (copy?.noteRequired === true && note.trim().length === 0) ||
    note.length > REVIEW_NOTE_MAX;

  return (
    <>
      <div className="space-y-2">
        <div className="flex flex-wrap items-center gap-2">
          <Button
            className="gap-1.5"
            disabled={!permissions.canReview}
            onClick={() => setAction("approve")}
          >
            <Check className="size-4" />
            {t("approvals.approve")}
          </Button>
          <Button
            variant="outline"
            className="gap-1.5 border-critical-200 text-critical-700 hover:bg-critical-50 hover:text-critical-700"
            disabled={!permissions.canReview}
            onClick={() => setAction("reject")}
          >
            <X className="size-4" />
            {t("approvals.reject")}
          </Button>
          {permissions.canWithdraw && (
            <Button
              variant="outline"
              className="gap-1.5"
              onClick={() => setAction("withdraw")}
            >
              <Undo2 className="size-4" />
              {t("approvals.withdraw")}
            </Button>
          )}
        </div>

        {permissions.reviewBlockedReason && (
          <p className="max-w-prose text-sm text-slate-500">
            {permissions.reviewBlockedReason}
          </p>
        )}
      </div>

      <Dialog
        open={action !== null}
        onOpenChange={(open) => {
          if (!open && !submitting) setAction(null);
        }}
      >
        <DialogContent className="sm:max-w-lg">
          {copy && (
            <>
              <DialogHeader>
                <DialogTitle>{t(copy.titleKey)}</DialogTitle>
                <DialogDescription>
                  {t(copy.descriptionKey(approval.subject))}
                </DialogDescription>
              </DialogHeader>

              <div className="space-y-1.5">
                <label className="flex items-center gap-1 font-mono text-xs uppercase tracking-widest text-slate-500">
                  Note
                  {copy.noteRequired && (
                    <span className="text-critical-700">*</span>
                  )}
                </label>
                <Textarea
                  value={note}
                  onChange={(event) => {
                    setNote(event.target.value);
                    if (noteError) setNoteError(null);
                  }}
                  rows={4}
                  maxLength={REVIEW_NOTE_MAX}
                  placeholder={t(copy.notePromptKey)}
                  aria-invalid={noteError ? true : undefined}
                  className={cn(
                    noteError &&
                      "border-critical-500 focus-visible:ring-critical-500"
                  )}
                />
                <p className="text-xs text-slate-500">{t(copy.noteHintKey)}</p>
                {noteError && (
                  <p className="whitespace-pre-line text-xs text-critical-700">
                    {noteError}
                  </p>
                )}
              </div>

              <DialogFooter>
                <Button
                  variant="outline"
                  onClick={() => setAction(null)}
                  disabled={submitting}
                >
                  {t("common.cancel")}
                </Button>
                <Button onClick={submit} disabled={blocked}>
                  {submitting ? t(copy.pendingKey) : t(copy.confirmKey)}
                </Button>
              </DialogFooter>
            </>
          )}
        </DialogContent>
      </Dialog>
    </>
  );
}
