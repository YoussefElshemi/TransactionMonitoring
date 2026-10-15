import { CaseDisposition, CaseStatus } from "@/types";

const TERMINAL_STATUSES: readonly string[] = ["Approved", "Rejected", "Expired"];

export const isTerminalCase = (status: string): boolean =>
  TERMINAL_STATUSES.includes(status);

export const DESK_STATUSES = [
  "InReview",
  "Escalated",
  "PendingInformation",
  "OnHold",
] as const;

const ALLOWED_TRANSITIONS: Record<string, readonly CaseStatus[]> = {
  Assigned: ["InReview", "Escalated", "PendingInformation", "OnHold"],
  InReview: ["Escalated", "PendingInformation", "OnHold"],
  Escalated: ["InReview", "PendingInformation", "OnHold"],
  PendingInformation: ["InReview", "Escalated", "OnHold"],
  OnHold: ["InReview", "Escalated", "PendingInformation"],
};

export const allowedTransitionsFrom = (
  status: string
): readonly CaseStatus[] => ALLOWED_TRANSITIONS[status] ?? [];

export const isCaseInProgress = (subject: {
  status: string;
  assignedToUserId: string | null;
}): boolean =>
  Boolean(subject.assignedToUserId) && !isTerminalCase(subject.status);

export const CASE_STATUSES: readonly CaseStatus[] = [
  "Pending",
  "Assigned",
  "InReview",
  "Escalated",
  "PendingInformation",
  "OnHold",
  "Approved",
  "Rejected",
  "Expired",
];

const STATUS_LABELS: Record<string, string> = {
  Pending: "Pending",
  Assigned: "Assigned",
  InReview: "In review",
  Escalated: "Escalated",
  PendingInformation: "Pending information",
  OnHold: "On hold",
  Approved: "Approved",
  Rejected: "Rejected",
  Expired: "Expired",
};

export const caseStatusLabel = (status: string): string =>
  STATUS_LABELS[status] ?? status;

export const DESK_STATUS_HINTS: Record<string, string> = {
  InReview: "You are working it now.",
  Escalated: "Handed up — someone senior has to look.",
  PendingInformation: "Waiting on an answer from outside the desk.",
  OnHold: "Parked deliberately, with nothing outstanding to chase.",
};

export const PRIMARY_DISPOSITIONS: readonly CaseDisposition[] = [
  "TruePositive",
  "FalsePositive",
];

export const SECONDARY_DISPOSITIONS: readonly CaseDisposition[] = [
  "TrueNegative",
  "FalseNegative",
];

export const DISPOSITIONS: readonly CaseDisposition[] = [
  ...PRIMARY_DISPOSITIONS,
  ...SECONDARY_DISPOSITIONS,
];

const DISPOSITION_LABEL_KEYS: Record<string, string> = {
  TruePositive: "disposition.truePositive",
  FalsePositive: "disposition.falsePositive",
  TrueNegative: "disposition.trueNegative",
  FalseNegative: "disposition.falseNegative",
};

export const dispositionLabelKey = (disposition: string): string =>
  DISPOSITION_LABEL_KEYS[disposition] ?? disposition;

export const DISPOSITION_HINTS: Record<CaseDisposition, string> = {
  TruePositive: "disposition.hint.truePositive",
  FalsePositive: "disposition.hint.falsePositive",
  TrueNegative: "disposition.hint.trueNegative",
  FalseNegative: "disposition.hint.falseNegative",
};

export const requiresNote = (
  status: "Approved" | "Rejected",
  disposition: CaseDisposition | null
): boolean =>
  status === "Rejected" ||
  disposition === "TruePositive" ||
  disposition === "FalseNegative";
