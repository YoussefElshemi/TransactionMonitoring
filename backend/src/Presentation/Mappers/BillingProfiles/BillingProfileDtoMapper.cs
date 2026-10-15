using Core.Helpers;
using Core.Models.Billing;
using Core.ValueObjects;
using Presentation.Models.BillingProfiles;

namespace Presentation.Mappers.BillingProfiles;

public static class BillingProfileDtoMapper
{
    public static SaveBillingProfileRequest ToModel(this SaveBillingProfileRequestDto dto)
    {
        return new SaveBillingProfileRequest
        {
            OrganizationId = new OrganizationId(dto.OrganizationId),
            BillingEntityId = new BillingEntityId(dto.Body.BillingEntityId),
            LegalName = new LegalName(dto.Body.LegalName),
            ContactName = new ContactName(dto.Body.ContactName),
            ContactEmail = new Email(dto.Body.ContactEmail),
            Address = new PostalAddress(dto.Body.Address),
            CountryCode = new CountryCode(dto.Body.CountryCode),
            TaxIdentifier = string.IsNullOrWhiteSpace(dto.Body.TaxIdentifier) ? null : new TaxIdentifier(dto.Body.TaxIdentifier),
            Currency = dto.Body.Currency,
            PaymentTerms = new PaymentTerms(dto.Body.PaymentTerms),
            UpdatedByUserId = dto.UpdatedByUserId.HasValue ? new ApplicationUserId(dto.UpdatedByUserId.Value) : null,
            Locale = string.IsNullOrWhiteSpace(dto.Body.Locale) ? null : new Locale(dto.Body.Locale)
        };
    }

    public static BillingProfileResponseDto ToDto(this BillingProfile billingProfile)
    {
        return new BillingProfileResponseDto
        {
            BillingProfileId = billingProfile.BillingProfileId,
            OrganizationId = billingProfile.OrganizationId,
            BillingEntityId = billingProfile.BillingEntityId,
            BillingEntityLegalName = billingProfile.BillingEntity is not null ? billingProfile.BillingEntity.LegalName : null,
            LegalName = billingProfile.LegalName,
            ContactName = billingProfile.ContactName,
            ContactEmail = billingProfile.ContactEmail,
            Address = billingProfile.Address,
            CountryCode = billingProfile.CountryCode,
            TaxIdentifier = billingProfile.TaxIdentifier.HasValue ? billingProfile.TaxIdentifier.Value : null,
            TaxIdentifierVerifiedAt = billingProfile.TaxIdentifierVerifiedAt.HasValue ? billingProfile.TaxIdentifierVerifiedAt.Value : null,
            Currency = CurrencyFormatter.Code(billingProfile.Currency),
            PaymentTerms = billingProfile.PaymentTerms,
            UpdatedByUserId = billingProfile.UpdatedByUserId.HasValue ? billingProfile.UpdatedByUserId.Value : null,
            Locale = billingProfile.Locale?.ToString(),
            CreatedAt = billingProfile.CreatedAt,
            UpdatedAt = billingProfile.UpdatedAt
        };
    }
}
