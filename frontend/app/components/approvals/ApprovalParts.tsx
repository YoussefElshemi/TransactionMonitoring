"use client";

import Link from "next/link";
import { Clock, FileText, FolderOpen, Rocket } from "lucide-react";

import { Approval, ApprovalResource, CaseReasonCode } from "@/types";
import {
  approvalStatusLabel,
  approvalStatusText,
  approvalSubjectLabel,
  approvalAgeMs,
  isStaleApproval,
} from "@/lib/approvals";
import { durationLabel } from "@/lib/ageing";
import { filingHref } from "@/lib/suspiciousActivityReports";
import { relationText, shortId, userName } from "@/lib/resources";
import { cn } from "@/lib/utils";
import { DispositionBadge } from "@/components/DispositionBadge";
import { StatusBadge } from "@/components/StatusBadge";

import { LocalTime } from "@/components/LocalTime";
import { useFormat } from "@/lib/useFormat";
import { useTranslation } from "@/context/LocalizationContext";

export function ApprovalStatusBadge({ status }: { status: string }) {
  return <StatusBadge status={status} label={approvalStatusLabel(status)} />;
}

const SUBJECT_ICONS: Record<string, typeof Rocket> = {
  RuleDeployment: Rocket,
  CaseResolution: FolderOpen,
  SuspiciousActivityReport: FileText,
};

export function ApprovalSubjectChip({
  subject,
  className,
}: {
  subject: string;
  className?: string;
}) {
  const Icon = SUBJECT_ICONS[subject] ?? FolderOpen;
  return (
    <span
      className={cn(
        "inline-flex items-center gap-1.5 text-sm text-slate-700",
        className
      )}
    >
      <Icon className="size-3.5 flex-shrink-0 text-slate-400" />
      {approvalSubjectLabel(subject)}
    </span>
  );
}

export function ApprovalAge({ approval }: { approval: Approval }) {
  const format = useFormat();
  const age = durationLabel(approvalAgeMs(approval));
  const stale = isStaleApproval(approval);

  return (
    <span
      className={cn(
        "inline-flex items-center gap-1.5 font-mono text-xs",
        stale ? "text-attention-700" : "text-slate-500"
      )}
      title={`Raised ${format.instant(approval.createdAt)}`}
    >
      {stale && <Clock className="size-3.5 flex-shrink-0" />}
      {age} ago
    </span>
  );
}

export function RequestedBy({ resource }: { resource: ApprovalResource }) {
  return (
    <span className="text-sm text-slate-700">
      {relationText(
        userName(resource.requestedByUser),
        resource.approval.requestedByUserId
      )}
    </span>
  );
}

export function ProposedResolutionSummary({ approval }: { approval: Approval }) {
  const proposed = approval.proposedCaseResolution;
  if (!proposed) return <span className="text-sm text-slate-400">—</span>;

  return (
    <span className="inline-flex flex-wrap items-center gap-1.5">
      <DispositionBadge disposition={proposed.disposition} />
      <span className="font-mono text-xs text-slate-500">
        {proposed.status === "Approved" ? "release" : "block"}
      </span>
    </span>
  );
}

export function ApprovalProposalSummary({
  resource,
}: {
  resource: ApprovalResource;
}) {
  const { approval, suspiciousActivityReport: report } = resource;

  if (approval.subject === "SuspiciousActivityReport") {
    if (!report) {
      return (
        <span className="font-mono text-xs text-slate-400">
          {approval.suspiciousActivityReportId
            ? shortId(approval.suspiciousActivityReportId)
            : "—"}
        </span>
      );
    }

    const lines = report.narrative.split("\n").length;
    return (
      <span className="inline-flex flex-wrap items-center gap-x-2 gap-y-1">
        <span className="font-mono text-xs text-slate-700">
          {report.friendlyName}
        </span>
        <span className="font-mono text-xs text-slate-500">
          narrative, {lines} {lines === 1 ? "line" : "lines"}
        </span>
      </span>
    );
  }

  return <ProposedResolutionSummary approval={approval} />;
}

