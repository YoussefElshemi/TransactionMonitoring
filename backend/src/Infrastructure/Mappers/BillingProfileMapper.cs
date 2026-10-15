using Core.Enums;
using Core.Models.Billing;
using Core.ValueObjects;
using Infrastructure.Entities;

namespace Infrastructure.Mappers;

public static class BillingProfileMapper
{
    public static BillingProfile ToModel(this BillingProfileEntity billingProfileEntity)
    {
        return new BillingProfile
        {
            BillingProfileId = new BillingProfileId(billingProfileEntity.BillingProfileId),
            OrganizationId = new OrganizationId(billingProfileEntity.OrganizationId),
            BillingEntityId = new BillingEntityId(billingProfileEntity.BillingEntityId),
            BillingEntity = billingProfileEntity.BillingEntity?.ToModel(),
            LegalName = new LegalName(billingProfileEntity.LegalName),
            ContactName = new ContactName(billingProfileEntity.ContactName),
            ContactEmail = new Email(billingProfileEntity.ContactEmail),
            Address = new PostalAddress(billingProfileEntity.Address),
            CountryCode = new CountryCode(billingProfileEntity.CountryCode),
            TaxIdentifier = billingProfileEntity.TaxIdentifier is null ? null : new TaxIdentifier(billingProfileEntity.TaxIdentifier),
            TaxIdentifierVerifiedAt = billingProfileEntity.TaxIdentifierVerifiedAt.HasValue ? new VerifiedAt(billingProfileEntity.TaxIdentifierVerifiedAt.Value) : null,
            ProviderCustomerReference = billingProfileEntity.ProviderCustomerReference is null
                ? null
                : new ProviderCustomerReference(billingProfileEntity.ProviderCustomerReference),
            Currency = (Currency)billingProfileEntity.CurrencyId,
            PaymentTerms = new PaymentTerms(billingProfileEntity.PaymentTerms),
            UpdatedByUserId = billingProfileEntity.UpdatedByUserId.HasValue ? new ApplicationUserId(billingProfileEntity.UpdatedByUserId.Value) : null,
            Locale = billingProfileEntity.Locale is not null ? new Locale(billingProfileEntity.Locale) : null,
            CreatedAt = new CreatedAt(billingProfileEntity.CreatedAt),
            UpdatedAt = new UpdatedAt(billingProfileEntity.UpdatedAt),
            IsDeleted = new IsDeleted(billingProfileEntity.IsDeleted),
            DeletedAt = billingProfileEntity.DeletedAt.HasValue ? new DeletedAt(billingProfileEntity.DeletedAt.Value) : null
        };
    }

    public static BillingProfileEntity ToEntity(this BillingProfile billingProfile)
    {
        return new BillingProfileEntity
        {
            BillingProfileId = billingProfile.BillingProfileId,
            OrganizationId = billingProfile.OrganizationId,
            BillingEntityId = billingProfile.BillingEntityId,
            LegalName = billingProfile.LegalName,
            ContactName = billingProfile.ContactName,
            ContactEmail = billingProfile.ContactEmail,
            Address = billingProfile.Address,
            CountryCode = billingProfile.CountryCode,
            TaxIdentifier = billingProfile.TaxIdentifier.HasValue ? billingProfile.TaxIdentifier.Value : null,
            TaxIdentifierVerifiedAt = billingProfile.TaxIdentifierVerifiedAt.HasValue ? billingProfile.TaxIdentifierVerifiedAt.Value : null,
            ProviderCustomerReference = billingProfile.ProviderCustomerReference?.ToString(),
            CurrencyId = (int)billingProfile.Currency,
            PaymentTerms = billingProfile.PaymentTerms,
            UpdatedByUserId = billingProfile.UpdatedByUserId.HasValue ? billingProfile.UpdatedByUserId.Value : null,
            Locale = billingProfile.Locale?.ToString(),
            CreatedAt = billingProfile.CreatedAt,
            UpdatedAt = billingProfile.UpdatedAt,
            IsDeleted = billingProfile.IsDeleted,
            DeletedAt = billingProfile.DeletedAt.HasValue ? billingProfile.DeletedAt.Value : null
        };
    }
}
