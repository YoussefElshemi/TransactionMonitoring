import type { SandboxCreditResponse } from "@/types";

import { currentReaderPreferences, formatInstant } from "@/lib/format";

export const SANDBOX_EVALUATION_LIMIT_PER_MINUTE = 120;
export const MAX_SIMULATION_PAYMENT_COUNT = 5_000;
export const SANDBOX_RESET_CONFIRMATION = "RESET";

export const isSandboxExhausted = (
  credit: Pick<SandboxCreditResponse, "remaining"> | null
): boolean => credit?.remaining === 0;

export const sandboxCreditPercentRemaining = (
  credit: Pick<SandboxCreditResponse, "allowance" | "remaining"> | null
): number => {
  if (!credit || credit.allowance <= 0) return 0;
  return Math.max(0, Math.min(100, (credit.remaining / credit.allowance) * 100));
};

export const sandboxRateLimitDescription = (retryMs: number | null): string => {
  const prefix = `The shared ${SANDBOX_EVALUATION_LIMIT_PER_MINUTE} requests/minute limit is reached.`;
  if (retryMs === null) return `${prefix} Try again shortly.`;

  const seconds = Math.max(1, Math.ceil(retryMs / 1000));
  return `${prefix} Try again in ${seconds} second${seconds === 1 ? "" : "s"}.`;
};

export const isSandboxResetConfirmation = (value: string): boolean =>
  value === SANDBOX_RESET_CONFIRMATION;

export const sandboxResetDescription = (organizationName: string): string =>
  `This permanently deletes ${organizationName}'s Test traffic and Test configuration. Test API keys are kept, so nothing needs reissuing. Monthly credit usage is not reset. Live data and Live credentials are not changed.`;

const SANDBOX_CREDIT_FREE: readonly RegExp[] = [
  /^\/organizations\/[^/]+\/sandbox\/reset$/i,
  /^\/auth(\/|$)/i,
  /^\/oauth\/token$/i,
];

export const spendsSandboxCredit = (method: string, path: string): boolean => {
  const verb = method.toUpperCase();
  if (verb === "GET" || verb === "HEAD" || verb === "OPTIONS") return false;

  return !SANDBOX_CREDIT_FREE.some((pattern) => pattern.test(path));
};

export interface SandboxCreditProblem {
  allowance: number | null;
  used: number | null;
  remaining: number | null;
  requested: number | null;
  resetsAt: string | null;
  mode: string | null;
}

const numberOrNull = (value: unknown): number | null =>
  typeof value === "number" && Number.isFinite(value) ? value : null;

const stringOrNull = (value: unknown): string | null =>
  typeof value === "string" && value.length > 0 ? value : null;

export const readSandboxCreditProblem = (
  body: unknown
): SandboxCreditProblem | null => {
  if (!body || typeof body !== "object") return null;

  const problem = body as Record<string, unknown>;
  const credit: SandboxCreditProblem = {
    allowance: numberOrNull(problem.allowance),
    used: numberOrNull(problem.used),
    remaining: numberOrNull(problem.remaining),
    requested: numberOrNull(problem.requested),
    resetsAt: stringOrNull(problem.resetsAt),
    mode: stringOrNull(problem.mode),
  };

  const named =
    credit.allowance !== null ||
    credit.remaining !== null ||
    credit.used !== null ||
    credit.resetsAt !== null;

  return named ? credit : null;
};

export const sandboxExhaustedDescription = (
  credit: SandboxCreditProblem | null,
  formatDate: (iso: string) => string = (iso) => {
    const { locale, timeZone } = currentReaderPreferences();
    return formatInstant(iso, locale, timeZone, "dateTime");
  }
): string => {
  const parts: string[] = ["Nothing was changed."];

  if (credit?.remaining !== null && credit?.remaining !== undefined) {
    parts.push(
      credit.allowance !== null
        ? `${credit.remaining} of ${credit.allowance} Test credits remain.`
        : `${credit.remaining} Test credits remain.`
    );
  }

  const resets = credit?.resetsAt ? safeFormat(credit.resetsAt, formatDate) : null;
  parts.push(
    resets
      ? `Retrying won't help until the allowance resets on ${resets}, or an admin raises it.`
      : "Retrying won't help until the allowance resets or an admin raises it."
  );
  parts.push("Resetting the Sandbox still works.");

  return parts.join(" ");
};

const safeFormat = (
  iso: string,
  formatDate: (iso: string) => string
): string | null => {
  if (Number.isNaN(Date.parse(iso))) return null;

  try {
    return formatDate(iso);
  } catch {
    return null;
  }
};

const creditListeners = new Set<() => void>();

export const subscribeToSandboxCredit = (
  listener: () => void
): (() => void) => {
  creditListeners.add(listener);
  return () => creditListeners.delete(listener);
};

export const notifySandboxCreditSpent = (): void => {
  creditListeners.forEach((listener) => listener());
};
