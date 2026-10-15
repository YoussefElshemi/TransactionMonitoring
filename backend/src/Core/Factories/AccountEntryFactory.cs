using Core.Enums;
using Core.Models.Accounts;
using Core.Models.Payments;
using Core.ValueObjects;

namespace Core.Factories;

public static class AccountEntryFactory
{
    public static AccountEntry Receipt(Payment payment, EntryDescription description, TimeProvider timeProvider)
    {
        return Create(
            payment.OrganizationId,
            payment.Currency,
            new EntryAmount(payment.Amount),
            AccountEntryReason.Receipt,
            payment.PaymentId,
            invoiceId: null,
            creditNoteId: null,
            description,
            createdByUserId: null,
            timeProvider);
    }

    public static AccountEntry Allocation(
        OrganizationId organizationId,
        Currency currency,
        decimal amount,
        InvoiceId invoiceId,
        PaymentId? paymentId,
        EntryDescription description,
        ApplicationUserId? allocatedByUserId,
        TimeProvider timeProvider)
    {
        return Create(
            organizationId,
            currency,
            new EntryAmount(-Math.Abs(amount)),
            AccountEntryReason.Allocation,
            paymentId,
            invoiceId,
            creditNoteId: null,
            description,
            allocatedByUserId,
            timeProvider);
    }

    public static AccountEntry Return(Payment payment, EntryDescription description, TimeProvider timeProvider)
    {
        return Create(
            payment.OrganizationId,
            payment.Currency,
            new EntryAmount(-payment.Amount),
            AccountEntryReason.Return,
            payment.PaymentId,
            invoiceId: null,
            creditNoteId: null,
            description,
            createdByUserId: null,
            timeProvider);
    }

    public static AccountEntry Refund(
        OrganizationId organizationId,
        Currency currency,
        decimal amount,
        EntryDescription description,
        ApplicationUserId? refundedByUserId,
        TimeProvider timeProvider)
    {
        return Create(
            organizationId,
            currency,
            new EntryAmount(-Math.Abs(amount)),
            AccountEntryReason.Refund,
            paymentId: null,
            invoiceId: null,
            creditNoteId: null,
            description,
            refundedByUserId,
            timeProvider);
    }

    public static AccountEntry CreditNote(
        OrganizationId organizationId,
        Currency currency,
        decimal amount,
        InvoiceId invoiceId,
        CreditNoteId creditNoteId,
        EntryDescription description,
        ApplicationUserId? createdByUserId,
        TimeProvider timeProvider)
    {
        return Create(
            organizationId,
            currency,
            new EntryAmount(Math.Abs(amount)),
            AccountEntryReason.CreditNote,
            paymentId: null,
            invoiceId,
            creditNoteId,
            description,
            createdByUserId,
            timeProvider);
    }

    private static AccountEntry Create(
        OrganizationId organizationId,
        Currency currency,
        EntryAmount amount,
        AccountEntryReason reason,
        PaymentId? paymentId,
        InvoiceId? invoiceId,
        CreditNoteId? creditNoteId,
        EntryDescription description,
        ApplicationUserId? createdByUserId,
        TimeProvider timeProvider)
    {
        var now = timeProvider.GetUtcNow();

        return new AccountEntry
        {
            AccountEntryId = new AccountEntryId(Guid.NewGuid()),
            OrganizationId = organizationId,
            Currency = currency,
            Amount = amount,
            Reason = reason,
            PaymentId = paymentId,
            InvoiceId = invoiceId,
            CreditNoteId = creditNoteId,
            Description = description,
            CreatedByUserId = createdByUserId,
            CreatedAt = new CreatedAt(now),
            UpdatedAt = new UpdatedAt(now)
        };
    }
}
