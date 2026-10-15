using Core.Models.Adjustments;
using Core.ValueObjects;

namespace Core.Mappers;

public static class CreateInvoiceAdjustmentRequestMapper
{
    public static InvoiceAdjustment ToModel(this CreateInvoiceAdjustmentRequest request, TimeProvider timeProvider)
    {
        var now = timeProvider.GetUtcNow();

        return new InvoiceAdjustment
        {
            InvoiceAdjustmentId = new InvoiceAdjustmentId(Guid.NewGuid()),
            OrganizationId = request.OrganizationId,
            Month = request.Month,
            Year = request.Year,
            Description = request.Description,
            Amount = request.Amount,
            AppliedToInvoiceId = null,
            CreatedByUserId = request.CreatedByUserId,
            CreatedAt = new CreatedAt(now),
            UpdatedAt = new UpdatedAt(now),
            IsDeleted = new IsDeleted(false),
            DeletedAt = null
        };
    }
}
