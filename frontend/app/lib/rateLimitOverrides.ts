import { RateLimitOverride, RateLimitOverridePool } from "@/types";

export const OVERRIDE_POOLS: readonly RateLimitOverridePool[] = [
  "Read",
  "Write",
  "Evaluation",
];

export const ALL_POOLS = "All";

export const poolLabel = (pool: RateLimitOverridePool | null): string =>
  pool ?? "All pools";

export const POOL_HINTS: Record<RateLimitOverridePool, string> = {
  Read: "Every GET. Listing cases, loading a screen, polling for changes.",
  Write:
    "Every other method, except the three evaluate endpoints below. Creating and updating records.",
  Evaluation:
    "Running a rule or a rule set against a transaction. This is the pool an authorization integration sits in, so clamping it is the one a customer feels on their own checkout.",
};

export const ALL_POOLS_HINT =
  "Every pool at once — reads, writes and evaluations. The widest clamp available, and the one to reach for when a tenant is causing trouble across the board rather than in one place.";

export const REASON_MAX_LENGTH = 500;

export const REASON_HINT =
  "Required. Name the incident and what you saw — this row is the only record of why a tenant's limits were changed, and it is what somebody reads back weeks later.";

export const isValidReason = (reason: string): boolean => {
  const trimmed = reason.trim();
  return trimmed.length > 0 && trimmed.length <= REASON_MAX_LENGTH;
};

export const MAX_DURATION_DAYS = 30;

export const MAX_DURATION_HOURS = MAX_DURATION_DAYS * 24;

export interface ExpiryPreset {
  label: string;
  hours: number;
  hint?: string;
}

export const EXPIRY_PRESETS: readonly ExpiryPreset[] = [
  { label: "1 hour", hours: 1, hint: "Long enough to see whether it worked." },
  { label: "4 hours", hours: 4, hint: "An incident somebody is sitting with." },
  {
    label: "24 hours",
    hours: 24,
    hint: "Overnight, for something that needs looking at in the morning.",
  },
  {
    label: "7 days",
    hours: 24 * 7,
    hint: "A tenant being held while a conversation happens.",
  },
  {
    label: "30 days",
    hours: MAX_DURATION_HOURS,
    hint: "The longest the API accepts. Renew rather than extend — a second row is a second person having looked.",
  },
];

export const DEFAULT_EXPIRY_HOURS = 4;

export const expiresAtFrom = (hours: number, now: Date = new Date()): string =>
  new Date(now.getTime() + hours * 3_600_000).toISOString();

export const isValidExpiry = (
  expiresAt: string,
  now: Date = new Date()
): boolean => {
  const parsed = Date.parse(expiresAt);
  if (Number.isNaN(parsed)) return false;

  return (
    parsed > now.getTime() &&
    parsed <= now.getTime() + MAX_DURATION_HOURS * 3_600_000
  );
};

export const hasExpired = (
  override: RateLimitOverride,
  now: Date = new Date()
): boolean => Date.parse(override.expiresAt) <= now.getTime();

export const isValidLimit = (value: number): boolean =>
  Number.isInteger(value) && value > 0;

export const bucketSentence = (override: {
  permitLimit: number;
  tokensPerPeriod: number;
  replenishmentPeriodSeconds: number;
}): string => {
  const period =
    override.replenishmentPeriodSeconds === 60
      ? "minute"
      : `${override.replenishmentPeriodSeconds}s`;

  return `${override.permitLimit} at once, ${override.tokensPerPeriod} back per ${period}`;
};

export const effectiveOverride = (
  overrides: readonly RateLimitOverride[],
  pool: RateLimitOverridePool,
  now: Date = new Date()
): RateLimitOverride | null => {
  const live = overrides.filter((override) => !hasExpired(override, now));

  const newestFirst = (a: RateLimitOverride, b: RateLimitOverride) =>
    Date.parse(b.createdAt) - Date.parse(a.createdAt);

  const specific = live
    .filter((override) => override.pool === pool)
    .sort(newestFirst);
  if (specific.length > 0) return specific[0];

  const blanket = live
    .filter((override) => override.pool === null)
    .sort(newestFirst);

  return blanket[0] ?? null;
};

export const effectiveByPool = (
  overrides: readonly RateLimitOverride[],
  now: Date = new Date()
): Record<RateLimitOverridePool, RateLimitOverride | null> => ({
  Read: effectiveOverride(overrides, "Read", now),
  Write: effectiveOverride(overrides, "Write", now),
  Evaluation: effectiveOverride(overrides, "Evaluation", now),
});

export const isSupersededFor = (
  override: RateLimitOverride,
  overrides: readonly RateLimitOverride[],
  pool: RateLimitOverridePool,
  now: Date = new Date()
): boolean =>
  effectiveOverride(overrides, pool, now)?.rateLimitOverrideId !==
  override.rateLimitOverrideId;

export const poolsInForce = (
  override: RateLimitOverride,
  overrides: readonly RateLimitOverride[],
  now: Date = new Date()
): RateLimitOverridePool[] =>
  OVERRIDE_POOLS.filter(
    (pool) => !isSupersededFor(override, overrides, pool, now)
  );

export const APPLY_DELAY_SECONDS = 15;

export const APPLIED_SHORTLY = `The limiter reads its configuration from a snapshot refreshed every ${APPLY_DELAY_SECONDS} seconds, so this takes up to that long to bite. Don't apply it again if nothing changes immediately.`;

export const SETTLE_DELAY_MS = (APPLY_DELAY_SECONDS + 2) * 1000;
