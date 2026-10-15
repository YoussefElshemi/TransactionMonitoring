import { EscalationRule, RuleBodyDto, RuleSeverity } from "@/types";
import { hasVelocityCondition } from "@/lib/queues";

export const PRIORITY_MIN = 0;
export const PRIORITY_MAX = 1000;

export const PRIORITY_DEFAULT = 1000;

export const isValidPriority = (priority: number): boolean =>
  Number.isInteger(priority) &&
  priority >= PRIORITY_MIN &&
  priority <= PRIORITY_MAX;

export const PRIORITY_HINT =
  "0–1000, lower runs first. The first rule that matches escalates the case and no other rule is tried, so this is the order rather than an importance.";

export const CONDITION_MINUTES_MIN = 1;
export const CONDITION_MINUTES_MAX = 525_600;

export const isValidConditionMinutes = (minutes: number): boolean =>
  Number.isInteger(minutes) &&
  minutes >= CONDITION_MINUTES_MIN &&
  minutes <= CONDITION_MINUTES_MAX;

export const CONDITION_HINTS = {
  unassignedForMinutes:
    "The case has had nobody assigned to it for at least this long.",
  holdExpiryWithinMinutes:
    "The soonest hold under this case runs out within this long. The provider's deadline approaching, not the desk's.",
  minimumSeverity:
    "The case is at this band or above. Inclusive and upward — High also takes Critical.",
  matcher:
    "Anything askable of the transaction payload, in the language rules are written in. An amount band goes here, as a greater-than on the amount field. Party facts are re-read at sweep time rather than replayed, so \"still open, touching a party we have since scored above 80\" works.",
} as const;

export const CONDITIONS_NOTE =
  "All four are optional and a rule that names several fires only when every one of them holds. At least one is required: a rule with no conditions would escalate every open case in the organisation on the next sweep.";

export const hasAnyCondition = (subject: {
  unassignedForMinutes: number | null;
  holdExpiryWithinMinutes: number | null;
  minimumSeverity: string | null;
  matcher: RuleBodyDto | null;
}): boolean =>
  subject.unassignedForMinutes !== null ||
  subject.holdExpiryWithinMinutes !== null ||
  subject.minimumSeverity !== null ||
  subject.matcher !== null;

export const NO_CONDITION_ERROR =
  "An escalation rule must declare at least one condition";

export const NO_CONDITION_DESCRIPTION =
  "Without one it would escalate every open case in the organisation on the next sweep.";

export const VELOCITY_REFUSED =
  "An escalation matcher cannot ask about velocity";

export const VELOCITY_REFUSED_DESCRIPTION =
  "The sweep runs on a timer with no transaction in hand to draw a window from, so the window would resolve to nothing and this rule would never fire.";

export const ACTION_HINTS = {
  targetQueueId:
    "Moves the case to this queue. A move to the queue it is already on is not recorded.",
  raiseSeverityTo:
    "Raises the case to this band. It only ever raises — a rule pointing at a band below the case's own leaves the severity alone, so a downgrade cannot be written this way.",
} as const;

export const ACTIONS_NOTE =
  "Neither action changes the case's status, and neither moves its deadline: a case raised to Critical keeps the deadline it was given at its old severity. Escalation stamps the case; an analyst is still the only one who moves it into the Escalated status.";

export const SWEEP_NOTE =
  "Rules are applied continuously in the background, about once a minute. There is no way to run them on demand: a rule saved now starts stamping cases within the minute.";

export interface EscalationRuleDraft {
  name: string;
  description: string;
  priority: number;
  isEnabled: boolean;
  unassignedForMinutes: number | null;
  holdExpiryWithinMinutes: number | null;
  minimumSeverity: RuleSeverity | null;
  matcher: RuleBodyDto | null;
  targetQueueId: string | null;
  raiseSeverityTo: RuleSeverity | null;
}

export const draftFrom = (rule: EscalationRule): EscalationRuleDraft => ({
  name: rule.name,
  description: rule.description ?? "",
  priority: rule.priority,
  isEnabled: rule.isEnabled,
  unassignedForMinutes: rule.unassignedForMinutes,
  holdExpiryWithinMinutes: rule.holdExpiryWithinMinutes,
  minimumSeverity: rule.minimumSeverity,
  matcher: rule.matcher,
  targetQueueId: rule.targetQueueId,
  raiseSeverityTo: rule.raiseSeverityTo,
});

export const wouldHaveAnyCondition = (draft: EscalationRuleDraft): boolean =>
  hasAnyCondition(draft);

