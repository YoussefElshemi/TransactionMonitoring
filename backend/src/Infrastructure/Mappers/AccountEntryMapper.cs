using Core.Enums;
using Core.Models.Accounts;
using Core.ValueObjects;
using Infrastructure.Entities;

namespace Infrastructure.Mappers;

public static class AccountEntryMapper
{
    public static AccountEntry ToModel(this AccountEntryEntity accountEntryEntity)
    {
        return new AccountEntry
        {
            AccountEntryId = new AccountEntryId(accountEntryEntity.AccountEntryId),
            OrganizationId = new OrganizationId(accountEntryEntity.OrganizationId),
            Currency = (Currency)accountEntryEntity.CurrencyId,
            Amount = new EntryAmount(accountEntryEntity.Amount),
            Reason = (AccountEntryReason)accountEntryEntity.AccountEntryReasonId,
            PaymentId = accountEntryEntity.PaymentId.HasValue ? new PaymentId(accountEntryEntity.PaymentId.Value) : null,
            InvoiceId = accountEntryEntity.InvoiceId.HasValue ? new InvoiceId(accountEntryEntity.InvoiceId.Value) : null,
            CreditNoteId = accountEntryEntity.CreditNoteId.HasValue ? new CreditNoteId(accountEntryEntity.CreditNoteId.Value) : null,
            Description = new EntryDescription(accountEntryEntity.Description),
            CreatedByUserId = accountEntryEntity.CreatedByUserId.HasValue
                ? new ApplicationUserId(accountEntryEntity.CreatedByUserId.Value)
                : null,
            CreatedAt = new CreatedAt(accountEntryEntity.CreatedAt),
            UpdatedAt = new UpdatedAt(accountEntryEntity.UpdatedAt)
        };
    }

    public static AccountEntryEntity ToEntity(this AccountEntry accountEntry)
    {
        return new AccountEntryEntity
        {
            AccountEntryId = accountEntry.AccountEntryId,
            OrganizationId = accountEntry.OrganizationId,
            CurrencyId = (int)accountEntry.Currency,
            Amount = accountEntry.Amount,
            AccountEntryReasonId = (int)accountEntry.Reason,
            PaymentId = accountEntry.PaymentId.HasValue ? accountEntry.PaymentId.Value : null,
            InvoiceId = accountEntry.InvoiceId.HasValue ? accountEntry.InvoiceId.Value : null,
            CreditNoteId = accountEntry.CreditNoteId.HasValue ? accountEntry.CreditNoteId.Value : null,
            Description = accountEntry.Description,
            CreatedByUserId = accountEntry.CreatedByUserId.HasValue ? accountEntry.CreatedByUserId.Value : null,
            CreatedAt = accountEntry.CreatedAt,
            UpdatedAt = accountEntry.UpdatedAt
        };
    }
}