export function ProposedResolutionPanel({
  approval,
  reasonCodes,
}: {
  approval: Approval;
  reasonCodes: CaseReasonCode[];
}) {
  const t = useTranslation();
  const proposed = approval.proposedCaseResolution;
  if (!proposed) return null;

  const code = proposed.caseReasonCodeId
    ? reasonCodes.find(
        (candidate) => candidate.caseReasonCodeId === proposed.caseReasonCodeId
      )
    : undefined;

  return (
    <div className="space-y-4 rounded-lg border border-slate-200 bg-card p-4">
      <div className="flex flex-wrap items-center gap-x-6 gap-y-3">
        <Detail label={t("approvals.verdictOnTheMoney")}>
          <span className="text-sm text-slate-900">
            {proposed.status === "Approved"
              ? "Release — the payments go through"
              : "Block — the payments are rejected"}
          </span>
        </Detail>
        <Detail label={t("approvals.disposition")}>
          <DispositionBadge disposition={proposed.disposition} />
        </Detail>
        <Detail label={t("approvals.reasonCode")}>
          {proposed.caseReasonCodeId ? (
            <span className="text-sm text-slate-900">
              <span className="font-mono">
                {code?.code ?? shortId(proposed.caseReasonCodeId)}
              </span>
              {code?.description && (
                <span className="text-slate-500"> — {code.description}</span>
              )}
            </span>
          ) : (
            <span className="text-sm text-slate-400">{t("approvals.none")}</span>
          )}
        </Detail>
      </div>

      <div>
        <p className="font-mono text-xs uppercase tracking-widest text-slate-400">
          {t("approvals.proposedNote")}
        </p>
        {proposed.note ? (
          <p className="mt-1 whitespace-pre-wrap text-sm text-slate-700">
            {proposed.note}
          </p>
        ) : (
          <p className="mt-1 text-sm text-slate-400">{t("approvals.noneGiven")}</p>
        )}
        <p className="mt-2 max-w-prose text-xs text-slate-500">
          Not a case note. It explains a close that has not happened — it is
          written onto the case only if this request is approved.
        </p>
      </div>
    </div>
  );
}

function Detail({
  label,
  children,
}: {
  label: string;
  children: React.ReactNode;
}) {
  return (
    <div className="min-w-0 space-y-1">
      <p className="font-mono text-xs uppercase tracking-widest text-slate-400">
        {label}
      </p>
      {children}
    </div>
  );
}

export function ApprovalOutcomeNote({
  resource,
  className,
}: {
  resource: ApprovalResource;
  className?: string;
}) {
  const { approval } = resource;
  const meaning = approvalStatusText(approval.status);
  if (!meaning) return null;

  const reviewer = relationText(
    userName(resource.reviewedByUser),
    approval.reviewedByUserId,
    approval.status === "Withdrawn" ? "the requester" : "—"
  );

  return (
    <div className={cn("space-y-2", className)}>
      <p className="text-sm text-slate-600">{meaning}</p>
      {approval.reviewedAt && (
        <p className="font-mono text-xs text-slate-500">
          {approval.status === "Withdrawn" ? "Withdrawn" : "Answered by"}{" "}
          {approval.status === "Withdrawn" ? "" : `${reviewer} `}
          <LocalTime value={approval.reviewedAt} />
        </p>
      )}
      {approval.reviewNote && (
        <div>
          <p className="font-mono text-xs uppercase tracking-widest text-slate-400">
            {approval.status === "Withdrawn"
              ? "Withdrawal note"
              : "Reviewer's note"}
          </p>
          <p className="mt-1 whitespace-pre-wrap text-sm text-slate-700">
            {approval.reviewNote}
          </p>
          {approval.status === "Rejected" && approval.caseId && (
            <p className="mt-2 text-xs text-slate-500">
              This note was also written onto the case, so it appears in its
              notes.
            </p>
          )}
        </div>
      )}
    </div>
  );
}

export function ApprovalTargetLink({ approval }: { approval: Approval }) {
  const t = useTranslation();
  if (approval.suspiciousActivityReportId) {
    return (
      <Link
        href={filingHref(approval.suspiciousActivityReportId)}
        className="font-mono text-xs text-brand-800 hover:underline"
      >
        {shortId(approval.suspiciousActivityReportId)}
      </Link>
    );
  }

  if (approval.caseId) {
    return (
      <Link
        href={`/cases/${approval.caseId}`}
        className="font-mono text-xs text-brand-800 hover:underline"
      >
        {shortId(approval.caseId)}
      </Link>
    );
  }

  if (approval.ruleDeploymentId) {
    return (
      <span
        className="font-mono text-xs text-slate-500"
        title={t("approvals.deploymentIdOpenTheApproval")}
      >
        {shortId(approval.ruleDeploymentId)}
      </span>
    );
  }

  return <span className="font-mono text-xs text-slate-400">—</span>;
}
