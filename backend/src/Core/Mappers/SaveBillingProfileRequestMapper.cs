using Core.Models.Billing;
using Core.ValueObjects;

namespace Core.Mappers;

public static class SaveBillingProfileRequestMapper
{
    public static BillingProfile ToModel(this SaveBillingProfileRequest request, TimeProvider timeProvider)
    {
        var now = timeProvider.GetUtcNow();

        return new BillingProfile
        {
            BillingProfileId = new BillingProfileId(Guid.NewGuid()),
            OrganizationId = request.OrganizationId,
            BillingEntityId = request.BillingEntityId,
            BillingEntity = null,
            LegalName = request.LegalName,
            ContactName = request.ContactName,
            ContactEmail = request.ContactEmail,
            Address = request.Address,
            CountryCode = request.CountryCode,
            TaxIdentifier = request.TaxIdentifier,

            TaxIdentifierVerifiedAt = null,

            ProviderCustomerReference = null,

            Currency = request.Currency,
            PaymentTerms = request.PaymentTerms,
            UpdatedByUserId = request.UpdatedByUserId,
            Locale = request.Locale,
            CreatedAt = new CreatedAt(now),
            UpdatedAt = new UpdatedAt(now),
            IsDeleted = new IsDeleted(false),
            DeletedAt = null
        };
    }

    public static BillingProfile ToModel(this SaveBillingProfileRequest request, BillingProfile existing, TimeProvider timeProvider)
    {
        return existing with
        {
            BillingEntityId = request.BillingEntityId,
            BillingEntity = null,
            LegalName = request.LegalName,
            ContactName = request.ContactName,
            ContactEmail = request.ContactEmail,
            Address = request.Address,
            CountryCode = request.CountryCode,
            TaxIdentifier = request.TaxIdentifier,
            TaxIdentifierVerifiedAt = request.TaxIdentifier == existing.TaxIdentifier && request.CountryCode == existing.CountryCode
                ? existing.TaxIdentifierVerifiedAt
                : null,
            Currency = request.Currency,
            PaymentTerms = request.PaymentTerms,
            UpdatedByUserId = request.UpdatedByUserId,
            Locale = request.Locale,
            UpdatedAt = new UpdatedAt(timeProvider.GetUtcNow())
        };
    }
}
