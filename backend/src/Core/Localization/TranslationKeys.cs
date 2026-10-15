namespace Core.Localization;

public static class TranslationKeys
{
    public const string NotificationCaseOpenedSubject = "notification.case.opened.subject";
    public const string NotificationCaseOpenedBody = "notification.case.opened.body";
    public const string NotificationCaseBreachedSubject = "notification.case.breached.subject";
    public const string NotificationCaseBreachedBody = "notification.case.breached.body";
    public const string NotificationFilingOverdueSubject = "notification.filing.overdue.subject";
    public const string NotificationFilingOverdueBody = "notification.filing.overdue.body";
    public const string NotificationWebhookDisabledSubject = "notification.webhook.disabled.subject";
    public const string NotificationWebhookDisabledBody = "notification.webhook.disabled.body";
    public const string NotificationIncidentSubject = "notification.incident.subject";
    public const string NotificationIncidentBody = "notification.incident.body";
    public const string NotificationMaintenanceSubject = "notification.maintenance.subject";
    public const string NotificationMaintenanceBody = "notification.maintenance.body";
    public const string NotificationInvitationSubject = "notification.invitation.subject";
    public const string NotificationInvitationBody = "notification.invitation.body";

    public const string NotificationArrearsFirstSubject = "notification.arrears.first.subject";
    public const string NotificationArrearsFirstBody = "notification.arrears.first.body";
    public const string NotificationArrearsSecondSubject = "notification.arrears.second.subject";
    public const string NotificationArrearsSecondBody = "notification.arrears.second.body";
    public const string NotificationArrearsFinalSubject = "notification.arrears.final.subject";
    public const string NotificationArrearsFinalBody = "notification.arrears.final.body";
    public const string NotificationArrearsProposedSubject = "notification.arrears.proposed.subject";
    public const string NotificationArrearsProposedBody = "notification.arrears.proposed.body";

    public const string SeverityInfo = "severity.info";
    public const string SeverityLow = "severity.low";
    public const string SeverityMedium = "severity.medium";
    public const string SeverityHigh = "severity.high";
    public const string SeverityCritical = "severity.critical";

    public const string FilingStatusDraft = "filingStatus.draft";
    public const string FilingStatusPendingApproval = "filingStatus.pendingApproval";
    public const string FilingStatusApproved = "filingStatus.approved";
    public const string FilingStatusFiled = "filingStatus.filed";
    public const string FilingStatusAcknowledged = "filingStatus.acknowledged";
    public const string FilingStatusNotFiled = "filingStatus.notFiled";

    public const string IncidentStatusInvestigating = "incidentStatus.investigating";
    public const string IncidentStatusIdentified = "incidentStatus.identified";
    public const string IncidentStatusMonitoring = "incidentStatus.monitoring";
    public const string IncidentStatusResolved = "incidentStatus.resolved";
    public const string IncidentImpactNone = "incidentImpact.none";
    public const string IncidentImpactMinor = "incidentImpact.minor";
    public const string IncidentImpactMajor = "incidentImpact.major";
    public const string IncidentImpactCritical = "incidentImpact.critical";

    public const string DocumentInvoiceTitle = "document.invoice.title";
    public const string DocumentInvoiceDraftTitle = "document.invoice.draftTitle";
    public const string DocumentCreditNoteTitle = "document.creditNote.title";
    public const string DocumentBillingPeriod = "document.billingPeriod";
    public const string DocumentIssueDate = "document.issueDate";
    public const string DocumentDueDate = "document.dueDate";
    public const string DocumentCurrency = "document.currency";
    public const string DocumentNotYetIssued = "document.notYetIssued";
    public const string DocumentDueInDays = "document.dueInDays";
    public const string DocumentAgainstInvoice = "document.againstInvoice";
    public const string DocumentReason = "document.reason";
    public const string DocumentFrom = "document.from";
    public const string DocumentFor = "document.for";
    public const string DocumentTaxIdentifier = "document.taxIdentifier";
    public const string DocumentColumnDescription = "document.column.description";
    public const string DocumentColumnQuantity = "document.column.quantity";
    public const string DocumentColumnUnitPrice = "document.column.unitPrice";
    public const string DocumentColumnAmount = "document.column.amount";
    public const string DocumentSubtotal = "document.subtotal";
    public const string DocumentSubtotalCredited = "document.subtotalCredited";
    public const string DocumentTax = "document.tax";
    public const string DocumentTotal = "document.total";
    public const string DocumentTotalCredited = "document.totalCredited";
    public const string DocumentConversionNote = "document.conversionNote";
    public const string DocumentCreditTermsNote = "document.creditTermsNote";

    public const string DocumentReceiptTitle = "document.receipt.title";
    public const string DocumentReceiptDate = "document.receipt.date";
    public const string DocumentPaymentMethod = "document.receipt.paymentMethod";
    public const string DocumentAmountReceived = "document.receipt.amountReceived";
    public const string DocumentReceiptUnappliedNote = "document.receipt.unappliedNote";
    public const string DocumentReceiptNote = "document.receipt.note";

