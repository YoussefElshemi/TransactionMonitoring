import { toast } from "sonner";

import { translateForReader as t } from "@/lib/i18n";

import {
  PagedMetadata,
  PermissionDeniedProblemDetails,
  PermissionNotGrantableProblemDetails,
  ProblemDetails,
  RoleInUseProblemDetails,
  SchemaViolation,
  SchemaViolationProblemDetails,
  ValidationProblemDetails,
} from "@/types";

export const API_BASE = (
  process.env.NEXT_PUBLIC_API_BASE ?? "http://localhost:5253"
).replace(/\/+$/, "");

export const organizationScopedPath = (
  suffix: string,
  organizationId?: string
): string =>
  organizationId
    ? `${API_BASE}/organizations/${organizationId}/${suffix}`
    : `${API_BASE}/${suffix}`;

export interface ResponseData<T> {
  data: T;
}

export const unwrap = async <T>(response: Response): Promise<T> =>
  ((await response.json()) as ResponseData<T>).data;

export const unwrapPage = async <T>(
  response: Response
): Promise<{ metadata: PagedMetadata; data: T[] }> =>
  (await response.json()) as { metadata: PagedMetadata; data: T[] };

export const formatValidationErrors = (
  data: ValidationProblemDetails,
  labels?: Record<string, string>
): string =>
  Object.keys(data.errors)
    .map((key) => {
      const label = labels?.[key] ?? key;
      return `${label}${label.length ? ": " : ""}${data.errors[key].join("\n")}`;
    })
    .join("\n");

export type SchemaViolationKind =
  | "rule"
  | "payload"
  | "party"
  | "partyMapping"
  | "velocityProfile";

export interface SchemaViolationReport {
  kind: SchemaViolationKind;
  title: string;
  violations: SchemaViolation[];
}

const RULE_VIOLATION_TYPE = "RuleSchemaViolationException";
const PAYLOAD_VIOLATION_TYPE = "PayloadSchemaViolationException";
const PARTY_VIOLATION_TYPE = "PartyRuleViolationException";
const PARTY_MAPPING_VIOLATION_TYPE = "PartyMappingSchemaViolationException";
const VELOCITY_PROFILE_VIOLATION_TYPE =
  "VelocityProfileSchemaViolationException";

const VIOLATION_KINDS: Record<string, SchemaViolationKind> = {
  [RULE_VIOLATION_TYPE]: "rule",
  [PAYLOAD_VIOLATION_TYPE]: "payload",
  [PARTY_VIOLATION_TYPE]: "party",
  [PARTY_MAPPING_VIOLATION_TYPE]: "partyMapping",
  [VELOCITY_PROFILE_VIOLATION_TYPE]: "velocityProfile",
};

const VIOLATION_TITLES: Record<SchemaViolationKind, string> = {
  rule: "Rule does not match the transaction schema",
  payload: "Transaction payload does not match the transaction schema",
  party: "Rule does not match the party mapping",
  partyMapping: "Party mapping does not match the transaction schema",
  velocityProfile: "Velocity profile does not match the transaction schema",
};

export const readSchemaViolations = (
  body: unknown
): SchemaViolationReport | null => {
  const problem = body as SchemaViolationProblemDetails | null;
  if (!problem || !Array.isArray(problem.violations)) return null;

  const kind = problem.type ? VIOLATION_KINDS[problem.type] : undefined;
  if (!kind) return null;

  return {
    kind,
    title: problem.title || VIOLATION_TITLES[kind],
    violations: problem.violations,
  };
};

export const violationsByField = (
  violations: SchemaViolation[]
): Map<string, string[]> => {
  const byField = new Map<string, string[]>();
  for (const violation of violations) {
    const existing = byField.get(violation.field);
    if (existing) existing.push(violation.message);
    else byField.set(violation.field, [violation.message]);
  }
  return byField;
};

export const formatSchemaViolations = (violations: SchemaViolation[]): string =>
  violations.map(({ field, message }) => `${field}: ${message}`).join("\n");

const PERMISSION_DENIED_TYPE = "PermissionDeniedException";
const PERMISSION_NOT_GRANTABLE_TYPE = "PermissionNotGrantableException";
const BUILT_IN_ROLE_TYPE = "BuiltInRoleException";
const ROLE_IN_USE_TYPE = "RoleInUseException";
const CROSS_ORGANIZATION_SCOPE_TYPE = "CrossOrganizationScopeException";

export type PermissionDenialKind = "missing" | "reach";

export interface PermissionDenialReport {
  kind: PermissionDenialKind;
  requiredPermission: string | null;
  detail: string | null;
}

export const readPermissionDenial = (
  body: unknown
): PermissionDenialReport | null => {
  const problem = body as PermissionDeniedProblemDetails | null;
  if (!problem || problem.type !== PERMISSION_DENIED_TYPE) return null;

  const detail = problem.detail ?? null;
  return {
    kind: detail && /does not reach|not reach|out of scope/i.test(detail)
      ? "reach"
      : "missing",
    requiredPermission: problem.requiredPermission ?? null,
    detail,
  };
};

export const readNotGrantable = (body: unknown): string[] | null => {
  const problem = body as PermissionNotGrantableProblemDetails | null;
  if (!problem || problem.type !== PERMISSION_NOT_GRANTABLE_TYPE) return null;
  return problem.permissions ?? [];
};

export const readRoleInUse = (
  body: unknown
): { assignmentCount: number; userIds: string[] } | null => {
  const problem = body as RoleInUseProblemDetails | null;
  if (!problem || problem.type !== ROLE_IN_USE_TYPE) return null;
  return {
    assignmentCount: problem.assignmentCount ?? 0,
    userIds: problem.userIds ?? [],
  };
};

