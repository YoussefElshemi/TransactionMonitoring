import {
  Party,
  PartyMappingUsage,
  PartyNodeDto,
  PartyRole,
  PartyRuleAttribute,
} from "@/types";

export const ROLE_HINTS: Record<PartyRole, string> = {
  Remitter: "Who the money came from — the sending customer or account.",
  Beneficiary: "Who the money went to. A payload may carry several.",
};

const lowerFirst = (value: string): string =>
  value ? value.charAt(0).toLowerCase() + value.slice(1) : value;

export const partyLeafName = (party: PartyNodeDto): string =>
  `party.${lowerFirst(String(party.role))}.${lowerFirst(String(party.attribute))}`;

export const PARTY_ATTRIBUTE_LABEL: Record<PartyRuleAttribute, string> = {
  RiskScore: "Risk score",
  DaysSinceFirstSeen: "Days since first seen",
  DaysSinceLastSeen: "Days since last seen",
};

export const PARTY_ATTRIBUTE_HINT: Record<PartyRuleAttribute, string> = {
  RiskScore:
    "The assessment an analyst recorded against this party. Null until somebody sets one, and a rule reading an unset score does not fire.",
  DaysSinceFirstSeen:
    "How long this party has been known to you. A party first seen on this very payment reads 0 — so a first-time-beneficiary rule fires on the payment that introduces them, which is the point.",
  DaysSinceLastSeen:
    "How long since the last payment this party stood in. 0 on a party seen earlier today.",
};

export const PARTY_ATTRIBUTE_TYPE = "Number" as const;

export const partyName = (
  party: Pick<Party, "displayName" | "identifier">
): string => party.displayName?.trim() || party.identifier;

export const paysThemselves = (roles: PartyRole[]): boolean =>
  roles.includes("Remitter") && roles.includes("Beneficiary");

export type RiskWeight = "settled" | "inflight" | "attention" | "critical";

export const riskWeight = (score: number): RiskWeight => {
  if (score >= 75) return "critical";
  if (score >= 50) return "attention";
  if (score >= 25) return "inflight";
  return "settled";
};

export const MAX_RISK_NOTE = 2000;

export const PARTY_MAPPING_IN_USE = "PartyMappingInUseException";
export const PARTY_RULE_VIOLATION = "PartyRuleViolationException";
export const PARTY_MAPPING_CONFLICT = "PartyMappingConflictException";

export const readMappingUsages = (
  body: unknown
): PartyMappingUsage[] | null => {
  const problem = body as { type?: string; usages?: unknown } | null;
  if (!problem || problem.type !== PARTY_MAPPING_IN_USE) return null;
  return Array.isArray(problem.usages)
    ? (problem.usages as PartyMappingUsage[])
    : [];
};

export const isMappingConflict = (body: unknown): boolean =>
  (body as { type?: string } | null)?.type === PARTY_MAPPING_CONFLICT;

export const describeUsage = (usage: PartyMappingUsage): string =>
  usage.roles.join(", ");
