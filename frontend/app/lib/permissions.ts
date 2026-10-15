import {
  ApplicationUser,
  PermissionGrant,
  PermissionScope,
  PermissionScopeType,
} from "@/types";

export const PERMISSIONS = [
  "alerts:read",
  "api-clients:read",
  "api-clients:write",
  "approvals:read",
  "approvals:decide",
  "attachments:read",
  "attachments:write",
  "audit-logs:read",
  "audit-logs:export",
  "audit-logs:verify",
  "backtests:read",
  "backtests:write",
  "billing-entities:read",
  "billing-entities:write",
  "billing-profiles:read",
  "billing-profiles:write",
  "cases:read",
  "cases:write",
  "cases:assign",
  "cases:resolve",
  "cases:delete",
  "cases:evidence-pack",
  "case-links:read",
  "case-links:write",
  "case-notes:read",
  "case-notes:write",
  "case-reason-codes:read",
  "case-reason-codes:write",
  "contract-terms:read",
  "contract-terms:write",
  "credit-notes:read",
  "credit-notes:write",
  "decisions:resolve",
  "escalation-rules:read",
  "escalation-rules:write",
  "exchange-rates:read",
  "exchange-rates:write",
  "invoices:read",
  "invoices:write",
  "notification-subscriptions:read",
  "notification-subscriptions:write",
  "organizations:read",
  "organizations:write",
  "parties:read",
  "parties:write",
  "party-mappings:read",
  "party-mappings:write",
  "plans:read",
  "plans:write",
  "queues:read",
  "queues:write",
  "queues:claim",
  "rules:read",
  "rules:write",
  "rules:deploy",
  "rules:evaluate",
  "rules:test",
  "rule-sets:read",
  "rule-sets:write",
  "rule-sets:evaluate",
  "rule-evaluations:read",
  "sla-targets:read",
  "sla-targets:write",
  "statistics:read",
  "suspicious-activity-reports:read",
  "suspicious-activity-reports:write",
  "suspicious-activity-reports:file",
  "suspicious-activity-reports:export",
  "transactions:read",
  "transactions:reveal",
  "transaction-schemas:read",
  "transaction-schemas:write",
  "usages:read",
  "users:read",
  "users:write",
  "invitations:read",
  "invitations:write",
  "users:offboard",
  "velocity-profiles:read",
  "velocity-profiles:write",
  "webhook-configurations:read",
  "webhook-configurations:write",
  "approval-policies:read",
  "approval-policies:write",
  "suspicious-activity-report-policies:read",
  "suspicious-activity-report-policies:write",
  "permissions:read",
  "roles:read",
  "roles:write",
  "roles:assign",
  "sub-organizations:read",
  "sub-organizations:write",
  "subscriptions:read",
  "subscriptions:write",
  "platform:administer",
  "sandbox:read",
  "sandbox:write",
  "sandbox:reset",
  "incidents:read",
  "incidents:write",
  "maintenance-windows:read",
  "maintenance-windows:write",
  "availability:read",
  "availability-commitments:read",
  "availability-commitments:write",
  "service-credits:read",
  "service-credits:issue",
  "payments:read",
  "payments:collect",
  "payments:reconcile",
  "payments:refund",
  "payment-methods:read",
  "payment-methods:write",
  "dunning:read",
  "revenue:read",
  "service-cost-rates:read",
  "service-cost-rates:write",
] as const;

export type Permission = (typeof PERMISSIONS)[number];

export const PLATFORM_ADMINISTER: Permission = "platform:administer";

// GET /permissions/mine 403s for accounts still in the default organization, which would hide
// the admin console from admins. Built-in Admin membership implies the full bundle, so use it.
export const unreadableGrantsFallback = (
  user: ApplicationUser | null
): PermissionGrant[] => {
  const administersByRole =
    !!user && user.subOrganizationId === null && user.roles.includes("Admin");

  if (!administersByRole) return [];

  return PERMISSIONS.map((name) => ({
    name,
    scope: { scopeType: "Organization" },
  }));
};

export interface ScopeTarget {
  subOrganizationId?: string | null;
  ruleSetId?: string | null;
  queueId?: string | null;
}

export const grantsHold = (
  grants: PermissionGrant[] | null,
  permission: Permission
): boolean => (grants ?? []).some((grant) => grant.name === permission);

export type BillingWritePermission =
  | "plans:write"
  | "subscriptions:write"
  | "contract-terms:write"
  | "billing-entities:write"
  | "billing-profiles:write"
  | "exchange-rates:write"
  | "credit-notes:write"
  | "payments:reconcile"
  | "payments:refund";

export const canWriteBilling = (
  grants: PermissionGrant[] | null,
  permission: BillingWritePermission
): boolean =>
  grantsHold(grants, permission) && grantsHold(grants, PLATFORM_ADMINISTER);

