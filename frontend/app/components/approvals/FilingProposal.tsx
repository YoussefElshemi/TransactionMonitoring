"use client";

import Link from "next/link";
import { ArrowRight } from "lucide-react";

import { ApprovalResource } from "@/types";
import { filingHref } from "@/lib/suspiciousActivityReports";
import { shortId } from "@/lib/resources";
import {
  FilingCaseLink,
  FilingDeadline,
  FilingReference,
  FilingStatusBadge,
  NarrativeExcerpt,
} from "@/components/suspiciousActivityReports/FilingParts";
import { useTranslation } from "@/context/LocalizationContext";

export function FilingProposal({ resource }: { resource: ApprovalResource }) {
  const t = useTranslation();
  const { approval, suspiciousActivityReport: report } = resource;

  if (!report) {
    return (
      <div className="rounded-lg border border-slate-200 bg-card p-4">
        <p className="text-sm text-slate-500">
          {t("approvals.theReportItselfWasNot")}
        </p>
        {approval.suspiciousActivityReportId && (
          <Link
            href={filingHref(approval.suspiciousActivityReportId)}
            className="mt-2 inline-flex items-center gap-1.5 text-sm text-brand-800 hover:underline"
          >
            Open filing {shortId(approval.suspiciousActivityReportId)}
            <ArrowRight className="size-3.5" />
          </Link>
        )}
      </div>
    );
  }

  return (
    <div className="space-y-4 rounded-lg border border-slate-200 bg-card p-4">
      <div className="flex flex-wrap items-center gap-x-4 gap-y-2">
        <FilingReference report={report} />
        <FilingStatusBadge status={report.status} />
        <FilingCaseLink caseId={report.caseId} />
        <FilingDeadline report={report} className="ml-auto" />
      </div>

      <div className="space-y-2">
        <p className="font-mono text-xs uppercase tracking-widest text-slate-400">
          {t("approvals.theNarrativeBeingSignedOff")}
        </p>
        <NarrativeExcerpt narrative={report.narrative} lines={14} />
        <Link
          href={filingHref(report.suspiciousActivityReportId)}
          className="inline-flex items-center gap-1.5 text-sm text-brand-800 hover:underline"
        >
          {t("approvals.openTheFilingToRead")}
          <ArrowRight className="size-3.5" />
        </Link>
      </div>

      <div className="space-y-2 border-t border-slate-100 pt-3">
        <p className="max-w-prose text-xs text-slate-500">
          <span className="font-medium text-slate-700">
            {t("approvals.approvingDoesNotFileThis")}
          </span>{" "}
          It marks the report ready to send. Somebody still has to submit it to
          the financial intelligence unit through whatever channel the unit uses
          and record the reference — and the deadline runs until they do.
        </p>
        <p className="max-w-prose text-xs text-slate-500">
          <span className="font-medium text-slate-700">
            {t("approvals.rejectingDoesNotEndIt")}
          </span>{" "}
          Unlike a rule deployment or a case decision, turning this down sends
          the report back to draft so it can be rewritten and submitted again.
          The filing stays open, and so does the deadline.
        </p>
      </div>
    </div>
  );
}
