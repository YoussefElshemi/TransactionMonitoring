import prettyMilliseconds from "pretty-ms";

import {
  Queue,
  QueueMemberLoad,
  QueueStatistic,
  RoutingPolicy,
  RuleBodyDto,
  RuleSeverity,
} from "@/types";

export const PRIORITY_MIN = 0;
export const PRIORITY_MAX = 1000;

export const PRIORITY_DEFAULT = 1000;

export const isValidPriority = (priority: number): boolean =>
  Number.isInteger(priority) &&
  priority >= PRIORITY_MIN &&
  priority <= PRIORITY_MAX;

const ROUTING_POLICY_LABELS: Record<string, string> = {
  Manual: "Manual",
  RoundRobin: "Round robin",
  LeastLoaded: "Least loaded",
};

export const routingPolicyLabel = (policy: string): string =>
  ROUTING_POLICY_LABELS[policy] ?? policy;

export const ROUTING_POLICY_HINTS: Record<RoutingPolicy, string> = {
  Manual:
    "Nobody is handed the case. It waits on the queue until a supervisor assigns it or an analyst claims it.",
  RoundRobin:
    "Handed to whoever on this queue has gone longest without work from it. Someone who has never been handed one goes first.",
  LeastLoaded:
    "Handed to whichever member holds the fewest open cases across the organisation — not just the fewest from this queue.",
};

export const assignsOnArrival = (policy: string): boolean =>
  policy === "RoundRobin" || policy === "LeastLoaded";

export const hasIdleRouting = (queue: {
  routingPolicy: string;
  isEnabled: boolean;
  members: { userId: string }[];
}): boolean =>
  queue.isEnabled &&
  assignsOnArrival(queue.routingPolicy) &&
  queue.members.length === 0;

export const hasIdleRoutingFromStatistic = (statistic: QueueStatistic): boolean =>
  statistic.isEnabled &&
  assignsOnArrival(statistic.routingPolicy) &&
  statistic.memberLoads.length === 0;

export const hasVelocityCondition = (body: RuleBodyDto | null): boolean => {
  if (!body) return false;
  if (body.velocity) return true;
  // An expression can carry a window as an *operand* — `sum(velocity…)` — which
  // is just as unresolvable at routing time as a bare velocity leaf.
  if (expressionHasVelocity(body.expression)) return true;
  return [...(body.and ?? []), ...(body.or ?? []), ...(body.not ?? [])].some(
    hasVelocityCondition
  );
};

const expressionHasVelocity = (
  expression: RuleBodyDto["expression"]
): boolean => {
  if (!expression) return false;
  if (expression.velocity) return true;
  return (expression.arguments ?? []).some(expressionHasVelocity);
};

export const isCatchAll = (queue: {
  minimumSeverity: string | null;
  ruleIds: string[];
  matcher: RuleBodyDto | null;
}): boolean =>
  queue.minimumSeverity === null &&
  queue.ruleIds.length === 0 &&
  queue.matcher === null;

export interface CriterionChip {
  key: "severity" | "rules" | "matcher";
  label: string;
}

export const criteriaChips = (queue: {
  minimumSeverity: string | null;
  ruleIds: string[];
  matcher: RuleBodyDto | null;
}): CriterionChip[] => {
  const chips: CriterionChip[] = [];

  if (queue.minimumSeverity) {
    chips.push({
      key: "severity",
      label: `${queue.minimumSeverity} and above`,
    });
  }

  if (queue.ruleIds.length > 0) {
    chips.push({
      key: "rules",
      label: `any of ${queue.ruleIds.length} rule${
        queue.ruleIds.length === 1 ? "" : "s"
      }`,
    });
  }

  if (queue.matcher) {
    chips.push({ key: "matcher", label: "matcher" });
  }

  return chips;
};

export const oldestWaitingLabel = (seconds: number | null): string => {
  if (seconds === null) return "—";
  return prettyMilliseconds(seconds * 1000, {
    unitCount: 2,
    secondsDecimalDigits: 0,
  });
};

export const depthShare = (statistic: QueueStatistic): number => {
  const open = statistic.depth + statistic.inProgressCount;
  return open === 0 ? 0 : statistic.depth / open;
};

