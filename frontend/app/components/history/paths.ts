import { AuditEntityType } from "@/types";

export const historyPath = {
  Role: (organizationId: string, roleId: string) =>
    `/organizations/${organizationId}/roles/${roleId}/history`,
  SubOrganization: (organizationId: string, subOrganizationId: string) =>
    `/organizations/${organizationId}/sub-organizations/${subOrganizationId}/history`,
  Rule: (ruleId: string) => `/rules/${ruleId}/history`,
  RuleSet: (ruleSetId: string) => `/rule-sets/${ruleSetId}/history`,
  Transaction: (transactionId: string) =>
    `/transactions/${transactionId}/history`,
  Case: (caseId: string) => `/cases/${caseId}/history`,
  CaseNote: (caseId: string, caseNoteId: string) =>
    `/cases/${caseId}/notes/${caseNoteId}/history`,
  CaseReasonCode: (organizationId: string, caseReasonCodeId: string) =>
    `/organizations/${organizationId}/reason-codes/${caseReasonCodeId}/history`,
  ApiClient: (apiClientId: string, organizationId?: string) =>
    organizationId
      ? `/organizations/${organizationId}/api-clients/${apiClientId}/history`
      : `/api-clients/${apiClientId}/history`,
  Organization: (organizationId: string) =>
    `/organizations/${organizationId}/history`,
  WebhookConfiguration: (organizationId: string, webhookConfigurationId: string) =>
    `/organizations/${organizationId}/webhook-configurations/${webhookConfigurationId}/history`,
  ApplicationUser: (organizationId: string, userId: string) =>
    `/organizations/${organizationId}/users/${userId}/history`,
  TransactionSchema: (organizationSegment: string) =>
    `/organizations/${organizationSegment}/transaction-schema/history`,
  PartyMapping: (organizationSegment: string) =>
    `/organizations/${organizationSegment}/party-mapping/history`,
  VelocityProfile: (organizationSegment: string) =>
    `/organizations/${organizationSegment}/velocity-profile/history`,
  Party: (partyId: string) => `/parties/${partyId}/history`,
  Queue: (organizationId: string, queueId: string) =>
    `/organizations/${organizationId}/queues/${queueId}/history`,
  EscalationRule: (organizationId: string, escalationRuleId: string) =>
    `/organizations/${organizationId}/escalation-rules/${escalationRuleId}/history`,
  Approval: (approvalId: string) => `/approvals/${approvalId}/history`,
  Attachment: (caseId: string, attachmentId: string) =>
    `/cases/${caseId}/attachments/${attachmentId}/history`,
  CaseLink: (caseId: string, caseLinkId: string) =>
    `/cases/${caseId}/links/${caseLinkId}/history`,
  NotificationSubscription: (notificationSubscriptionId: string) =>
    `/notification-subscriptions/${notificationSubscriptionId}/history`,
  SuspiciousActivityReport: (suspiciousActivityReportId: string) =>
    `/suspicious-activity-reports/${suspiciousActivityReportId}/history`,
} satisfies Record<AuditEntityType, (...args: string[]) => string>;

export const ENTITY_NOUNS: Record<AuditEntityType, string> = {
  Role: "role",
  SubOrganization: "business unit",
  Rule: "rule",
  RuleSet: "rule set",
  Transaction: "transaction",
  Case: "case",
  CaseNote: "note",
  CaseReasonCode: "reason code",
  ApiClient: "API client",
  Organization: "organization",
  WebhookConfiguration: "webhook configuration",
  ApplicationUser: "user",
  TransactionSchema: "transaction schema",
  PartyMapping: "party mapping",
  VelocityProfile: "velocity profile",
  Party: "party",
  Queue: "queue",
  EscalationRule: "escalation rule",
  Approval: "approval",
  Attachment: "attachment",
  CaseLink: "case link",
  NotificationSubscription: "notification subscription",
  SuspiciousActivityReport: "filing",
};
