namespace Core.Enums;

// Persisted as a foreign key from RolePermissions: never renumber or reuse; retired members stay.
public enum Permission
{
    AlertsRead = 1,

    ApiClientsRead = 2,
    ApiClientsWrite = 3,

    ApprovalsRead = 4,

    ApprovalsDecide = 5,

    AttachmentsRead = 6,
    AttachmentsWrite = 7,

    AuditLogsRead = 8,

    AuditLogsExport = 9,
    AuditLogsVerify = 10,

    BacktestsRead = 11,
    BacktestsWrite = 12,

    CasesRead = 13,
    CasesWrite = 14,

    CasesAssign = 15,
    CasesResolve = 16,
    CasesDelete = 17,

    CasesEvidencePack = 18,

    CaseLinksRead = 19,
    CaseLinksWrite = 20,

    CaseNotesRead = 21,
    CaseNotesWrite = 22,

    DecisionsResolve = 23,

    EscalationRulesRead = 24,
    EscalationRulesWrite = 25,

    InvoicesRead = 26,
    InvoicesWrite = 27,

    NotificationSubscriptionsRead = 28,
    NotificationSubscriptionsWrite = 29,

    OrganizationsRead = 30,
    OrganizationsWrite = 31,

    PartiesRead = 32,
    PartiesWrite = 33,

    PartyMappingsRead = 34,
    PartyMappingsWrite = 35,

    QueuesRead = 36,
    QueuesWrite = 37,

    QueuesClaim = 38,

    CaseReasonCodesRead = 39,
    CaseReasonCodesWrite = 40,

    RulesRead = 41,
    RulesWrite = 42,

    RulesDeploy = 43,
    RulesEvaluate = 44,
    RulesTest = 45,

    RuleSetsRead = 46,
    RuleSetsWrite = 47,
    RuleSetsEvaluate = 48,

    RuleEvaluationsRead = 49,

    SlaTargetsRead = 50,
    SlaTargetsWrite = 51,

    StatisticsRead = 52,

    SuspiciousActivityReportsRead = 53,
    SuspiciousActivityReportsWrite = 54,

    SuspiciousActivityReportsFile = 55,
    SuspiciousActivityReportsExport = 56,

    TransactionsRead = 57,

    TransactionsReveal = 58,

    TransactionSchemasRead = 59,
    TransactionSchemasWrite = 60,

    UsagesRead = 61,

    UsersRead = 62,
    UsersWrite = 63,

    VelocityProfilesRead = 64,
    VelocityProfilesWrite = 65,

    WebhookConfigurationsRead = 66,
    WebhookConfigurationsWrite = 67,

    ApprovalPoliciesRead = 68,
    ApprovalPoliciesWrite = 69,

    SuspiciousActivityReportPoliciesRead = 70,
    SuspiciousActivityReportPoliciesWrite = 71,

    PermissionsRead = 72,
    RolesRead = 73,
    RolesWrite = 74,
    RolesAssign = 75,

    SubOrganizationsRead = 76,
    SubOrganizationsWrite = 77,

    PlatformAdminister = 78,

    SandboxRead = 79,
    SandboxWrite = 80,
    SandboxReset = 81,

    PlansRead = 82,
    PlansWrite = 83,

    SubscriptionsRead = 84,
    SubscriptionsWrite = 85,

    ContractTermsRead = 86,
    ContractTermsWrite = 87,

    BillingEntitiesRead = 88,
    BillingEntitiesWrite = 89,

    BillingProfilesRead = 90,
    BillingProfilesWrite = 91,

    ExchangeRatesRead = 92,
    ExchangeRatesWrite = 93,

    CreditNotesRead = 94,
    CreditNotesWrite = 95,

    IncidentsRead = 96,
    IncidentsWrite = 97,

    MaintenanceWindowsRead = 99,
    MaintenanceWindowsWrite = 100,

    AvailabilityRead = 101,

    AvailabilityCommitmentsRead = 102,
    AvailabilityCommitmentsWrite = 103,

    ServiceCreditsRead = 104,
    ServiceCreditsIssue = 105,

    StatusProbesWrite = 108,

    InvitationsRead = 109,
    InvitationsWrite = 110,

    UsersOffboard = 111,

    PaymentsRead = 112,

    PaymentsCollect = 113,

    PaymentsReconcile = 114,

    PaymentsRefund = 115,

    PaymentMethodsRead = 116,
    PaymentMethodsWrite = 117,

    DunningRead = 118,

    RevenueRead = 119,

    ServiceCostRatesRead = 120,
    ServiceCostRatesWrite = 121
}
