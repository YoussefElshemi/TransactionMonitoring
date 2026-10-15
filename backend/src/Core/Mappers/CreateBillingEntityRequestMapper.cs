using Core.Models.Billing;
using Core.ValueObjects;

namespace Core.Mappers;

public static class CreateBillingEntityRequestMapper
{
    public static BillingEntity ToModel(this CreateBillingEntityRequest request, TimeProvider timeProvider)
    {
        var now = timeProvider.GetUtcNow();

        return new BillingEntity
        {
            BillingEntityId = new BillingEntityId(Guid.NewGuid()),
            LegalName = request.LegalName,
            Address = request.Address,
            CountryCode = request.CountryCode,
            TaxIdentifier = request.TaxIdentifier,
            InvoiceNumberPrefix = request.InvoiceNumberPrefix,
            CreditNoteNumberPrefix = request.CreditNoteNumberPrefix,
            Currency = request.Currency,
            IsEnabled = new IsEnabled(true),
            CreatedByUserId = request.CreatedByUserId,
            CreatedAt = new CreatedAt(now),
            UpdatedAt = new UpdatedAt(now),
            IsDeleted = new IsDeleted(false),
            DeletedAt = null
        };
    }
}
