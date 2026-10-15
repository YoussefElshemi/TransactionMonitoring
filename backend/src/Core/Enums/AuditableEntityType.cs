namespace Core.Enums;

public enum AuditableEntityType
{
    Rule = 1,
    RuleSet = 2,
    Case = 3,
    CaseNote = 4,

    // Reserved: historical AuditLogs rows still carry this EntityTypeId. Never reuse or remove.
    ApiKey = 5,

    WebhookConfiguration = 6,
    Organization = 7,
    ApplicationUser = 8,
    ApiClient = 9,
    ApiClientSecret = 10,
    Transaction = 11,
    Decision = 12,
    TransactionSchema = 13,
    PartyMapping = 14,
    Party = 15,
    VelocityProfile = 16,
    Alert = 17,
    CaseReasonCode = 18,
    Queue = 19,
    EscalationRule = 20,
    Approval = 21,
    Attachment = 22,
    CaseLink = 23,
    NotificationSubscription = 24,
    SuspiciousActivityReport = 25,
    Role = 26,
    RoleAssignment = 27,
    SubOrganization = 28,
    RateLimitOverride = 29,
    Plan = 30,
    Subscription = 31,

    ContractDiscount = 32,
    InvoiceAdjustment = 33,
    Commitment = 34,

    BillingEntity = 35,
    BillingProfile = 36,
    CreditNote = 37,

    Incident = 38,
    MaintenanceWindow = 39,

    // Retired. Its persisted value stays reserved and must never be reused.
    AvailabilityCommitment = 40,
    OnCallShift = 41,

    Invitation = 42
}
