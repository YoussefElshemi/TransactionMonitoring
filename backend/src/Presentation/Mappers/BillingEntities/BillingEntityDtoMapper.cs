using Core.Helpers;
using Core.Models.Billing;
using Core.ValueObjects;
using Presentation.Models.BillingEntities;

namespace Presentation.Mappers.BillingEntities;

public static class BillingEntityDtoMapper
{
    public static CreateBillingEntityRequest ToModel(this CreateBillingEntityRequestDto dto)
    {
        return new CreateBillingEntityRequest
        {
            LegalName = new LegalName(dto.Body.LegalName),
            Address = new PostalAddress(dto.Body.Address),
            CountryCode = new CountryCode(dto.Body.CountryCode),
            TaxIdentifier = string.IsNullOrWhiteSpace(dto.Body.TaxIdentifier) ? null : new TaxIdentifier(dto.Body.TaxIdentifier),
            InvoiceNumberPrefix = new NumberPrefix(dto.Body.InvoiceNumberPrefix),
            CreditNoteNumberPrefix = new NumberPrefix(dto.Body.CreditNoteNumberPrefix),
            Currency = dto.Body.Currency,
            CreatedByUserId = dto.CreatedByUserId.HasValue ? new ApplicationUserId(dto.CreatedByUserId.Value) : null
        };
    }

    public static UpdateBillingEntityRequest ToModel(this UpdateBillingEntityRequestDto dto)
    {
        return new UpdateBillingEntityRequest
        {
            BillingEntityId = new BillingEntityId(dto.BillingEntityId),
            LegalName = string.IsNullOrWhiteSpace(dto.Body.LegalName) ? null : new LegalName(dto.Body.LegalName),
            Address = string.IsNullOrWhiteSpace(dto.Body.Address) ? null : new PostalAddress(dto.Body.Address),
            CountryCode = string.IsNullOrWhiteSpace(dto.Body.CountryCode) ? null : new CountryCode(dto.Body.CountryCode),
            TaxIdentifier = string.IsNullOrWhiteSpace(dto.Body.TaxIdentifier) ? null : new TaxIdentifier(dto.Body.TaxIdentifier),
            Currency = dto.Body.Currency,
            IsEnabled = dto.Body.IsEnabled.HasValue ? new IsEnabled(dto.Body.IsEnabled.Value) : null
        };
    }

    public static ListBillingEntityRequest ToModel(this ListBillingEntityRequestDto dto)
    {
        return new ListBillingEntityRequest
        {
            IncludeDisabled = new IncludeDisabled(dto.IncludeDisabled ?? false),
            PageNumber = new PageNumber(dto.PageNumber),
            PageSize = new PageSize(dto.PageSize),
            FromDate = new DateRange(dto.FromDate),
            ToDate = new DateRange(dto.ToDate)
        };
    }

    public static BillingEntityResponseDto ToDto(this BillingEntity billingEntity)
    {
        return new BillingEntityResponseDto
        {
            BillingEntityId = billingEntity.BillingEntityId,
            LegalName = billingEntity.LegalName,
            Address = billingEntity.Address,
            CountryCode = billingEntity.CountryCode,
            TaxIdentifier = billingEntity.TaxIdentifier.HasValue ? billingEntity.TaxIdentifier.Value : null,
            InvoiceNumberPrefix = billingEntity.InvoiceNumberPrefix,
            CreditNoteNumberPrefix = billingEntity.CreditNoteNumberPrefix,
            Currency = CurrencyFormatter.Code(billingEntity.Currency),
            IsEnabled = billingEntity.IsEnabled,
            CreatedByUserId = billingEntity.CreatedByUserId.HasValue ? billingEntity.CreatedByUserId.Value : null,
            CreatedAt = billingEntity.CreatedAt,
            UpdatedAt = billingEntity.UpdatedAt
        };
    }
}