export const permissionDenialMessage = (
  report: PermissionDenialReport
): { title: string; description: string | undefined } => {
  const name = report.requiredPermission;

  if (report.kind === "reach") {
    return {
      title: name
        ? `Your ${name} permission doesn't reach this`
        : "Your permission doesn't reach this",
      description:
        report.detail ??
        "You hold the permission, but only over some of the organisation — not over this queue, rule set or business unit. Ask for the grant to be widened rather than for the permission again.",
    };
  }

  return {
    title: name ? `You need ${name} to do this` : "You don't have permission",
    description: report.detail ?? undefined,
  };
};

interface ReportOptions {
  fallback: string;
  onUnauthorized: () => void;
  toastId?: string;
  fieldLabels?: Record<string, string>;
  onSchemaViolations?: (report: SchemaViolationReport) => void;
  onPermissionDenied?: (report: PermissionDenialReport) => void;
  suppressPermissionToast?: boolean;
}

export const correlationId = (response: Response): string | null =>
  response.headers.get("X-Correlation-ID");

export const reportApiError = async (
  response: Response,
  {
    fallback,
    onUnauthorized,
    toastId,
    fieldLabels,
    onSchemaViolations,
    onPermissionDenied,
    suppressPermissionToast,
  }: ReportOptions
): Promise<void> => {
  if (response.status === 401) {
    toast.error(t("common.pleaseLoginPerformAction"), { id: "unauthorized" });
    onUnauthorized();
    return;
  }

  const correlation = correlationId(response);
  const trace = correlation ? { description: `Reference: ${correlation}` } : {};

  let body: unknown = null;
  try {
    body = await response.json();
  } catch {
  }

  if (response.status === 400) {
    const schema = readSchemaViolations(body);
    if (schema) {
      onSchemaViolations?.(schema);
      toast.error(schema.title, {
        id: toastId,
        description: formatSchemaViolations(schema.violations),
        descriptionClassName: "whitespace-pre-line",
      });
      return;
    }

    if ((body as ProblemDetails & { type?: string } | null)?.type === CROSS_ORGANIZATION_SCOPE_TYPE) {
      toast.error(t("common.scopeOutsideYourOrganisation"), {
        id: toastId,
        description:
          (body as ProblemDetails | null)?.detail ||
          t("common.queueRuleSetBusinessUnit"),
      });
      return;
    }

    const validation = body as ValidationProblemDetails | null;
    if (validation?.errors) {
      toast.error(formatValidationErrors(validation, fieldLabels), {
        id: toastId,
      });
      return;
    }
  }

  if (response.status === 403) {
    const denial = readPermissionDenial(body);
    if (denial) {
      onPermissionDenied?.(denial);
      if (!suppressPermissionToast) {
        const { title, description } = permissionDenialMessage(denial);
        toast.error(title, { id: toastId, description });
      }
      return;
    }

    const notGrantable = readNotGrantable(body);
    if (notGrantable) {
      toast.error(t("common.youCanTGrantPermissions"), {
        id: toastId,
        description: notGrantable.length
          ? t("common.refusedList", notGrantable.join(", "))
          : (body as ProblemDetails | null)?.detail,
        descriptionClassName: "break-words",
      });
      return;
    }

    toast.error(
      (body as ProblemDetails | null)?.detail ||
        t("common.youDoNotHavePermission2"),
      { id: toastId }
    );
    return;
  }

  if (response.status === 429) {
    const retryAfter = Number(response.headers.get("Retry-After"));
    const seconds =
      Number.isFinite(retryAfter) && retryAfter > 0 ? retryAfter : null;

    toast.error(t("common.apiBusy"), {
      id: toastId ?? "rate-limited",
      description: seconds
        ? t(
            seconds === 1
              ? "common.tooManyRequestsRetryOneSecond"
              : "common.tooManyRequestsRetryIn",
            seconds
          )
        : t("common.tooManyRequestsRetryShortly"),
    });
    return;
  }

  if (response.status === 409) {
    const problem = body as
      | (ProblemDetails & { type?: string; title?: string })
      | null;
    const problemType = problem?.type;
    // The idempotency filter reports both of its conflicts by title rather than
    // by `type`, so this is what separates them from a domain conflict.
    const problemTitle = problem?.title?.toLowerCase() ?? "";

    if (problemTitle.includes("request in progress")) {
      toast.error(t("common.stillWorkingYourLastRequest"), {
        id: toastId,
        description:
          t("common.earlierAttemptTakingLongerThan"),
      });
      return;
    }

    if (problemTitle.includes("x-idempotency-key already used")) {
      toast.error(t("common.actionAlreadySubmitted"), {
        id: toastId,
        description:
          (body as ProblemDetails | null)?.detail ??
          t("common.reloadPageTryAgainSame"),
      });
      return;
    }

    if (problemType === BUILT_IN_ROLE_TYPE) {
      toast.error(t("common.builtRolesCanTChanged"), {
        id: toastId,
        description:
          t("common.userManagerAdminAuditorAre"),
      });
      return;
    }

    const inUse = readRoleInUse(body);
    if (inUse) {
      toast.error(t("common.somebodyStillRole"), {
        id: toastId,
        description: t(
          inUse.assignmentCount === 1
            ? "common.oneAssignmentPointingAtNothing"
            : "common.assignmentsPointingAtNothing",
          inUse.assignmentCount
        ),
      });
      return;
    }
  }

  toast.error((body as ProblemDetails | null)?.detail || fallback, {
    id: toastId,
    ...trace,
  });
};
