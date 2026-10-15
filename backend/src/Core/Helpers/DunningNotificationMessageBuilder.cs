using Core.Constants;
using Core.Enums;
using Core.Localization;
using Core.Models.Invoices;
using Core.Models.Notifications;
using Core.Models.Payments;

namespace Core.Helpers;

public static class DunningNotificationMessageBuilder
{
    public static NotificationMessage Build(
        Invoice invoice,
        InvoiceSettlement invoiceSettlement,
        DunningStage stage,
        Translator translator,
        DateTimeOffset timestamp,
        int restrictionProposalDays)
    {
        var reference = invoice.InvoiceNumber?.ToString() ?? invoice.InvoiceId.ToString();

        var outstanding = CurrencyFormatter.Format(invoice.Currency, invoiceSettlement.OutstandingAmount);

        var dueAt = translator.Moment(invoice.DueAt!.Value);

        var (subjectKey, bodyKey) = Keys(stage);

        return new NotificationMessage
        {
            Subject = translator.Text(subjectKey, reference),

            // String.Format ignores unused arguments, so every rung can be formatted with the same call.
            Body = translator.Text(bodyKey, reference, dueAt, outstanding, restrictionProposalDays),
            CaseId = null,
            Event = WebhookEvents.InvoiceOverdue,
            Timestamp = timestamp
        };
    }

    private static (string SubjectKey, string BodyKey) Keys(DunningStage stage)
    {
        return stage switch
        {
            DunningStage.SecondNotice => (TranslationKeys.NotificationArrearsSecondSubject, TranslationKeys.NotificationArrearsSecondBody),
            DunningStage.FinalNotice => (TranslationKeys.NotificationArrearsFinalSubject, TranslationKeys.NotificationArrearsFinalBody),
            DunningStage.RestrictionProposed => (TranslationKeys.NotificationArrearsProposedSubject, TranslationKeys.NotificationArrearsProposedBody),

            _ => (TranslationKeys.NotificationArrearsFirstSubject, TranslationKeys.NotificationArrearsFirstBody)
        };
    }
}
