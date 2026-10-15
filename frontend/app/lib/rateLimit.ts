export type RateLimitPool = "read" | "write" | "evaluation";

export const RATE_LIMIT_POOLS: readonly RateLimitPool[] = [
  "read",
  "write",
  "evaluation",
];

export const RATE_LIMIT_LIMIT_HEADER = "RateLimit-Limit";
export const RATE_LIMIT_REMAINING_HEADER = "RateLimit-Remaining";
export const RATE_LIMIT_RESET_HEADER = "RateLimit-Reset";
export const RETRY_AFTER_HEADER = "Retry-After";

// POSTs the API classifies as evaluation. Must match the server's [RateLimit(Evaluation)] list.
const EVALUATION_PATHS: readonly RegExp[] = [
  /^\/rules\/[^/]+\/evaluate$/,
  /^\/rule-sets\/evaluate$/,
  /^\/rule-sets\/[^/]+\/evaluate$/,
];

export const poolForRequest = (method: string, path: string): RateLimitPool => {
  const verb = method.toUpperCase();

  if (verb === "GET" || verb === "HEAD") return "read";
  if (EVALUATION_PATHS.some((pattern) => pattern.test(path)))
    return "evaluation";

  return "write";
};

export interface RateLimitBudget {
  limit: number | null;
  remaining: number | null;
  resetSeconds: number | null;
  observedAt: number | null;
  blockedUntil: number | null;
}

const emptyBudget = (): RateLimitBudget => ({
  limit: null,
  remaining: null,
  resetSeconds: null,
  observedAt: null,
  blockedUntil: null,
});

export interface RateLimitStatus {
  throttled: boolean;
  waiting: number;
  longestWaitMs: number;
  resumesAt: number | null;
  pools: readonly RateLimitPool[];
}

type Listener = (status: RateLimitStatus) => void;

const budgets: Record<RateLimitPool, RateLimitBudget> = {
  read: emptyBudget(),
  write: emptyBudget(),
  evaluation: emptyBudget(),
};

const listeners = new Set<Listener>();

const waiters = new Map<number, { pool: RateLimitPool; since: number }>();
let nextWaiterId = 0;

const readNumericHeader = (
  response: Response,
  header: string
): number | null => {
  const raw = response.headers.get(header);
  if (raw === null) return null;

  const value = Number(raw.trim());

  return Number.isFinite(value) && value >= 0 ? value : null;
};

export const readRetryAfterMs = (response: Response): number | null => {
  const raw = response.headers.get(RETRY_AFTER_HEADER);
  if (raw === null) return null;

  const seconds = Number(raw.trim());
  if (Number.isFinite(seconds) && seconds >= 0) return seconds * 1000;

  const date = Date.parse(raw);
  if (!Number.isNaN(date)) return Math.max(0, date - Date.now());

  return null;
};

export const observeResponse = (
  pool: RateLimitPool,
  response: Response
): void => {
  const limit = readNumericHeader(response, RATE_LIMIT_LIMIT_HEADER);
  const remaining = readNumericHeader(response, RATE_LIMIT_REMAINING_HEADER);
  const resetSeconds = readNumericHeader(response, RATE_LIMIT_RESET_HEADER);

  if (limit === null && remaining === null && resetSeconds === null) return;

  const budget = budgets[pool];

  budget.limit = limit ?? budget.limit;
  budget.remaining = remaining ?? budget.remaining;
  budget.resetSeconds = resetSeconds ?? budget.resetSeconds;
  budget.observedAt = Date.now();

  if (response.status === 429) {
    const retryAfterMs =
      readRetryAfterMs(response) ??
      (resetSeconds !== null ? resetSeconds * 1000 : null);

    if (retryAfterMs !== null) {
      budget.blockedUntil = Math.max(
        budget.blockedUntil ?? 0,
        Date.now() + retryAfterMs
      );
    }
  }

  publish();
};

const spend = (pool: RateLimitPool): void => {
  const budget = budgets[pool];
  if (budget.remaining !== null && budget.remaining > 0) budget.remaining -= 1;
};

const HEADROOM_THRESHOLD = 0.2;

const MAX_PACING_DELAY_MS = 2_000;

const MAX_EXHAUSTED_WAIT_MS = 5_000;

const blockedDelayMs = (pool: RateLimitPool): number => {
  const until = budgets[pool].blockedUntil;
  if (until === null) return 0;

  return Math.max(0, until - Date.now());
};

const pacingDelayMs = (pool: RateLimitPool): number => {
  const { limit, remaining, resetSeconds, observedAt } = budgets[pool];

  if (limit === null || remaining === null || limit <= 0) return 0;
  if (remaining / limit > HEADROOM_THRESHOLD) return 0;

  const windowMs = (resetSeconds ?? 60) * 1000;
  const elapsedMs = observedAt === null ? 0 : Date.now() - observedAt;
  const remainingWindowMs = Math.max(0, windowMs - elapsedMs);

  if (remainingWindowMs === 0) return 0;

  if (remaining <= 0) {
    return Math.min(remainingWindowMs, MAX_EXHAUSTED_WAIT_MS);
  }

  return Math.min(remainingWindowMs / remaining, MAX_PACING_DELAY_MS);
};

