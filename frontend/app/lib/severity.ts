import { RuleSeverity } from "@/types";

export const SEVERITY_ORDER: RuleSeverity[] = [
  "Info",
  "Low",
  "Medium",
  "High",
  "Critical",
];

export const severityRank = (severity: string): number =>
  SEVERITY_ORDER.indexOf(severity as RuleSeverity);

export const SEVERITY_FILTERS: RuleSeverity[] = [...SEVERITY_ORDER].reverse();
