import { DecisionReasonCode, DecisionStatus, HoldExpiryVerdict } from "@/types";

export const isTerminalDecision = (status: string): boolean =>
  status === "Released" || status === "Blocked" || status === "Expired";

const REASON_TEXT: Record<DecisionReasonCode, string> = {
  NoRuleMatched: "No rule matched this transaction",
  RuleAllowed: "Rules matched, and every one of them allowed it",
  RuleFlagged: "Flagged for reporting only — nothing was held",
  RuleReviewRequired: "A rule asked for an analyst to review it",
  RuleBlocked: "A rule blocked it outright, with no analyst involved",
  AnalystApproved: "An analyst approved the case",
  AnalystRejected: "An analyst rejected the case",
  HoldExpired: "The hold expired before anyone acted",
};

export const reasonText = (reasonCode: string): string =>
  REASON_TEXT[reasonCode as DecisionReasonCode] ?? reasonCode;

const STATUS_TEXT: Record<DecisionStatus, string> = {
  Pending: "No verdict has been reached yet",
  Held: "The transaction is held, and cannot proceed until the case is resolved",
  Released: "The transaction may proceed",
  Blocked: "The transaction must be rejected",
  Expired:
    "The hold ran out with no verdict — nobody decided, and no automatic verdict applied",
};

export const statusText = (status: string): string | null =>
  STATUS_TEXT[status as DecisionStatus] ?? null;

const HOLD_EXPIRY_VERDICT_TEXT: Record<HoldExpiryVerdict, string> = {
  Release: "Let the transaction through if nobody reviews it in time",
  Block: "Reject the transaction if nobody reviews it in time",
  Expire: "Stop holding and tell the provider nobody decided",
  Never: "Hold indefinitely — the transaction waits for a human",
};

export const holdExpiryVerdictText = (verdict: string): string | null =>
  HOLD_EXPIRY_VERDICT_TEXT[verdict as HoldExpiryVerdict] ?? null;

const HOLD_EXPIRY_VERDICT_LABEL: Record<HoldExpiryVerdict, string> = {
  Release: "Releases automatically",
  Block: "Blocks automatically",
  Expire: "Hold lapses",
  Never: "No deadline",
};

export const holdExpiryVerdictLabel = (verdict: string): string =>
  HOLD_EXPIRY_VERDICT_LABEL[verdict as HoldExpiryVerdict] ?? `Expires (${verdict})`;

export const holdExpiryOutcomeStatus = (
  verdict: string
): DecisionStatus | null => {
  switch (verdict) {
    case "Release":
      return "Released";
    case "Block":
      return "Blocked";
    case "Expire":
      return "Expired";
    default:
      return null;
  }
};

export const isSystemExpiry = (decision: {
  reasonCode: string;
  resolvedByUserId: string | null;
}): boolean =>
  decision.reasonCode === "HoldExpired" && decision.resolvedByUserId === null;

const VERDICT_RANK: Record<HoldExpiryVerdict, number> = {
  Release: 0,
  Expire: 1,
  Block: 2,
  Never: 3,
};

export const mostRestrictiveVerdict = (
  verdicts: string[]
): string | null =>
  verdicts.reduce<string | null>((worst, verdict) => {
    if (worst === null) return verdict;
    const a = VERDICT_RANK[verdict as HoldExpiryVerdict] ?? -1;
    const b = VERDICT_RANK[worst as HoldExpiryVerdict] ?? -1;
    return a > b ? verdict : worst;
  }, null);

export const DEFAULT_HOLD_WINDOW_MINUTES = 1440;
export const DEFAULT_HOLD_EXPIRY_VERDICT: HoldExpiryVerdict = "Block";

export const formatHoldWindow = (minutes: number): string => {
  const hours = Math.floor(minutes / 60);
  const remainder = minutes % 60;
  if (hours === 0) return `${remainder}m`;
  if (remainder === 0) return `${hours}h`;
  return `${hours}h ${remainder}m`;
};
