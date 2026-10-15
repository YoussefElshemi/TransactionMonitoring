using System.Collections.Frozen;
using Core.Enums;

namespace Core.Helpers;

public static class PermissionHelper
{
    public const string PolicyPrefix = "permission:";

    private static readonly FrozenDictionary<Permission, string> Names = new Dictionary<Permission, string>
    {
        [Permission.AlertsRead] = "alerts:read",
        [Permission.ApiClientsRead] = "api-clients:read",
        [Permission.ApiClientsWrite] = "api-clients:write",
        [Permission.ApprovalsRead] = "approvals:read",
        [Permission.ApprovalsDecide] = "approvals:decide",
        [Permission.AttachmentsRead] = "attachments:read",
        [Permission.AttachmentsWrite] = "attachments:write",
        [Permission.AuditLogsRead] = "audit-logs:read",
        [Permission.AuditLogsExport] = "audit-logs:export",
        [Permission.AuditLogsVerify] = "audit-logs:verify",
        [Permission.BacktestsRead] = "backtests:read",
        [Permission.BacktestsWrite] = "backtests:write",
        [Permission.CasesRead] = "cases:read",
        [Permission.CasesWrite] = "cases:write",
        [Permission.CasesAssign] = "cases:assign",
        [Permission.CasesResolve] = "cases:resolve",
        [Permission.CasesDelete] = "cases:delete",
        [Permission.CasesEvidencePack] = "cases:evidence-pack",
        [Permission.CaseLinksRead] = "case-links:read",
        [Permission.CaseLinksWrite] = "case-links:write",
        [Permission.CaseNotesRead] = "case-notes:read",
        [Permission.CaseNotesWrite] = "case-notes:write",
        [Permission.DecisionsResolve] = "decisions:resolve",
        [Permission.EscalationRulesRead] = "escalation-rules:read",
        [Permission.EscalationRulesWrite] = "escalation-rules:write",
        [Permission.InvoicesRead] = "invoices:read",
        [Permission.PlansRead] = "plans:read",
        [Permission.PlansWrite] = "plans:write",
        [Permission.SubscriptionsRead] = "subscriptions:read",
        [Permission.SubscriptionsWrite] = "subscriptions:write",
        [Permission.ContractTermsRead] = "contract-terms:read",
        [Permission.ContractTermsWrite] = "contract-terms:write",
        [Permission.InvoicesWrite] = "invoices:write",
        [Permission.NotificationSubscriptionsRead] = "notification-subscriptions:read",
        [Permission.NotificationSubscriptionsWrite] = "notification-subscriptions:write",
        [Permission.OrganizationsRead] = "organizations:read",
        [Permission.OrganizationsWrite] = "organizations:write",
        [Permission.PartiesRead] = "parties:read",
        [Permission.PartiesWrite] = "parties:write",
        [Permission.PartyMappingsRead] = "party-mappings:read",
        [Permission.PartyMappingsWrite] = "party-mappings:write",
        [Permission.QueuesRead] = "queues:read",
        [Permission.QueuesWrite] = "queues:write",
        [Permission.QueuesClaim] = "queues:claim",
        [Permission.CaseReasonCodesRead] = "case-reason-codes:read",
        [Permission.CaseReasonCodesWrite] = "case-reason-codes:write",
        [Permission.RulesRead] = "rules:read",
        [Permission.RulesWrite] = "rules:write",
        [Permission.RulesDeploy] = "rules:deploy",
        [Permission.RulesEvaluate] = "rules:evaluate",
        [Permission.RulesTest] = "rules:test",
        [Permission.RuleSetsRead] = "rule-sets:read",
        [Permission.RuleSetsWrite] = "rule-sets:write",
        [Permission.RuleSetsEvaluate] = "rule-sets:evaluate",
        [Permission.RuleEvaluationsRead] = "rule-evaluations:read",
        [Permission.SlaTargetsRead] = "sla-targets:read",
        [Permission.SlaTargetsWrite] = "sla-targets:write",
        [Permission.StatisticsRead] = "statistics:read",
        [Permission.SuspiciousActivityReportsRead] = "suspicious-activity-reports:read",
        [Permission.SuspiciousActivityReportsWrite] = "suspicious-activity-reports:write",
        [Permission.SuspiciousActivityReportsFile] = "suspicious-activity-reports:file",
        [Permission.SuspiciousActivityReportsExport] = "suspicious-activity-reports:export",
        [Permission.TransactionsRead] = "transactions:read",
        [Permission.TransactionsReveal] = "transactions:reveal",
        [Permission.TransactionSchemasRead] = "transaction-schemas:read",
        [Permission.TransactionSchemasWrite] = "transaction-schemas:write",
        [Permission.UsagesRead] = "usages:read",
        [Permission.UsersRead] = "users:read",
        [Permission.UsersWrite] = "users:write",
        [Permission.VelocityProfilesRead] = "velocity-profiles:read",
        [Permission.VelocityProfilesWrite] = "velocity-profiles:write",
        [Permission.WebhookConfigurationsRead] = "webhook-configurations:read",
        [Permission.WebhookConfigurationsWrite] = "webhook-configurations:write",
        [Permission.ApprovalPoliciesRead] = "approval-policies:read",
        [Permission.ApprovalPoliciesWrite] = "approval-policies:write",
        [Permission.SuspiciousActivityReportPoliciesRead] = "suspicious-activity-report-policies:read",
        [Permission.SuspiciousActivityReportPoliciesWrite] = "suspicious-activity-report-policies:write",
        [Permission.PermissionsRead] = "permissions:read",
        [Permission.RolesRead] = "roles:read",
        [Permission.RolesWrite] = "roles:write",
        [Permission.RolesAssign] = "roles:assign",
        [Permission.SubOrganizationsRead] = "sub-organizations:read",
        [Permission.SubOrganizationsWrite] = "sub-organizations:write",
        [Permission.PlatformAdminister] = "platform:administer",
        [Permission.SandboxRead] = "sandbox:read",
        [Permission.SandboxWrite] = "sandbox:write",
        [Permission.SandboxReset] = "sandbox:reset",
        [Permission.BillingEntitiesRead] = "billing-entities:read",
        [Permission.BillingEntitiesWrite] = "billing-entities:write",
        [Permission.BillingProfilesRead] = "billing-profiles:read",
        [Permission.BillingProfilesWrite] = "billing-profiles:write",
        [Permission.ExchangeRatesRead] = "exchange-rates:read",
        [Permission.ExchangeRatesWrite] = "exchange-rates:write",
        [Permission.CreditNotesRead] = "credit-notes:read",
        [Permission.CreditNotesWrite] = "credit-notes:write",
        [Permission.IncidentsRead] = "incidents:read",
        [Permission.IncidentsWrite] = "incidents:write",
        [Permission.MaintenanceWindowsRead] = "maintenance-windows:read",
        [Permission.MaintenanceWindowsWrite] = "maintenance-windows:write",
        [Permission.AvailabilityRead] = "availability:read",
        [Permission.AvailabilityCommitmentsRead] = "availability-commitments:read",
        [Permission.AvailabilityCommitmentsWrite] = "availability-commitments:write",
        [Permission.ServiceCreditsRead] = "service-credits:read",
        [Permission.ServiceCreditsIssue] = "service-credits:issue",
        [Permission.StatusProbesWrite] = "status-probes:write",
        [Permission.InvitationsRead] = "invitations:read",
        [Permission.InvitationsWrite] = "invitations:write",
        [Permission.UsersOffboard] = "users:offboard",
        [Permission.PaymentsRead] = "payments:read",
        [Permission.PaymentsCollect] = "payments:collect",
        [Permission.PaymentsReconcile] = "payments:reconcile",
        [Permission.PaymentsRefund] = "payments:refund",
        [Permission.PaymentMethodsRead] = "payment-methods:read",
        [Permission.PaymentMethodsWrite] = "payment-methods:write",
        [Permission.DunningRead] = "dunning:read",
        [Permission.RevenueRead] = "revenue:read",
        [Permission.ServiceCostRatesRead] = "service-cost-rates:read",
        [Permission.ServiceCostRatesWrite] = "service-cost-rates:write"
    }.ToFrozenDictionary();

    private static readonly FrozenDictionary<string, Permission> Values =
        Names.ToDictionary(x => x.Value, x => x.Key, StringComparer.OrdinalIgnoreCase).ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    public static string ToPermissionString(Permission permission)
    {
        if (!Names.TryGetValue(permission, out var name))
        {
            throw new ArgumentOutOfRangeException(nameof(permission), permission, "The permission has no declared name.");
        }

        return name;
    }

    public static string ToPolicyName(Permission permission)
    {
        return PolicyPrefix + ToPermissionString(permission);
    }

    public static Permission Parse(string value)
    {
        if (!TryParse(value, out var permission))
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown permission.");
        }

        return permission;
    }

    public static bool TryParse(string value, out Permission permission)
    {
        return Values.TryGetValue(value, out permission);
    }

    public static bool TryParsePolicyName(string policyName, out Permission permission)
    {
        if (!policyName.StartsWith(PolicyPrefix, StringComparison.Ordinal))
        {
            permission = default;
            return false;
        }

        return TryParse(policyName[PolicyPrefix.Length..], out permission);
    }
}
