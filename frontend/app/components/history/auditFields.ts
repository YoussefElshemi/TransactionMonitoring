import { AuditLogChange } from "@/types";

const FIELD_LABELS: Record<string, string> = {
  FriendlyName: "Name",
  RuleBody: "Rule logic",
  IsEnabled: "Enabled",
  RuleSetId: "Rule set",
  CurrentVersionNumber: "Current version",
  IsDeleted: "Deleted",
  AssignedToUserId: "Assigned to",
  AssignedAt: "Assigned at",
  ResolvedAt: "Resolved at",
  Disposition: "Disposition",
  CaseReasonCodeId: "Reason code",
  ReopenedAt: "Reopened at",
  CaseId: "Case",
  Message: "Message",
  EndpointUrl: "Endpoint URL",
  CostPerRequest: "Cost per request",
  UserName: "Username",
  Email: "Email",
  EmailConfirmed: "Email confirmed",
  OrganizationId: "Organization",
  Fields: "Schema fields",
  Mappings: "Party mapping",
  RiskScore: "Risk score",
  RiskNote: "Reason",
  RiskSetByUserId: "Assessed by",
  RiskSetAt: "Assessed at",
  Matcher: "Matcher",
  MinimumSeverity: "Severity floor",
  Priority: "Priority (routing order)",
  UnassignedForMinutes: "Unassigned for (minutes)",
  HoldExpiryWithinMinutes: "Hold expiring within (minutes)",
  TargetQueueId: "Moves to queue",
  RaiseSeverityTo: "Raises severity to",
  EscalatedAt: "Escalated by a rule at",
  EscalationRuleId: "Escalated by rule",
  ReviewedByUserId: "Reviewed by",
  ReviewedAt: "Reviewed at",
  ReviewNote: "Reviewer's note",
  ScanStatus: "Scan status",
  ScannedAt: "Scanned at",
  ThreatName: "Threat",
  FileName: "File name",
  ByteCount: "Size (bytes)",
  Checksum: "Checksum",
  UploadedByUserId: "Uploaded by",
  LinkedCaseId: "Linked case",
  Reason: "Reason",
  Channel: "Channel",
  Destination: "Destination",
  NotifyOnBreach: "Notify on breach",
};

const splitPascalCase = (field: string): string => {
  const words = field.replace(/([a-z0-9])([A-Z])/g, "$1 $2");
  return words.charAt(0).toUpperCase() + words.slice(1).toLowerCase();
};

export const fieldLabel = (field: string): string =>
  FIELD_LABELS[field] ?? splitPascalCase(field);

export type FieldKind =
  | "user"
  | "ruleSet"
  | "case"
  | "organization"
  | "money"
  | "longText"
  | "disposition"
  | "plain";

const FIELD_KINDS: Record<string, FieldKind> = {
  AssignedToUserId: "user",
  RuleSetId: "ruleSet",
  CaseId: "case",
  OrganizationId: "organization",
  CostPerRequest: "money",
  RuleBody: "longText",
  Message: "longText",
  Fields: "longText",
  Mappings: "longText",
  RiskSetByUserId: "user",
  ReviewedByUserId: "user",
  ReviewNote: "longText",
  RiskNote: "longText",
  Matcher: "longText",
  Disposition: "disposition",
  UploadedByUserId: "user",
  LinkedCaseId: "case",
  Reason: "longText",
};

export const fieldKind = (field: string): FieldKind =>
  FIELD_KINDS[field] ?? "plain";

export const referenceHref = (
  kind: FieldKind,
  id: string,
  canReachAdminPages: boolean
): string | null => {
  switch (kind) {
    case "ruleSet":
      return `/rule-sets/${id}`;
    case "case":
      return `/cases/${id}`;
    case "organization":
      return canReachAdminPages ? `/admin/organizations/${id}` : null;
    default:
      return null;
  }
};

const GUID_PATTERN =
  /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

export const isGuid = (value: string): boolean => GUID_PATTERN.test(value);

// `2026-08-12T00:49:31.123+00:00` and friends. Deliberately strict so that a
// free-text note starting with a number is not mistaken for a timestamp.
const TIMESTAMP_PATTERN = /^\d{4}-\d{2}-\d{2}[T ]\d{2}:\d{2}/;

export const isTimestamp = (value: string): boolean =>
  TIMESTAMP_PATTERN.test(value) && !Number.isNaN(Date.parse(value));

export const shortenId = (id: string): string => `${id.slice(0, 8)}…`;

const IS_DELETED = "IsDeleted";

export type Lifecycle = "softDeleted" | "recovered" | null;

export const lifecycleOf = (changes: AuditLogChange[]): Lifecycle => {
  const change = changes.find((c) => c.field === IS_DELETED);
  if (!change) return null;
  if (change.oldValue !== true && change.newValue === true) return "softDeleted";
  if (change.oldValue === true && change.newValue !== true) return "recovered";
  return null;
};

export const isDeletedField = (field: string): boolean => field === IS_DELETED;