const CONCURRENCY_LIMITS: Record<RateLimitPool, number> = {
  read: 6,
  write: 3,
  evaluation: 4,
};

interface Gate {
  active: number;
  queue: (() => void)[];
}

const gates: Record<RateLimitPool, Gate> = {
  read: { active: 0, queue: [] },
  write: { active: 0, queue: [] },
  evaluation: { active: 0, queue: [] },
};

export const resetRateLimitState = (): void => {
  for (const pool of RATE_LIMIT_POOLS) {
    budgets[pool] = emptyBudget();
  }
  publish();
};

const acquire = (pool: RateLimitPool): Promise<void> => {
  const gate = gates[pool];

  if (gate.active < CONCURRENCY_LIMITS[pool]) {
    gate.active += 1;
    return Promise.resolve();
  }

  return new Promise<void>((resolve) => {
    gate.queue.push(() => {
      gate.active += 1;
      resolve();
    });
  });
};

const release = (pool: RateLimitPool): void => {
  const gate = gates[pool];
  gate.active -= 1;

  const next = gate.queue.shift();
  if (next) next();
};

export const sleep = (ms: number, signal?: AbortSignal): Promise<void> =>
  new Promise<void>((resolve, reject) => {
    if (signal?.aborted) {
      reject(new DOMException("Aborted", "AbortError"));
      return;
    }

    const onAbort = () => {
      clearTimeout(timer);
      reject(new DOMException("Aborted", "AbortError"));
    };

    const timer = setTimeout(() => {
      signal?.removeEventListener("abort", onAbort);
      resolve();
    }, ms);

    signal?.addEventListener("abort", onAbort, { once: true });
  });

export const withJitter = (delayMs: number): number => {
  const floor = delayMs * 0.5;
  return floor + Math.random() * (delayMs - floor);
};

export const beginWait = (pool: RateLimitPool): (() => void) => {
  const id = nextWaiterId++;
  waiters.set(id, { pool, since: Date.now() });
  publish();

  return () => {
    if (waiters.delete(id)) publish();
  };
};

export const throughGate = async <T>(
  pool: RateLimitPool,
  run: () => Promise<T>,
  signal?: AbortSignal
): Promise<T> => {
  let done = beginWait(pool);
  await acquire(pool);

  try {
    // A block is re-checked until it clears; pacing is applied at most once.
    let paced = false;

    for (;;) {
      const blocked = blockedDelayMs(pool);

      if (blocked > 0) {
        await sleep(blocked, signal);
        continue;
      }

      if (paced) break;
      paced = true;

      const pacing = pacingDelayMs(pool);
      if (pacing <= 0) break;

      await sleep(pacing, signal);
    }

    spend(pool);
    done();
    done = () => {};

    return await run();
  } finally {
    done();
    release(pool);
  }
};

export const mapBounded = async <T, R>(
  items: readonly T[],
  run: (item: T, index: number) => Promise<R>,
  {
    pool = "read",
    signal,
  }: { pool?: RateLimitPool; signal?: AbortSignal } = {}
): Promise<R[]> => {
  const results = new Array<R>(items.length);
  let cursor = 0;

  const worker = async (): Promise<void> => {
    for (;;) {
      const index = cursor++;
      if (index >= items.length) return;
      if (signal?.aborted) throw new DOMException("Aborted", "AbortError");
      results[index] = await run(items[index], index);
    }
  };

  const workers = Array.from(
    { length: Math.min(CONCURRENCY_LIMITS[pool], items.length) },
    worker
  );

  await Promise.all(workers);

  return results;
};

const currentStatus = (): RateLimitStatus => {
  const now = Date.now();

  const blocked = RATE_LIMIT_POOLS.filter((pool) => {
    const until = budgets[pool].blockedUntil;
    return until !== null && until > now;
  });

  let longestWaitMs = 0;
  for (const { since } of waiters.values()) {
    longestWaitMs = Math.max(longestWaitMs, now - since);
  }

  const resumesAt = blocked.reduce<number | null>((latest, pool) => {
    const until = budgets[pool].blockedUntil as number;
    return latest === null || until > latest ? until : latest;
  }, null);

  return {
    throttled: blocked.length > 0,
    waiting: waiters.size,
    longestWaitMs,
    resumesAt,
    pools: blocked,
  };
};

const publish = (): void => {
  const status = currentStatus();
  for (const listener of listeners) listener(status);
};

export const subscribeToRateLimit = (listener: Listener): (() => void) => {
  listeners.add(listener);
  listener(currentStatus());

  return () => {
    listeners.delete(listener);
  };
};

export const rateLimitStatus = (): RateLimitStatus => currentStatus();

export const budgetFor = (pool: RateLimitPool): Readonly<RateLimitBudget> =>
  budgets[pool];
