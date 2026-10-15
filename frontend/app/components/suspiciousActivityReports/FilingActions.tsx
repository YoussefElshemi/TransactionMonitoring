"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { toast } from "sonner";
import { Ban, Check, Download, Inbox, Send, Stamp } from "lucide-react";

import {
  ApprovalResource,
  ProblemDetails,
  SuspiciousActivityReport,
  SuspiciousActivityReportResource,
  ValidationProblemDetails,
} from "@/types";
import { API_BASE, reportApiError, unwrap } from "@/lib/api";
import { approvalHref } from "@/lib/approvals";
import { saveResponseAsFile } from "@/lib/download";
import {
  FilingPermissions,
  filingExportFileName,
  filingExportPath,
} from "@/lib/suspiciousActivityReports";
import { cn } from "@/lib/utils";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
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

type FilingAction = "submit" | "file" | "acknowledge" | "decline";

interface ActionCopy {
  titleKey: string;
  descriptionKey: string;
  field?: {
    labelKey: string;
    name: string;
    required: boolean;
    multiline: boolean;
    placeholderKey: string;
    hintKey: string;
  };
  confirmKey: string;
  pendingKey: string;
}

const COPY: Record<FilingAction, ActionCopy> = {
  submit: {
    titleKey: "suspiciousActivityReports.submitTitle",
    descriptionKey: "",
    field: {
      labelKey: "common.reason",
      name: "reason",
      required: false,
      multiline: true,
      placeholderKey: "suspiciousActivityReports.submitReasonPlaceholder",
      hintKey: "suspiciousActivityReports.submitReasonHint",
    },
    confirmKey: "suspiciousActivityReports.submitConfirm",
    pendingKey: "suspiciousActivityReports.submitPending",
  },
  file: {
    titleKey: "suspiciousActivityReports.fileTitle",
    descriptionKey: "suspiciousActivityReports.fileDescription",
    field: {
      labelKey: "suspiciousActivityReports.filingReferenceLabel",
      name: "filingReference",
      required: true,
      multiline: false,
      placeholderKey: "suspiciousActivityReports.filingReferencePlaceholder",
      hintKey: "suspiciousActivityReports.filingReferenceHint",
    },
    confirmKey: "suspiciousActivityReports.fileConfirm",
    pendingKey: "suspiciousActivityReports.recordingPending",
  },
  acknowledge: {
    titleKey: "suspiciousActivityReports.acknowledgeTitle",
    descriptionKey: "suspiciousActivityReports.acknowledgeDescription",
    field: {
      labelKey: "suspiciousActivityReports.acknowledgementReferenceLabel",
      name: "acknowledgementReference",
      required: false,
      multiline: false,
      placeholderKey: "suspiciousActivityReports.acknowledgementReferencePlaceholder",
      hintKey: "suspiciousActivityReports.acknowledgementReferenceHint",
    },
    confirmKey: "suspiciousActivityReports.acknowledgeConfirm",
    pendingKey: "suspiciousActivityReports.recordingPending",
  },
  decline: {
    titleKey: "suspiciousActivityReports.declineTitle",
    descriptionKey: "suspiciousActivityReports.declineDescription",
    field: {
      labelKey: "common.reason",
      name: "reason",
      required: true,
      multiline: true,
      placeholderKey: "suspiciousActivityReports.declineReasonPlaceholder",
      hintKey: "suspiciousActivityReports.declineReasonHint",
    },
    confirmKey: "suspiciousActivityReports.declineConfirm",
    pendingKey: "suspiciousActivityReports.recordingPending",
  },
};

const submitDescription = (requireFilingApproval: boolean): string =>
  requireFilingApproval
    ? "Your organisation requires a second pair of eyes on a filing, so this raises a four-eyes request over the narrative and freezes it — you will not be able to edit it while somebody reads it, and a rejection sends it back to draft. Nothing goes to the financial intelligence unit, and the deadline keeps running."
    : "Your organisation does not require a second pair of eyes on a filing, so this marks the report approved and ready to send. It does not send it — somebody still has to submit it to the financial intelligence unit and record the reference — and the deadline keeps running until they do.";

const messagesFor = (
  errors: Record<string, string[]> | undefined,
  field: string
): string[] | null => {
  if (!errors) return null;
  const wanted = field.toLowerCase();
  const key = Object.keys(errors).find((candidate) => {
    const lowered = candidate.toLowerCase();
    return lowered === wanted || lowered.endsWith(`.${wanted}`);
  });
  return key ? errors[key] : null;
};

