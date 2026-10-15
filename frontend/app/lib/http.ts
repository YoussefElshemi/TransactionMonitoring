import { toast } from "sonner";

import { translateForReader as t } from "@/lib/i18n";

import { API_BASE } from "@/lib/api";
import { MODE_HEADER, effectiveModeMatches, modeRequestHeader } from "@/lib/mode";
import {
  SandboxCreditProblem,
  notifySandboxCreditSpent,
  readSandboxCreditProblem,
  sandboxExhaustedDescription,
  spendsSandboxCredit,
} from "@/lib/sandbox";
import {
  RateLimitPool,
  observeResponse,
  poolForRequest,
  readRetryAfterMs,
  sleep,
  throughGate,
  withJitter,
} from "@/lib/rateLimit";

export const IDEMPOTENCY_KEY_HEADER = "X-Idempotency-Key";
export const IDEMPOTENT_REPLAY_HEADER = "X-Idempotent-Replay";

export const apiPath = (input: string): string | null => {
  if (input.startsWith("/")) return stripQuery(input);

  const base = API_BASE.replace(/\/$/, "");
  if (input.startsWith(base)) return stripQuery(input.slice(base.length)) || "/";

  try {
    const url = new URL(input);
    const baseUrl = new URL(base);
    if (url.host === baseUrl.host) return stripQuery(url.pathname) || "/";
  } catch {
  }

  return null;
};

