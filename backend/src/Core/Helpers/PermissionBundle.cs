using System.Collections.Frozen;
using Core.Enums;

namespace Core.Helpers;

public static class PermissionBundle
{
    private static readonly Permission[] User =
    [
        Permission.AlertsRead,
        Permission.ApprovalsRead,
        Permission.AttachmentsRead,
        Permission.AttachmentsWrite,
        Permission.BacktestsRead,
        Permission.CasesRead,
        Permission.CasesWrite,
        Permission.CasesResolve,
        Permission.CasesEvidencePack,
        Permission.CaseLinksRead,
        Permission.CaseLinksWrite,
        Permission.CaseNotesRead,
        Permission.CaseNotesWrite,
        Permission.DecisionsResolve,
        Permission.NotificationSubscriptionsRead,
        Permission.NotificationSubscriptionsWrite,
        Permission.OrganizationsRead,
        Permission.PartiesRead,
        Permission.QueuesClaim,
        Permission.RulesRead,
        Permission.RulesTest,
        Permission.RuleSetsRead,
        Permission.RuleEvaluationsRead,
        Permission.SandboxRead,
        Permission.StatisticsRead,
        Permission.SuspiciousActivityReportsRead,
        Permission.SuspiciousActivityReportsWrite,
        Permission.SuspiciousActivityReportsExport,
        Permission.TransactionsRead
    ];

    private static readonly Permission[] Manager =
    [
        ..User,
        Permission.ApiClientsRead,
        Permission.ApiClientsWrite,
        Permission.ApprovalsDecide,
        Permission.AuditLogsRead,
        Permission.AuditLogsExport,
        Permission.AuditLogsVerify,
        Permission.BacktestsWrite,
        Permission.CasesAssign,
        Permission.CasesDelete,
        Permission.EscalationRulesRead,
        Permission.EscalationRulesWrite,
        Permission.InvoicesRead,
        Permission.InvoicesWrite,

        Permission.PlansRead,
        Permission.SubscriptionsRead,
        Permission.ContractTermsRead,

        Permission.BillingProfilesRead,
        Permission.CreditNotesRead,

        Permission.PaymentsRead,
        Permission.DunningRead,

        Permission.PaymentMethodsRead,
        Permission.PaymentMethodsWrite,

        Permission.PaymentsCollect,

        Permission.AvailabilityRead,
        Permission.AvailabilityCommitmentsRead,
        Permission.ServiceCreditsRead,

        Permission.OrganizationsWrite,
        Permission.PartiesWrite,
        Permission.PartyMappingsRead,
        Permission.PartyMappingsWrite,
        Permission.QueuesRead,
        Permission.QueuesWrite,
        Permission.CaseReasonCodesRead,
        Permission.CaseReasonCodesWrite,
        Permission.RulesWrite,
        Permission.RulesDeploy,
        Permission.RulesEvaluate,
        Permission.RuleSetsWrite,
        Permission.RuleSetsEvaluate,
        Permission.SlaTargetsRead,
        Permission.SlaTargetsWrite,
        Permission.SuspiciousActivityReportsFile,
        Permission.TransactionsReveal,
        Permission.TransactionSchemasRead,
        Permission.TransactionSchemasWrite,
        Permission.UsagesRead,
        Permission.UsersRead,

        Permission.InvitationsRead,
        Permission.InvitationsWrite,
        Permission.UsersOffboard,
        Permission.VelocityProfilesRead,
        Permission.VelocityProfilesWrite,
        Permission.WebhookConfigurationsRead,
        Permission.WebhookConfigurationsWrite,
        Permission.ApprovalPoliciesRead,
        Permission.ApprovalPoliciesWrite,
        Permission.SuspiciousActivityReportPoliciesRead,
        Permission.SuspiciousActivityReportPoliciesWrite,
        Permission.PermissionsRead,
        Permission.RolesRead,
        Permission.RolesWrite,
        Permission.RolesAssign,
        Permission.SubOrganizationsRead,
        Permission.SubOrganizationsWrite,
        Permission.SandboxRead,
        Permission.SandboxWrite,
        Permission.SandboxReset
    ];

    private static readonly Permission[] Admin = [..Enum.GetValues<Permission>()];

    private static readonly Permission[] Auditor =
    [
        ..Manager.Where(x => x is not (
            Permission.ApiClientsWrite or
            Permission.ApprovalsDecide or
            Permission.AttachmentsWrite or
            Permission.BacktestsWrite or
            Permission.CasesWrite or
            Permission.CasesAssign or
            Permission.CasesResolve or
            Permission.CasesDelete or
            Permission.CaseLinksWrite or
            Permission.CaseNotesWrite or
            Permission.CaseReasonCodesWrite or
            Permission.DecisionsResolve or
            Permission.EscalationRulesWrite or
            Permission.InvoicesWrite or
            Permission.BillingProfilesWrite or
            Permission.BillingEntitiesWrite or
            Permission.ExchangeRatesWrite or
            Permission.CreditNotesWrite or
            Permission.NotificationSubscriptionsWrite or
            Permission.OrganizationsWrite or
            Permission.PartiesWrite or
            Permission.PartyMappingsWrite or
            Permission.QueuesWrite or
            Permission.QueuesClaim or
            Permission.RulesWrite or
            Permission.RulesDeploy or
            Permission.RulesEvaluate or
            Permission.RulesTest or
            Permission.RuleSetsWrite or
            Permission.RuleSetsEvaluate or
            Permission.SlaTargetsWrite or
            Permission.SuspiciousActivityReportsWrite or
            Permission.SuspiciousActivityReportsFile or
            Permission.TransactionsReveal or
            Permission.TransactionSchemasWrite or
            Permission.UsersWrite or
            Permission.InvitationsWrite or
            Permission.UsersOffboard or
            Permission.VelocityProfilesWrite or
            Permission.WebhookConfigurationsWrite or
            Permission.ApprovalPoliciesWrite or
            Permission.SuspiciousActivityReportPoliciesWrite or
            Permission.RolesWrite or
            Permission.RolesAssign or
            Permission.SubOrganizationsWrite or

            Permission.PaymentMethodsWrite or
            Permission.PaymentsCollect or

            Permission.SandboxWrite or
            Permission.SandboxReset)
        )
    ];

    private static readonly FrozenDictionary<UserRole, FrozenSet<Permission>> Bundles =
        new Dictionary<UserRole, FrozenSet<Permission>>
        {
            [UserRole.User] = User.ToFrozenSet(),
            [UserRole.Manager] = Manager.ToFrozenSet(),
            [UserRole.Admin] = Admin.ToFrozenSet(),
            [UserRole.Auditor] = Auditor.ToFrozenSet()
        }.ToFrozenDictionary();

    public static IReadOnlySet<Permission> For(UserRole userRole)
    {
        if (!Bundles.TryGetValue(userRole, out var permissions))
        {
            throw new ArgumentOutOfRangeException(nameof(userRole), userRole, "The role has no declared permission bundle.");
        }

        return permissions;
    }

    public static IReadOnlySet<Permission> For(IEnumerable<UserRole> userRoles)
    {
        return userRoles.SelectMany(For).ToFrozenSet();
    }

    public static bool IsGrantableToCustomRole(Permission permission)
    {
        return permission != Permission.PlatformAdminister;
    }
}