export const byRotation = (a: QueueMemberLoad, b: QueueMemberLoad): number => {
  if (a.lastAssignedAt === null && b.lastAssignedAt === null) return 0;
  if (a.lastAssignedAt === null) return -1;
  if (b.lastAssignedAt === null) return 1;
  return (
    new Date(a.lastAssignedAt).getTime() - new Date(b.lastAssignedAt).getTime()
  );
};

export const byLoad = (a: QueueMemberLoad, b: QueueMemberLoad): number =>
  a.openCaseCount - b.openCaseCount;

export const nextUpComparator = (
  policy: string
): ((a: QueueMemberLoad, b: QueueMemberLoad) => number) | null => {
  if (policy === "RoundRobin") return byRotation;
  if (policy === "LeastLoaded") return byLoad;
  return null;
};

export interface QueueDraft {
  name: string;
  description: string;
  priority: number;
  routingPolicy: RoutingPolicy;
  minimumSeverity: RuleSeverity | null;
  ruleIds: string[];
  matcher: RuleBodyDto | null;
  isEnabled: boolean;
  subOrganizationOwner?: string | null;
}

export const draftFrom = (queue: Queue): QueueDraft => ({
  name: queue.name,
  description: queue.description ?? "",
  priority: queue.priority,
  routingPolicy: (queue.routingPolicy as RoutingPolicy) ?? "Manual",
  minimumSeverity: queue.minimumSeverity,
  ruleIds: [...queue.ruleIds],
  matcher: queue.matcher,
  isEnabled: queue.isEnabled,
});

export const queuePatchBody = (
  draft: QueueDraft,
  original: Queue
): Record<string, unknown> => {
  const body: Record<string, unknown> = {};

  const description = draft.description.trim();
  const originalDescription = original.description ?? "";

  if (draft.name.trim() !== original.name) body.name = draft.name.trim();
  if (description !== originalDescription) body.description = description;
  if (draft.priority !== original.priority) body.priority = draft.priority;
  if (draft.routingPolicy !== original.routingPolicy) {
    body.routingPolicy = draft.routingPolicy;
  }
  if (draft.isEnabled !== original.isEnabled) body.isEnabled = draft.isEnabled;

  if (draft.minimumSeverity !== original.minimumSeverity) {
    if (draft.minimumSeverity === null) body.clearMinimumSeverity = true;
    else body.minimumSeverity = draft.minimumSeverity;
  }

  if (!sameIds(draft.ruleIds, original.ruleIds)) {
    if (draft.ruleIds.length === 0) body.clearRuleIds = true;
    else body.ruleIds = draft.ruleIds;
  }

  if (!sameMatcher(draft.matcher, original.matcher)) {
    if (draft.matcher === null) body.clearMatcher = true;
    else body.matcher = draft.matcher;
  }

  if (draft.subOrganizationOwner !== undefined) {
    if (draft.subOrganizationOwner === null) body.clearSubOrganization = true;
    else body.subOrganizationId = draft.subOrganizationOwner;
  }

  return body;
};

const sameIds = (left: string[], right: string[]): boolean => {
  if (left.length !== right.length) return false;
  const seen = new Set(right);
  return left.every((id) => seen.has(id));
};

const sameMatcher = (
  left: RuleBodyDto | null,
  right: RuleBodyDto | null
): boolean => {
  if (left === null || right === null) return left === right;
  return stableJson(left) === stableJson(right);
};

const stableJson = (value: unknown): string =>
  JSON.stringify(value, (_key, nested) => {
    if (nested === null || typeof nested !== "object" || Array.isArray(nested)) {
      return nested;
    }
    return Object.fromEntries(
      Object.entries(nested as Record<string, unknown>)
        // An absent key and an explicitly null one mean the same thing on a
        // rule body, and the builder is not consistent about which it emits.
        .filter(([, entry]) => entry !== null && entry !== undefined)
        .sort(([a], [b]) => a.localeCompare(b))
    );
  });

export const isDirty = (draft: QueueDraft, original: Queue): boolean =>
  Object.keys(queuePatchBody(draft, original)).length > 0;
