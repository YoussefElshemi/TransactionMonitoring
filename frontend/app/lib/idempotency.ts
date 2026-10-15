import { apiFetch, wasReplayed } from "@/lib/http";

export {
  IDEMPOTENCY_KEY_HEADER,
  IDEMPOTENT_REPLAY_HEADER,
  newIdempotencyKey,
  wasReplayed,
} from "@/lib/http";

export const TRANSACTION_REFERENCE_HEADER = "X-Transaction-Reference";
export const CREATE_CASE_HEADER = "X-Create-Case";

export interface EvaluateResult<T> {
  data: T;
  status: number;
  replayed: boolean | null;
}

export class IdempotencyKeyReusedError extends Error {
  constructor(detail: string) {
    super(detail);
    this.name = "IdempotencyKeyReusedError";
  }
}

export class TransactionReferenceConflictError extends Error {
  constructor(detail: string) {
    super(detail);
    this.name = "TransactionReferenceConflictError";
  }
}

export class InvalidIdempotencyKeyError extends Error {
  constructor(detail: string) {
    super(detail);
    this.name = "InvalidIdempotencyKeyError";
  }
}

export class RateLimitedError extends Error {
  constructor(
    readonly retryAfterSeconds: number | null,
    detail: string
  ) {
    super(detail);
    this.name = "RateLimitedError";
  }
}

export class EvaluateError extends Error {
  constructor(
    readonly response: Response,
    readonly body: unknown
  ) {
    super(`Evaluation failed with ${response.status}`);
    this.name = "EvaluateError";
  }
}

export interface EvaluateOptions {
  idempotencyKey?: string;
  transactionReference?: string;
  createCase?: boolean;
  signal?: AbortSignal;
  maxInProgressRetries?: number;
}

const readBody = async (response: Response): Promise<unknown> => {
  try {
    return await response.json();
  } catch {
    return null;
  }
};

export const postEvaluation = async <T>(
  path: string,
  body: unknown,
  {
    idempotencyKey,
    transactionReference,
    createCase,
    signal,
    maxInProgressRetries = 5,
  }: EvaluateOptions = {}
): Promise<EvaluateResult<T>> => {
  const headers: Record<string, string> = {
    "Content-Type": "application/json",
  };

  if (transactionReference) {
    headers[TRANSACTION_REFERENCE_HEADER] = transactionReference;
  }

  if (createCase) {
    headers[CREATE_CASE_HEADER] = "true";
  }

  const response = await apiFetch(
    path,
    {
      method: "POST",
      headers,
      body: JSON.stringify(body),
      signal,
    },
    { idempotencyKey, maxInProgressRetries }
  );

  if (response.ok) {
    const replayed = wasReplayed(response);
    const payload = (await readBody(response)) as { data: T } | null;

    return {
      data: payload?.data as T,
      status: response.status,
      replayed,
    };
  }

  const errorBody = (await readBody(response)) as {
    title?: string;
    detail?: string;
  } | null;

  if (response.status === 429) {
    const retryAfter = Number(response.headers.get("Retry-After"));

    throw new RateLimitedError(
      Number.isFinite(retryAfter) && retryAfter > 0 ? retryAfter : null,
      errorBody?.detail ??
        "The API is rate limiting this client. The evaluation was not run — try again shortly."
    );
  }

  if (response.status === 409) {
    const title = errorBody?.title?.toLowerCase() ?? "";

    if (title.includes(TRANSACTION_REFERENCE_HEADER.toLowerCase())) {
      throw new TransactionReferenceConflictError(
        errorBody?.detail ??
          `This ${TRANSACTION_REFERENCE_HEADER} is already recorded against a different payload. Use a new reference for a new transaction.`
      );
    }

    if (title.includes("in progress")) {
      throw new EvaluateError(response, errorBody);
    }

    if (idempotencyKey) {
      throw new IdempotencyKeyReusedError(
        errorBody?.detail ??
          `This idempotency key was used for a different request. Use a fresh key for a new one.`
      );
    }
  }

  if (
    response.status === 400 &&
    errorBody?.title?.toLowerCase().includes("invalid x-idempotency-key")
  ) {
    throw new InvalidIdempotencyKeyError(
      errorBody.detail ?? "X-Idempotency-Key must be a GUID."
    );
  }

  throw new EvaluateError(response, errorBody);
};

export const evaluateRule = <T>(
  ruleId: string,
  body: unknown,
  options?: EvaluateOptions
) => postEvaluation<T>(`/rules/${ruleId}/evaluate`, body, options);

export const evaluateRuleSets = <T>(body: unknown, options?: EvaluateOptions) =>
  postEvaluation<T>("/rule-sets/evaluate", body, options);

export const evaluateRuleSet = <T>(
  ruleSetId: string,
  body: unknown,
  options?: EvaluateOptions
) => postEvaluation<T>(`/rule-sets/${ruleSetId}/evaluate`, body, options);
