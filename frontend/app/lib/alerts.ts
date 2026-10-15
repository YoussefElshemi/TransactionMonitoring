import { Alert, AlertStatus, AlertSuppressionReason, RuleSeverity } from "@/types";
import { severityRank } from "@/lib/severity";

const STATUS_TEXT: Record<AlertStatus, string> = {
  Open: "Outstanding — this is on somebody's desk",
  Suppressed:
    "The payment was still controlled; the finding folded into work already open",
  Closed: "Answered — the case holding this alert was resolved",
};

export const alertStatusText = (status: string): string | null =>
  STATUS_TEXT[status as AlertStatus] ?? null;

const STATUS_SHORT: Record<AlertStatus, string> = {
  Open: "on a desk",
  Suppressed: "folded into open work",
  Closed: "answered",
};

export const alertStatusShort = (status: string): string | null =>
  STATUS_SHORT[status as AlertStatus] ?? null;

const SUPPRESSION_REASON_TEXT: Record<AlertSuppressionReason, string> = {
  DuplicateInWindow:
    "The same rule had already raised an alert about this party, recently enough that this one is the same finding rather than a new one.",
};

export const suppressionReasonText = (reason: string): string =>
  SUPPRESSION_REASON_TEXT[reason as AlertSuppressionReason] ??
  `Suppressed under the reason \`${reason}\`.`;

const SUPPRESSION_REASON_LABEL: Record<AlertSuppressionReason, string> = {
  DuplicateInWindow: "repeat within the window",
};

export const suppressionReasonLabel = (reason: string): string =>
  SUPPRESSION_REASON_LABEL[reason as AlertSuppressionReason] ?? reason;

export const SUPPRESSION_ASSURANCE =
  "Suppressing an alert never releases a payment. This finding's own decision held or blocked its transaction exactly as the rule required — what folded was the second entry on an analyst's queue, not the control on the money.";

export const bySeverityDescending = <T>(
  items: readonly T[],
  severityOf: (item: T) => string
): T[] =>
  [...items].sort(
    (left, right) =>
      severityRank(severityOf(right)) - severityRank(severityOf(left))
  );

export const worstAlertSeverity = (
  alerts: readonly { severity: RuleSeverity | string }[]
): string | null => {
  if (alerts.length === 0) return null;

  return alerts.reduce((worst, alert) =>
    severityRank(alert.severity) > severityRank(worst.severity) ? alert : worst
  ).severity;
};

export const openAlerts = <T extends { status: AlertStatus | string }>(
  alerts: readonly T[]
): T[] => alerts.filter((alert) => alert.status === "Open");

export const hasSubject = (alert: Alert): boolean => alert.partyId !== null;