export type StatusWritePermission =
  | "incidents:write"
  | "maintenance-windows:write"
  | "availability-commitments:write"
  | "service-credits:issue";

export const canOperateStatus = (
  grants: PermissionGrant[] | null,
  permission: StatusWritePermission
): boolean =>
  grantsHold(grants, permission) && grantsHold(grants, PLATFORM_ADMINISTER);

export type RevenuePermission =
  | "revenue:read"
  | "service-cost-rates:read"
  | "service-cost-rates:write";

export const canReadRevenue = (
  grants: PermissionGrant[] | null,
  permission: RevenuePermission = "revenue:read"
): boolean =>
  grantsHold(grants, permission) && grantsHold(grants, PLATFORM_ADMINISTER);

export const grantsHoldOver = (
  grants: PermissionGrant[] | null,
  permission: Permission,
  target: ScopeTarget
): boolean =>
  (grants ?? []).some(
    (grant) => grant.name === permission && scopeCovers(grant.scope, target)
  );

export const scopeCovers = (
  scope: PermissionScope,
  target: ScopeTarget
): boolean => {
  switch (scope.scopeType) {
    case "Organization":
      return true;
    case "SubOrganization":
      return (
        !!scope.subOrganizationId &&
        scope.subOrganizationId === target.subOrganizationId
      );
    case "RuleSet":
      return !!scope.ruleSetId && scope.ruleSetId === target.ruleSetId;
    case "Queue":
      return !!scope.queueId && scope.queueId === target.queueId;
    default:
      return false;
  }
};

export const heldPermissions = (
  grants: PermissionGrant[] | null
): Permission[] =>
  Array.from(new Set((grants ?? []).map((grant) => grant.name)))
    .sort()
    .filter((name): name is Permission =>
      (PERMISSIONS as readonly string[]).includes(name)
    );

export const grantsFor = (
  grants: PermissionGrant[] | null,
  permission: Permission
): PermissionGrant[] =>
  (grants ?? [])
    .filter((grant) => grant.name === permission)
    .sort(
      (a, b) =>
        SCOPE_BREADTH[a.scope.scopeType] - SCOPE_BREADTH[b.scope.scopeType]
    );

const SCOPE_BREADTH: Record<PermissionScopeType, number> = {
  Organization: 0,
  SubOrganization: 1,
  RuleSet: 2,
  Queue: 3,
};

export const holdsOrganizationWide = (
  grants: PermissionGrant[] | null,
  permission: Permission
): boolean =>
  (grants ?? []).some(
    (grant) =>
      grant.name === permission && grant.scope.scopeType === "Organization"
  );

export const splitPermission = (
  name: string
): { resource: string; action: string } => {
  const separator = name.indexOf(":");
  if (separator < 0) return { resource: name, action: "" };
  return {
    resource: name.slice(0, separator),
    action: name.slice(separator + 1),
  };
};

export const resourceLabel = (resource: string): string => {
  const words = resource.split("-").join(" ");
  return words.charAt(0).toUpperCase() + words.slice(1);
};

export const actionLabel = (action: string): string =>
  action.split("-").join(" ");

export const SCOPE_TYPE_LABELS: Record<PermissionScopeType, string> = {
  Organization: "Whole organisation",
  SubOrganization: "One business unit",
  RuleSet: "One rule set",
  Queue: "One queue",
};

export const SCOPE_TYPE_DESCRIPTIONS: Record<PermissionScopeType, string> = {
  Organization:
    "Everything the organisation has. This is what an assignment does if no scope is chosen.",
  SubOrganization:
    "Only the queues, rule sets and people in one business unit.",
  RuleSet: "Only that rule set and the rules inside it.",
  Queue: "Only that queue and the cases waiting in it.",
};

export const scopeIdField = (
  scopeType: PermissionScopeType
): keyof ScopeTarget | null => {
  switch (scopeType) {
    case "SubOrganization":
      return "subOrganizationId";
    case "RuleSet":
      return "ruleSetId";
    case "Queue":
      return "queueId";
    default:
      return null;
  }
};

export const scopeId = (scope: PermissionScope): string | null => {
  switch (scope.scopeType) {
    case "SubOrganization":
      return scope.subOrganizationId ?? null;
    case "RuleSet":
      return scope.ruleSetId ?? null;
    case "Queue":
      return scope.queueId ?? null;
    default:
      return null;
  }
};

// Queue and rule-set responses don't expose subOrganizationId, so it can be written but not read back.
export const SUB_ORGANIZATION_OWNERSHIP_UNREADABLE =
  "The API does not return which business unit owns a queue or a rule set, so this cannot show the current value. Setting it here still works; reading it back needs `subOrganizationId` on the queue and rule set responses.";