const stripQuery = (path: string): string => path.split(/[?#]/)[0];

const IDEMPOTENCY_EXEMPT: readonly RegExp[] = [
  /^\/auth(\/|$)/,
  /^\/oauth\/token$/,
  /^\/invitations\/accept$/,
  /^\/invoices\/[^/]+\/export$/,
  /^\/organizations\/[^/]+\/invoices\/[^/]+\/export$/,
];

export const acceptsIdempotencyKey = (method: string, path: string): boolean => {
  const verb = method.toUpperCase();
  if (verb === "GET" || verb === "HEAD" || verb === "OPTIONS") return false;

  return !IDEMPOTENCY_EXEMPT.some((pattern) => pattern.test(path));
};

export const newIdempotencyKey = (): string => {
  const source: Crypto | undefined =
    typeof crypto === "undefined" ? undefined : crypto;

  if (typeof source?.randomUUID === "function") return source.randomUUID();

  const bytes = new Uint8Array(16);
  source!.getRandomValues(bytes);
  bytes[6] = (bytes[6] & 0x0f) | 0x40;
  bytes[8] = (bytes[8] & 0x3f) | 0x80;

  const hex = Array.from(bytes, (byte) => byte.toString(16).padStart(2, "0"));

  return [
    hex.slice(0, 4).join(""),
    hex.slice(4, 6).join(""),
    hex.slice(6, 8).join(""),
    hex.slice(8, 10).join(""),
    hex.slice(10, 16).join(""),
  ].join("-");
};

export const wasReplayed = (response: Response): boolean | null => {
  const header = response.headers.get(IDEMPOTENT_REPLAY_HEADER);
  if (header === null) return null;

  return header.toLowerCase() === "true";
};

const IN_PROGRESS_TITLE = "request in progress";

const isInProgressConflict = async (response: Response): Promise<boolean> => {
  try {
    const body = (await response.clone().json()) as { title?: string } | null;
    return (body?.title ?? "").toLowerCase().includes(IN_PROGRESS_TITLE);
  } catch {
    return false;
  }
};

const BASE_BACKOFF_MS = 500;

const MAX_BACKOFF_MS = 30_000;

const backoffMs = (attempt: number): number =>
  Math.min(BASE_BACKOFF_MS * 2 ** attempt, MAX_BACKOFF_MS);

export interface ApiFetchOptions {
  idempotencyKey?: string;

  withoutIdempotencyKey?: boolean;

  maxRateLimitRetries?: number;

  maxInProgressRetries?: number;

  maxNetworkRetries?: number;

  pool?: RateLimitPool;
}

export const apiFetch = async (
  input: string,
  init: RequestInit = {},
  {
    idempotencyKey,
    withoutIdempotencyKey = false,
    maxRateLimitRetries = 4,
    maxInProgressRetries = 5,
    maxNetworkRetries = 2,
    pool: poolOverride,
  }: ApiFetchOptions = {}
): Promise<Response> => {
  const method = (init.method ?? "GET").toUpperCase();
  const path = apiPath(input);

  if (path === null) return fetch(input, init);

  const pool = poolOverride ?? poolForRequest(method, path);
  const signal = init.signal ?? undefined;

  // Chosen once, before the loop, so every retry reuses the same key.
  const key =
    withoutIdempotencyKey || !acceptsIdempotencyKey(method, path)
      ? null
      : (idempotencyKey ?? newIdempotencyKey());

  const headers = new Headers(init.headers);
  if (key !== null) headers.set(IDEMPOTENCY_KEY_HEADER, key);

  const requestedMode = modeRequestHeader();
  headers.set(MODE_HEADER, requestedMode);

  const request: RequestInit = {
    ...init,
    method: init.method,
    headers,
    credentials: init.credentials ?? "include",
  };

  const safeToRepeat = method === "GET" || method === "HEAD" || key !== null;

  let rateLimitRetries = 0;
  let inProgressRetries = 0;
  let networkRetries = 0;

  for (;;) {
    let response: Response;

    try {
      response = await throughGate(
        pool,
        () => fetch(`${API_BASE.replace(/\/$/, "")}${path}${queryOf(input)}`, request),
        signal
      );
    } catch (error) {
      if (isAbort(error) || signal?.aborted) throw error;

      if (safeToRepeat && networkRetries < maxNetworkRetries) {
        // The write may have committed; retrying with the same key gets a replay, not a duplicate.
        await sleep(withJitter(backoffMs(networkRetries)), signal);
        networkRetries += 1;
        continue;
      }

      throw error;
    }

    const effectiveMode = response.headers.get(MODE_HEADER)?.toLowerCase();
    if (!effectiveModeMatches(effectiveMode ?? null, requestedMode)) {
      return new Response(
        JSON.stringify({
          title: "Environment mismatch",
          detail: `The API served ${effectiveMode} data for a ${requestedMode} request. No response data was displayed.`,
          status: 409,
        }),
        {
          status: 409,
          headers: { "Content-Type": "application/problem+json", [MODE_HEADER]: effectiveMode ?? "unknown" },
        }
      );
    }

    observeResponse(pool, response);

    if (response.status === 429) {
      if (rateLimitRetries >= maxRateLimitRetries) return response;

      const retryAfterMs = readRetryAfterMs(response);
      const waitMs =
        retryAfterMs === null
          ? withJitter(backoffMs(rateLimitRetries))
          : retryAfterMs + withJitter(backoffMs(rateLimitRetries));

      await sleep(waitMs, signal);
      rateLimitRetries += 1;
      continue;
    }

    if (response.status === 409 && key !== null) {
      if (
        inProgressRetries < maxInProgressRetries &&
        (await isInProgressConflict(response))
      ) {
        await sleep(withJitter(backoffMs(inProgressRetries)), signal);
        inProgressRetries += 1;
        continue;
      }
    }

    if (requestedMode === "test") await accountForCredit(response, method, path);

    return response;
  }
};

const SANDBOX_CREDIT_TOAST_ID = "sandbox-credits-exhausted";

const reachedTheMeter = (status: number): boolean =>
  status !== 401 && status !== 403 && status !== 429;

const accountForCredit = async (
  response: Response,
  method: string,
  path: string
): Promise<void> => {
  if (response.status === 402) {
    await reportSandboxCreditExhausted(response);
    return;
  }

  if (spendsSandboxCredit(method, path) && reachedTheMeter(response.status)) {
    notifySandboxCreditSpent();
  }
};

const reportSandboxCreditExhausted = async (
  response: Response
): Promise<void> => {
  let credit: SandboxCreditProblem | null = null;

  try {
    credit = readSandboxCreditProblem(await response.clone().json());
  } catch {
  }

  notifySandboxCreditSpent();

  toast.error(t("common.outSandboxCredits"), {
    id: SANDBOX_CREDIT_TOAST_ID,
    description: sandboxExhaustedDescription(credit),
  });
};

const queryOf = (input: string): string => {
  const index = input.search(/[?#]/);
  return index === -1 ? "" : input.slice(index);
};

const isAbort = (error: unknown): boolean =>
  error instanceof DOMException && error.name === "AbortError";