export const escalationRulePatchBody = (
  draft: EscalationRuleDraft,
  original: EscalationRule
): Record<string, unknown> => {
  const body: Record<string, unknown> = {};

  const description = draft.description.trim();
  const originalDescription = original.description ?? "";

  if (draft.name.trim() !== original.name) body.name = draft.name.trim();
  if (description !== originalDescription) body.description = description;
  if (draft.priority !== original.priority) body.priority = draft.priority;
  if (draft.isEnabled !== original.isEnabled) body.isEnabled = draft.isEnabled;

  if (draft.unassignedForMinutes !== original.unassignedForMinutes) {
    if (draft.unassignedForMinutes === null) body.clearUnassignedFor = true;
    else body.unassignedForMinutes = draft.unassignedForMinutes;
  }

  if (draft.holdExpiryWithinMinutes !== original.holdExpiryWithinMinutes) {
    if (draft.holdExpiryWithinMinutes === null) {
      body.clearHoldExpiryWithin = true;
    } else {
      body.holdExpiryWithinMinutes = draft.holdExpiryWithinMinutes;
    }
  }

  if (draft.minimumSeverity !== original.minimumSeverity) {
    if (draft.minimumSeverity === null) body.clearMinimumSeverity = true;
    else body.minimumSeverity = draft.minimumSeverity;
  }

  if (!sameMatcher(draft.matcher, original.matcher)) {
    if (draft.matcher === null) body.clearMatcher = true;
    else body.matcher = draft.matcher;
  }

  if (draft.targetQueueId !== original.targetQueueId) {
    if (draft.targetQueueId === null) body.clearTargetQueueId = true;
    else body.targetQueueId = draft.targetQueueId;
  }

  if (draft.raiseSeverityTo !== original.raiseSeverityTo) {
    if (draft.raiseSeverityTo === null) body.clearRaiseSeverityTo = true;
    else body.raiseSeverityTo = draft.raiseSeverityTo;
  }

  return body;
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

export const isDirty = (
  draft: EscalationRuleDraft,
  original: EscalationRule
): boolean => Object.keys(escalationRulePatchBody(draft, original)).length > 0;

export interface DraftProblem {
  message: string;
  description?: string;
}

export const draftProblem = (
  draft: EscalationRuleDraft
): DraftProblem | null => {
  if (draft.name.trim().length === 0) {
    return { message: "An escalation rule needs a name" };
  }

  if (!isValidPriority(draft.priority)) {
    return {
      message: "Priority must be a whole number between 0 and 1000",
      description: "Lower runs first, and the first rule that matches wins.",
    };
  }

  if (!wouldHaveAnyCondition(draft)) {
    return {
      message: NO_CONDITION_ERROR,
      description: NO_CONDITION_DESCRIPTION,
    };
  }

  if (hasVelocityCondition(draft.matcher)) {
    return {
      message: VELOCITY_REFUSED,
      description: VELOCITY_REFUSED_DESCRIPTION,
    };
  }

  return null;
};

export interface ConditionChip {
  key: "unassigned" | "holdExpiry" | "severity" | "matcher";
  label: string;
}

export const conditionChips = (subject: {
  unassignedForMinutes: number | null;
  holdExpiryWithinMinutes: number | null;
  minimumSeverity: string | null;
  matcher: RuleBodyDto | null;
}): ConditionChip[] => {
  const chips: ConditionChip[] = [];

  if (subject.unassignedForMinutes !== null) {
    chips.push({
      key: "unassigned",
      label: `unassigned ${minutesLabel(subject.unassignedForMinutes)}`,
    });
  }

  if (subject.holdExpiryWithinMinutes !== null) {
    chips.push({
      key: "holdExpiry",
      label: `hold expires within ${minutesLabel(
        subject.holdExpiryWithinMinutes
      )}`,
    });
  }

  if (subject.minimumSeverity) {
    chips.push({
      key: "severity",
      label: `${subject.minimumSeverity} and above`,
    });
  }

  if (subject.matcher) {
    chips.push({ key: "matcher", label: "matcher" });
  }

  return chips;
};

export interface ActionChip {
  key: "queue" | "severity";
  label: string;
}

export const actionChips = (
  subject: { targetQueueId: string | null; raiseSeverityTo: string | null },
  queueName?: (queueId: string) => string | null
): ActionChip[] => {
  const chips: ActionChip[] = [];

  if (subject.targetQueueId) {
    const name = queueName?.(subject.targetQueueId);
    chips.push({
      key: "queue",
      label: `move to ${name ?? "another queue"}`,
    });
  }

  if (subject.raiseSeverityTo) {
    chips.push({
      key: "severity",
      label: `raise to ${subject.raiseSeverityTo}`,
    });
  }

  return chips;
};

export const minutesLabel = (minutes: number): string => {
  if (minutes % 1440 === 0) {
    const days = minutes / 1440;
    return `${days} day${days === 1 ? "" : "s"}`;
  }
  if (minutes % 60 === 0) {
    const hours = minutes / 60;
    return `${hours} hour${hours === 1 ? "" : "s"}`;
  }
  if (minutes > 60) {
    return `${Math.floor(minutes / 60)}h ${minutes % 60}m`;
  }
  return `${minutes} minute${minutes === 1 ? "" : "s"}`;
};