export function FilingActions({
  report,
  requireFilingApproval,
  permissions,
  onUpdated,
  onConflict,
  onUnauthorized,
  policyHref,
  canManagePolicy,
}: {
  report: SuspiciousActivityReport;
  requireFilingApproval: boolean;
  permissions: FilingPermissions;
  onUpdated: (updated: SuspiciousActivityReportResource) => void;
  onConflict: () => void;
  onUnauthorized: () => void;
  policyHref: string;
  canManagePolicy: boolean;
}) {
  const t = useTranslation();
  const router = useRouter();
  const [action, setAction] = useState<FilingAction | null>(null);
  const [value, setValue] = useState("");
  const [fieldError, setFieldError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [exporting, setExporting] = useState(false);

  const copy = action ? COPY[action] : null;

  useEffect(() => {
    if (!action) return;
    setValue("");
    setFieldError(null);
  }, [action]);

  const run = async () => {
    if (!action || !copy) return;

    const trimmed = value.trim();
    if (copy.field?.required && trimmed.length === 0) {
      setFieldError(t("common.fieldIsRequired", t(copy.field.labelKey)));
      return;
    }

    setSubmitting(true);
    setFieldError(null);

    try {
      const response = await apiFetch(
        `${API_BASE}/suspicious-activity-reports/${report.suspiciousActivityReportId}/${action}`,
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
          body: JSON.stringify(
            copy.field && trimmed.length > 0 ? { [copy.field.name]: trimmed } : {}
          ),
        }
      );

      if (response.ok) {
        if (action === "submit" && response.status === 202) {
          const approval = await unwrap<ApprovalResource>(response);
          setAction(null);
          toast.info(t("suspiciousActivityReports.sentApprovalNotFiled"), {
            description:
              t("suspiciousActivityReports.yourOrganisationRequiresSecondPair"),
            action: {
              label: t("approvals.openTheRequest"),
              onClick: () =>
                router.push(approvalHref(approval.approval.approvalId)),
            },
          });
          onConflict();
          return;
        }

        const updated = await unwrap<SuspiciousActivityReportResource>(
          response
        );
        setAction(null);
        onUpdated(updated);
        toast.success(t(SUCCESS[action].titleKey), {
          description: t(SUCCESS[action].descriptionKey),
        });
        return;
      }

      if (response.status === 400) {
        const problem = (await response.json().catch(() => null)) as
          | ValidationProblemDetails
          | null;
        const messages = copy.field
          ? messagesFor(problem?.errors, copy.field.name)
          : null;
        if (messages?.length) {
          setFieldError(messages.join("\n"));
          return;
        }
        const everything = problem?.errors
          ? Object.values(problem.errors).flat()
          : [];
        setFieldError(
          everything.length > 0
            ? everything.join("\n")
            : problem?.detail || "That was refused."
        );
        return;
      }

      if (response.status === 409) {
        const problem = (await response.json().catch(() => null)) as
          | ProblemDetails
          | null;
        toast.info(
          problem?.detail ||
            t("suspiciousActivityReports.filingHasAlreadyMovedSo")
        );
        setAction(null);
        onConflict();
        return;
      }

      await reportApiError(response, {
        fallback: t("suspiciousActivityReports.actionWasRefused"),
        onUnauthorized,
      });
      if (response.status === 403) onConflict();
    } catch {
      toast.error(t("suspiciousActivityReports.actionCouldNotCompleted"));
    } finally {
      setSubmitting(false);
    }
  };

  const handleExport = async () => {
    setExporting(true);

    try {
      const response = await apiFetch(
        `${API_BASE}${filingExportPath(report.suspiciousActivityReportId)}`,
        { method: "GET", credentials: "include" }
      );

      if (!response.ok) {
        if (response.status === 409) {
          const problem = (await response.json().catch(() => null)) as
            | ProblemDetails
            | null;
          toast.error(t("suspiciousActivityReports.noReportingEntityIdHas"), {
            description:
              problem?.detail ||
              t("suspiciousActivityReports.goAMLFileHasCarryIdentifier"),
            duration: 10_000,
            action: canManagePolicy
              ? {
                  label: t("suspiciousActivityReports.openFilingSettings"),
                  onClick: () => router.push(policyHref),
                }
              : undefined,
          });
          return;
        }

        await reportApiError(response, {
          fallback: t("suspiciousActivityReports.goAMLFileCouldNotProduced"),
          onUnauthorized,
          toastId: `export-${report.suspiciousActivityReportId}`,
        });
        return;
      }

      await saveResponseAsFile(response, filingExportFileName(report));
    } catch {
      toast.error(t("suspiciousActivityReports.goAMLFileCouldNotProduced"), {
        id: `export-${report.suspiciousActivityReportId}`,
      });
    } finally {
      setExporting(false);
    }
  };

  const blocked =
    submitting ||
    (copy?.field?.required === true && value.trim().length === 0);

  const showsManagerActions =
    report.status === "Draft" ||
    report.status === "Approved" ||
    report.status === "Filed";

  return (
    <>
      <div className="space-y-2">
        <div className="flex flex-wrap items-center gap-2">
          {report.status === "Draft" && (
            <Button
              className="gap-1.5"
              disabled={!permissions.canSubmit}
              onClick={() => setAction("submit")}
            >
              <Send className="size-4" />
              {t("suspiciousActivityReports.submit")}
            </Button>
          )}

          {report.status === "Approved" && (
            <Button
              className="gap-1.5"
              disabled={!permissions.canFile}
              onClick={() => setAction("file")}
            >
              <Stamp className="size-4" />
              {t("suspiciousActivityReports.recordAsFiled")}
            </Button>
          )}

          {report.status === "Filed" && (
            <Button
              className="gap-1.5"
              disabled={!permissions.canAcknowledge}
              onClick={() => setAction("acknowledge")}
            >
              <Check className="size-4" />
              {t("suspiciousActivityReports.recordAcknowledgement")}
            </Button>
          )}

          {(report.status === "Draft" || report.status === "Approved") && (
            <Button
              variant="outline"
              className="gap-1.5 border-slate-200 text-slate-600"
              disabled={!permissions.canDecline}
              onClick={() => setAction("decline")}
            >
              <Ban className="size-4" />
              {t("suspiciousActivityReports.decideNotToFile")}
            </Button>
          )}

          <Button
            variant="outline"
            className="gap-1.5"
            disabled={exporting}
            onClick={handleExport}
          >
            <Download className="size-4" />
            {exporting ? "Preparing..." : "Export goAML"}
          </Button>
        </div>

        {permissions.roleBlockedReason && showsManagerActions && (
          <p className="max-w-prose text-sm text-slate-500">
            {permissions.roleBlockedReason}
          </p>
        )}

        {report.status === "PendingApproval" && (
          <p className="flex flex-wrap items-center gap-x-2 gap-y-1 text-sm text-slate-500">
            <Inbox className="size-4 flex-shrink-0 text-slate-400" />
            A four-eyes request is open over this report. It is frozen until
            somebody answers it — and a rejection sends it back to draft rather
            than ending it.
            <Link
              href="/approvals"
              className="text-brand-800 hover:underline"
            >
              {t("suspiciousActivityReports.openTheApprovalsInbox")}
            </Link>
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
                  {action === "submit"
                    ? submitDescription(requireFilingApproval)
                    : t(copy.descriptionKey)}
                </DialogDescription>
              </DialogHeader>

              {copy.field && (
                <div className="space-y-1.5">
                  <label className="flex items-center gap-1 font-mono text-xs uppercase tracking-widest text-slate-500">
                    {t(copy.field.labelKey)}
                    {copy.field.required && (
                      <span className="text-critical-700">*</span>
                    )}
                  </label>
                  {copy.field.multiline ? (
                    <Textarea
                      value={value}
                      onChange={(event) => {
                        setValue(event.target.value);
                        if (fieldError) setFieldError(null);
                      }}
                      rows={4}
                      placeholder={t(copy.field.placeholderKey)}
                      aria-invalid={fieldError ? true : undefined}
                      className={cn(
                        fieldError &&
                          "border-critical-500 focus-visible:ring-critical-500"
                      )}
                    />
                  ) : (
                    <Input
                      value={value}
                      onChange={(event) => {
                        setValue(event.target.value);
                        if (fieldError) setFieldError(null);
                      }}
                      placeholder={t(copy.field.placeholderKey)}
                      aria-invalid={fieldError ? true : undefined}
                      className={cn(
                        "font-mono",
                        fieldError &&
                          "border-critical-500 focus-visible:ring-critical-500"
                      )}
                    />
                  )}
                  <p className="text-xs text-slate-500">{t(copy.field.hintKey)}</p>
                  {fieldError && (
                    <p className="whitespace-pre-line text-xs text-critical-700">
                      {fieldError}
                    </p>
                  )}
                </div>
              )}

              <DialogFooter>
                <Button
                  variant="outline"
                  onClick={() => setAction(null)}
                  disabled={submitting}
                >
                  {t("common.cancel")}
                </Button>
                <Button onClick={run} disabled={blocked}>
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

const SUCCESS: Record<
  FilingAction,
  { titleKey: string; descriptionKey: string }
> = {
  submit: {
    titleKey: "suspiciousActivityReports.approvedReadyToFile",
    descriptionKey: "suspiciousActivityReports.approvedReadyToFileDescription",
  },
  file: {
    titleKey: "suspiciousActivityReports.recordedAsFiled",
    descriptionKey: "suspiciousActivityReports.recordedAsFiledDescription",
  },
  acknowledge: {
    titleKey: "suspiciousActivityReports.acknowledgementRecorded",
    descriptionKey: "suspiciousActivityReports.acknowledgementRecordedDescription",
  },
  decline: {
    titleKey: "suspiciousActivityReports.recordedAsNotFiled",
    descriptionKey: "suspiciousActivityReports.recordedAsNotFiledDescription",
  },
};
