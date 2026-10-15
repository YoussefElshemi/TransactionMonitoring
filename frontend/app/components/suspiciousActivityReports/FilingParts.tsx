"use client";

import Link from "next/link";
import { AlertTriangle, FileText, FolderOpen } from "lucide-react";

import { SuspiciousActivityReport } from "@/types";
import {
  deadlinePhrase,
  filingDeadlineTitle,
  filingHref,
  filingStatusLabel,
  isNarrativeHeading,
} from "@/lib/suspiciousActivityReports";
import { shortId } from "@/lib/resources";
import { cn } from "@/lib/utils";
import { LocalTime } from "@/components/LocalTime";
import { StatusBadge } from "@/components/StatusBadge";

import { useLocalization, useTranslation } from "@/context/LocalizationContext";

export function FilingStatusBadge({ status }: { status: string }) {
  return <StatusBadge status={status} label={filingStatusLabel(status)} />;
}

export function FilingDeadline({
  report,
  compact = false,
  className,
}: {
  report: Pick<SuspiciousActivityReport, "dueAt" | "isOverdue" | "status">;
  compact?: boolean;
  className?: string;
}) {
  const t = useTranslation();
  const { locale, timeZone } = useLocalization();
  const phrase = deadlinePhrase(report);

  return (
    <span
      className={cn("inline-flex flex-wrap items-center gap-x-2 gap-y-1", className)}
      title={filingDeadlineTitle(report.dueAt, locale, timeZone)}
    >
      {phrase.overdue && (
        <span className="inline-flex items-center gap-1 rounded-md border border-critical-solid bg-critical-solid px-2 py-0.5 font-mono text-[11px] font-medium uppercase tracking-wide text-critical-solid-foreground">
          <AlertTriangle className="size-3.5 flex-shrink-0" />
          {t("suspiciousActivityReports.overdue")}
        </span>
      )}
      {!compact && (
        <LocalTime
          value={report.dueAt}
          dateOnly
          className="font-mono text-xs text-slate-500"
        />
      )}
      <span
        className={cn(
          "font-mono text-xs",
          phrase.overdue
            ? "font-medium text-critical-700"
            : phrase.stopped
              ? "text-slate-400"
              : "text-slate-500"
        )}
      >
        {compact ? phrase.short : phrase.text}
      </span>
    </span>
  );
}

export const overdueRowClasses = (isOverdue: boolean): string =>
  isOverdue
    ? "bg-critical-50/60 hover:bg-critical-50 [&>td:first-child]:border-l-2 [&>td:first-child]:border-l-critical-500"
    : "hover:bg-slate-50/50";

export function FilingReference({
  report,
  className,
}: {
  report: Pick<
    SuspiciousActivityReport,
    "suspiciousActivityReportId" | "friendlyName"
  >;
  className?: string;
}) {
  return (
    <Link
      href={filingHref(report.suspiciousActivityReportId)}
      className={cn(
        "inline-flex items-center gap-1.5 font-mono text-sm text-brand-800 hover:underline",
        className
      )}
    >
      <FileText className="size-3.5 flex-shrink-0 text-slate-400" />
      {report.friendlyName}
    </Link>
  );
}

export function FilingCaseLink({
  caseId,
  className,
}: {
  caseId: string;
  className?: string;
}) {
  return (
    <Link
      href={`/cases/${caseId}`}
      className={cn(
        "inline-flex items-center gap-1.5 font-mono text-xs text-brand-800 hover:underline",
        className
      )}
    >
      <FolderOpen className="size-3.5 flex-shrink-0 text-slate-400" />
      {shortId(caseId)}
    </Link>
  );
}

export function NarrativeText({
  narrative,
  className,
}: {
  narrative: string;
  className?: string;
}) {
  const lines = narrative.split("\n");

  return (
    <pre
      className={cn(
        "overflow-x-auto whitespace-pre-wrap break-words rounded-lg border border-slate-200 bg-slate-50 p-4 font-mono text-[13px] leading-relaxed text-slate-700",
        className
      )}
    >
      {lines.map((line, index) => (
        <span
          key={index}
          className={cn(
            "block",
            isNarrativeHeading(line) &&
              "mt-3 font-medium uppercase tracking-widest text-slate-900 first:mt-0"
          )}
        >
          {/* A zero-width space keeps an empty line's height without adding a
              character to text that is copied out of here and filed. */}
          {line === "" ? "​" : line}
        </span>
      ))}
    </pre>
  );
}

export function NarrativeExcerpt({
  narrative,
  lines = 8,
  className,
}: {
  narrative: string;
  lines?: number;
  className?: string;
}) {
  const all = narrative.split("\n");
  const shown = all.slice(0, lines);
  const hidden = all.length - shown.length;

  return (
    <div className={cn("space-y-1.5", className)}>
      <NarrativeText narrative={shown.join("\n")} />
      {hidden > 0 && (
        <p className="font-mono text-xs uppercase tracking-widest text-slate-400">
          {hidden} more {hidden === 1 ? "line" : "lines"} — open the filing to
          read all of it
        </p>
      )}
    </div>
  );
}