    public const string PaymentMethodCard = "paymentMethod.card";
    public const string PaymentMethodDirectDebit = "paymentMethod.directDebit";
    public const string PaymentMethodBankTransfer = "paymentMethod.bankTransfer";

    public const string TaxTreatmentStandard = "taxTreatment.standard";
    public const string TaxTreatmentReverseCharge = "taxTreatment.reverseCharge";
    public const string TaxTreatmentOutOfScope = "taxTreatment.outOfScope";

    public const string LineResourceUnclassified = "line.resource.unclassified";
    public const string LineResourceRuleEvaluation = "line.resource.ruleEvaluation";
    public const string LineResourceRuleSetEvaluation = "line.resource.ruleSetEvaluation";
    public const string LineResourceRuleTest = "line.resource.ruleTest";
    public const string LineResourceBacktest = "line.resource.backtest";
    public const string LineResourceSandboxWrite = "line.resource.sandboxWrite";
    public const string LineNotPriced = "line.notPriced";
    public const string LineIncludedAllowance = "line.includedAllowance";
    public const string LineBandRange = "line.band.range";
    public const string LineBandAndAbove = "line.band.andAbove";
    public const string LinePlatformFee = "line.platformFee";
    public const string LineMinimumTopUp = "line.minimumTopUp";
    public const string LineCommitmentDrawdown = "line.commitmentDrawdown";

    public const string TaxLineDomestic = "taxLine.domestic";
    public const string TaxLineReverseCharge = "taxLine.reverseCharge";
    public const string TaxLineOneStopShop = "taxLine.oneStopShop";
    public const string TaxLineNoJurisdiction = "taxLine.noJurisdiction";
    public const string TaxLineOutOfScope = "taxLine.outOfScope";

    public static readonly IReadOnlyList<string> All =
    [
        NotificationCaseOpenedSubject, NotificationCaseOpenedBody,
        NotificationCaseBreachedSubject, NotificationCaseBreachedBody,
        NotificationFilingOverdueSubject, NotificationFilingOverdueBody,
        NotificationWebhookDisabledSubject, NotificationWebhookDisabledBody,
        NotificationIncidentSubject, NotificationIncidentBody,
        NotificationMaintenanceSubject, NotificationMaintenanceBody,
        NotificationInvitationSubject, NotificationInvitationBody,
        NotificationArrearsFirstSubject, NotificationArrearsFirstBody,
        NotificationArrearsSecondSubject, NotificationArrearsSecondBody,
        NotificationArrearsFinalSubject, NotificationArrearsFinalBody,
        NotificationArrearsProposedSubject, NotificationArrearsProposedBody,
        SeverityInfo, SeverityLow, SeverityMedium, SeverityHigh, SeverityCritical,
        FilingStatusDraft, FilingStatusPendingApproval, FilingStatusApproved,
        FilingStatusFiled, FilingStatusAcknowledged, FilingStatusNotFiled,
        IncidentStatusInvestigating, IncidentStatusIdentified, IncidentStatusMonitoring, IncidentStatusResolved,
        IncidentImpactNone, IncidentImpactMinor, IncidentImpactMajor, IncidentImpactCritical,
        DocumentInvoiceTitle, DocumentInvoiceDraftTitle, DocumentCreditNoteTitle,
        DocumentBillingPeriod, DocumentIssueDate, DocumentDueDate, DocumentCurrency,
        DocumentReceiptTitle, DocumentReceiptDate, DocumentPaymentMethod, DocumentAmountReceived,
        DocumentReceiptUnappliedNote, DocumentReceiptNote,
        PaymentMethodCard, PaymentMethodDirectDebit, PaymentMethodBankTransfer,
        DocumentNotYetIssued, DocumentDueInDays, DocumentAgainstInvoice, DocumentReason,
        DocumentFrom, DocumentFor, DocumentTaxIdentifier,
        DocumentColumnDescription, DocumentColumnQuantity, DocumentColumnUnitPrice, DocumentColumnAmount,
        DocumentSubtotal, DocumentSubtotalCredited, DocumentTax, DocumentTotal, DocumentTotalCredited,
        DocumentConversionNote, DocumentCreditTermsNote,
        TaxTreatmentStandard, TaxTreatmentReverseCharge, TaxTreatmentOutOfScope,
        LineResourceUnclassified, LineResourceRuleEvaluation, LineResourceRuleSetEvaluation,
        LineResourceRuleTest, LineResourceBacktest, LineResourceSandboxWrite,
        LineNotPriced, LineIncludedAllowance, LineBandRange, LineBandAndAbove,
        LinePlatformFee, LineMinimumTopUp, LineCommitmentDrawdown,
        TaxLineDomestic, TaxLineReverseCharge, TaxLineOneStopShop, TaxLineNoJurisdiction, TaxLineOutOfScope
    ];
}
