import {
  CaseGroupingScope,
  CaseGroupingSeverityPolicy,
  PartyRole
} from "@/types";

export const DEFAULT_CASE_GROUPING_ENABLED = false;
export const DEFAULT_DEDUPLICATION_ENABLED = true;
export const DEFAULT_CASE_GROUPING_PERIOD_MINUTES = 1440;
export const DEFAULT_DEDUPLICATION_WINDOW_MINUTES = 60;
export const DEFAULT_CASE_GROUPING_SCOPE: CaseGroupingScope = "Party";
export const DEFAULT_CASE_GROUPING_SEVERITY_POLICY: CaseGroupingSeverityPolicy =
  "Any";
export const DEFAULT_MAX_ALERTS_PER_CASE: number | null = null;
export const DEFAULT_ALERT_SUBJECT_ROLE: PartyRole = "Remitter";

export const CASE_GROUPING_SCOPES: CaseGroupingScope[] = [
  "Party",
  "PartyAndRule"
];

export const CASE_GROUPING_SEVERITY_POLICIES: CaseGroupingSeverityPolicy[] = [
  "Any",
  "AtLeastAsSevere",
  "SameSeverity"
];

const SCOPE_LABEL: Record<CaseGroupingScope, string> = {
  Party: "One case per party",
  PartyAndRule: "One case per party per rule"
};

export const caseGroupingScopeLabel = (scope: string): string =>
  SCOPE_LABEL[scope as CaseGroupingScope] ?? scope;

const SCOPE_TEXT: Record<CaseGroupingScope, string> = {
  Party:
    "Everything about a party lands together — a sanctions hit and a velocity breach on the same party share one case, which is what an analyst investigating that party wants in front of them.",
  PartyAndRule:
    "Each rule keeps its own case for a party, so different kinds of finding stay apart. It costs more cases, and the same party can have several open at once."
};

export const caseGroupingScopeText = (scope: string): string | null =>
  SCOPE_TEXT[scope as CaseGroupingScope] ?? null;

const SEVERITY_POLICY_LABEL: Record<CaseGroupingSeverityPolicy, string> = {
  Any: "Group regardless of severity",
  AtLeastAsSevere: "Only join an equally or more serious case",
  SameSeverity: "Only join a case of exactly the same severity"
};

export const caseGroupingSeverityPolicyLabel = (policy: string): string =>
  SEVERITY_POLICY_LABEL[policy as CaseGroupingSeverityPolicy] ?? policy;

const SEVERITY_POLICY_TEXT: Record<CaseGroupingSeverityPolicy, string> = {
  Any: "Any finding joins the open case. A Critical finding can end up inside a case somebody opened over an Info finding, and it will be triaged at that case's severity.",
  AtLeastAsSevere:
    "A finding only joins a case already at least as serious as itself. A lesser finding still joins a serious case, and a serious finding opens its own — so nothing is triaged below its own severity.",
  SameSeverity:
    "A finding only joins a case at exactly its own severity. The strictest of the three, and the one that produces the most cases."
};

export const caseGroupingSeverityPolicyText = (policy: string): string | null =>
  SEVERITY_POLICY_TEXT[policy as CaseGroupingSeverityPolicy] ?? null;

export const describeMinutes = (minutes: number): string => {
  if (!Number.isFinite(minutes)) return "—";

  if (minutes < 60) {
    return `${minutes} ${minutes === 1 ? "minute" : "minutes"}`;
  }

  if (minutes >= 2880 && minutes % 1440 === 0) {
    return `${minutes / 1440} days`;
  }

  const hours = Math.floor(minutes / 60);
  const remainder = minutes % 60;
  const hourPart = `${hours} ${hours === 1 ? "hour" : "hours"}`;
  if (remainder === 0) return hourPart;
  return `${hourPart} ${remainder} ${remainder === 1 ? "minute" : "minutes"}`;
};
