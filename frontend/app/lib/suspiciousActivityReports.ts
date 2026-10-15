import {
  SuspiciousActivityReport,
  SuspiciousActivityReportStatus,
} from "@/types";
import { durationLabel } from "@/lib/ageing";

import { formatInstant } from "@/lib/format";

const STATUS_LABELS: Record<string, string> = {
  Draft: "Draft",
  PendingApproval: "Pending approval",
  Approved: "Approved",
  Filed: "Filed",
  Acknowledged: "Acknowledged",
  NotFiled: "Not filed",
};

export const filingStatusLabel = (status: string): string =>
  STATUS_LABELS[status] ?? status;

export const FILING_STATUS_TEXT: Record<
  SuspiciousActivityReportStatus,
  string
> = {
  Draft:
    "Being written. This is the only status in which the narrative can be edited.",
  PendingApproval:
    "A four-eyes request is open over this exact text. The report is frozen until somebody answers it — and nothing has gone to the financial intelligence unit.",
  Approved: "Signed off and ready to send. Nobody has filed it yet.",
  Filed:
    "Submitted, and the reference the unit gave back is recorded against it. The deadline clock has stopped.",
  Acknowledged:
    "The financial intelligence unit came back. Nothing more to do.",
  NotFiled:
    "Considered and deliberately not filed. This is an outcome with a reason and a name against it, not a failure — and it is a different answer from a case nobody ever looked at.",
};

export const filingStatusText = (status: string): string | null =>
  FILING_STATUS_TEXT[status as SuspiciousActivityReportStatus] ?? null;

const TERMINAL_STATUSES: SuspiciousActivityReportStatus[] = [
  "Filed",
  "Acknowledged",
  "NotFiled",
];

export const isTerminalFiling = (status: string): boolean =>
  TERMINAL_STATUSES.includes(status as SuspiciousActivityReportStatus);

export const isNarrativeEditable = (status: string): boolean =>
  status === "Draft";

export const narrativeFrozenReason = (status: string): string | null => {
  switch (status) {
    case "Draft":
      return null;
    case "PendingApproval":
      return "This has left draft: a four-eyes request is open over this exact text, so it is frozen while somebody reads it. If they turn it down it comes back to draft and you can rewrite it.";
    case "Approved":
      return "This has left draft. It is signed off as it stands, so the text is frozen — reopening it for edits would mean the sign-off was for something else.";
    case "Filed":
      return "This has left draft. It was submitted in these words, and the record has to stay as it was sent.";
    case "Acknowledged":
      return "This has left draft. It was submitted in these words, and the unit has answered on them.";
    case "NotFiled":
      return "This has left draft. It was considered and deliberately not filed, and the record of what was considered stays as it was.";
    default:
      return "This is not a draft, so the narrative is frozen.";
  }
};

export interface DeadlinePhrase {
  text: string;
  short: string;
  overdue: boolean;
  stopped: boolean;
}

export const deadlinePhrase = (
  report: Pick<SuspiciousActivityReport, "dueAt" | "isOverdue" | "status">
): DeadlinePhrase => {
  const distance = durationLabel(Date.now() - new Date(report.dueAt).getTime());

  if (isTerminalFiling(report.status)) {
    return {
      text: "deadline no longer running",
      short: "clock stopped",
      overdue: false,
      stopped: true,
    };
  }

  return report.isOverdue
    ? {
        text: `overdue by ${distance}`,
        short: `${distance} late`,
        overdue: true,
        stopped: false,
      }
    : {
        text: `due in ${distance}`,
        short: `${distance} left`,
        overdue: false,
        stopped: false,
      };
};

export const filingDeadlineTitle = (
  dueAt: string,
  locale: string,
  timeZone: string
): string =>
  `Due ${formatInstant(
    dueAt,
    locale,
    timeZone,
    "deadline"
  )} — fixed at the case's opening time, from the deadline in force then. It never moves, and changing the organisation's policy does not recalculate it.`;

export interface FilingPermissions {
  canSubmit: boolean;
  canFile: boolean;
  canAcknowledge: boolean;
  canDecline: boolean;
  roleBlockedReason: string | null;
}

const FILE_PERMISSION_ONLY =
  "Filing with the regulator, recording an acknowledgement and deciding not to file all need the suspicious-activity-reports:file permission, which is separate from being able to write one. You can open filings and write the narrative, and you can submit this one for review.";

export const filingPermissions = (
  report: Pick<SuspiciousActivityReport, "status">,
  {
    canWrite,
    canFile,
  }: {
    canWrite: boolean;
    canFile: boolean;
  }
): FilingPermissions => {
  const manager = canFile;
  const { status } = report;

  return {
    canSubmit: canWrite && status === "Draft",
    canFile: manager && status === "Approved",
    canAcknowledge: manager && status === "Filed",
    canDecline: manager && (status === "Draft" || status === "Approved"),
    roleBlockedReason:
      !manager &&
      (status === "Draft" || status === "Approved" || status === "Filed")
        ? FILE_PERMISSION_ONLY
        : null,
  };
};

export const ANY_FILING_STATUS = "All";

export interface FilingQuery {
  pageNumber: number;
  pageSize: number;
  status: string;
  overdueOnly: boolean;
  fromDate: string;
  toDate: string;
}

export const filingParams = (query: FilingQuery): URLSearchParams => {
  const params = new URLSearchParams({
    pageNumber: String(query.pageNumber),
    pageSize: String(query.pageSize),
  });
  if (query.status !== ANY_FILING_STATUS) params.set("status", query.status);
  if (query.overdueOnly) params.set("isOverdue", "true");
  if (query.fromDate) params.set("fromDate", query.fromDate);
  if (query.toDate) params.set("toDate", query.toDate);
  return params;
};

export const filingSearch = (query: FilingQuery): string =>
  filingParams(query).toString();

export const FILINGS_HREF = "/suspicious-activity-reports";

export const filingHref = (suspiciousActivityReportId: string): string =>
  `${FILINGS_HREF}/${suspiciousActivityReportId}`;

export const OVERDUE_FILINGS_HREF = `${FILINGS_HREF}?status=${ANY_FILING_STATUS}&isOverdue=true`;

export const filingExportPath = (suspiciousActivityReportId: string): string =>
  `/suspicious-activity-reports/${suspiciousActivityReportId}/export`;

export const filingExportFileName = (
  report: Pick<SuspiciousActivityReport, "friendlyName">
): string => `${report.friendlyName}.xml`;

export const NARRATIVE_SECTIONS = [
  "SUMMARY",
  "PAYMENT",
  "WHY IT WAS FLAGGED",
  "PARTIES",
  "INVESTIGATION",
];

export const isNarrativeHeading = (line: string): boolean =>
  NARRATIVE_SECTIONS.includes(line.trim());

export const COMPOSED_NARRATIVE_NOTE =
  "Composed from the case. It is a starting point and not a finished report — read every line, and rewrite whatever the case does not actually support, before you submit it.";
