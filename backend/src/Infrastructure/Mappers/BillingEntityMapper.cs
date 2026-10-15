using Core.Enums;
using Core.Models.Billing;
using Core.ValueObjects;
using Infrastructure.Entities;

namespace Infrastructure.Mappers;

public static class BillingEntityMapper
{
    public static BillingEntity ToModel(this BillingEntityEntity billingEntityEntity)
    {
        return new BillingEntity
        {
            BillingEntityId = new BillingEntityId(billingEntityEntity.BillingEntityId),
            LegalName = new LegalName(billingEntityEntity.LegalName),
            Address = new PostalAddress(billingEntityEntity.Address),
            CountryCode = new CountryCode(billingEntityEntity.CountryCode),
            TaxIdentifier = billingEntityEntity.TaxIdentifier is null ? null : new TaxIdentifier(billingEntityEntity.TaxIdentifier),
            InvoiceNumberPrefix = new NumberPrefix(billingEntityEntity.InvoiceNumberPrefix),
            CreditNoteNumberPrefix = new NumberPrefix(billingEntityEntity.CreditNoteNumberPrefix),
            Currency = (Currency)billingEntityEntity.CurrencyId,
            IsEnabled = new IsEnabled(billingEntityEntity.IsEnabled),
            CreatedByUserId = billingEntityEntity.CreatedByUserId.HasValue ? new ApplicationUserId(billingEntityEntity.CreatedByUserId.Value) : null,
            CreatedAt = new CreatedAt(billingEntityEntity.CreatedAt),
            UpdatedAt = new UpdatedAt(billingEntityEntity.UpdatedAt),
            IsDeleted = new IsDeleted(billingEntityEntity.IsDeleted),
            DeletedAt = billingEntityEntity.DeletedAt.HasValue ? new DeletedAt(billingEntityEntity.DeletedAt.Value) : null
        };
    }

    public static BillingEntityEntity ToEntity(this BillingEntity billingEntity)
    {
        return new BillingEntityEntity
        {
            BillingEntityId = billingEntity.BillingEntityId,
            LegalName = billingEntity.LegalName,
            Address = billingEntity.Address,
            CountryCode = billingEntity.CountryCode,
            TaxIdentifier = billingEntity.TaxIdentifier.HasValue ? billingEntity.TaxIdentifier.Value : null,
            InvoiceNumberPrefix = billingEntity.InvoiceNumberPrefix,
            CreditNoteNumberPrefix = billingEntity.CreditNoteNumberPrefix,
            CurrencyId = (int)billingEntity.Currency,
            IsEnabled = billingEntity.IsEnabled,
            CreatedByUserId = billingEntity.CreatedByUserId.HasValue ? billingEntity.CreatedByUserId.Value : null,
            CreatedAt = billingEntity.CreatedAt,
            UpdatedAt = billingEntity.UpdatedAt,
            IsDeleted = billingEntity.IsDeleted,
            DeletedAt = billingEntity.DeletedAt.HasValue ? billingEntity.DeletedAt.Value : null
        };
    }
}
