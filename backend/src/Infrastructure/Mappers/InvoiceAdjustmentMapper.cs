using Core.Models.Adjustments;
using Core.ValueObjects;
using Infrastructure.Entities;

namespace Infrastructure.Mappers;

public static class InvoiceAdjustmentMapper
{
    public static InvoiceAdjustment ToModel(this InvoiceAdjustmentEntity invoiceAdjustmentEntity)
    {
        return new InvoiceAdjustment
        {
            InvoiceAdjustmentId = new InvoiceAdjustmentId(invoiceAdjustmentEntity.InvoiceAdjustmentId),
            OrganizationId = new OrganizationId(invoiceAdjustmentEntity.OrganizationId),
            Month = new Month(invoiceAdjustmentEntity.Month),
            Year = new Year(invoiceAdjustmentEntity.Year),
            Description = new InvoiceAdjustmentDescription(invoiceAdjustmentEntity.Description),
            Amount = new AdjustmentAmount(invoiceAdjustmentEntity.Amount),
            AppliedToInvoiceId = invoiceAdjustmentEntity.AppliedToInvoiceId.HasValue ? new InvoiceId(invoiceAdjustmentEntity.AppliedToInvoiceId.Value) : null,
            CreatedByUserId = invoiceAdjustmentEntity.CreatedByUserId.HasValue ? new ApplicationUserId(invoiceAdjustmentEntity.CreatedByUserId.Value) : null,
            CreatedAt = new CreatedAt(invoiceAdjustmentEntity.CreatedAt),
            UpdatedAt = new UpdatedAt(invoiceAdjustmentEntity.UpdatedAt),
            IsDeleted = new IsDeleted(invoiceAdjustmentEntity.IsDeleted),
            DeletedAt = invoiceAdjustmentEntity.DeletedAt.HasValue ? new DeletedAt(invoiceAdjustmentEntity.DeletedAt.Value) : null
        };
    }

    public static InvoiceAdjustmentEntity ToEntity(this InvoiceAdjustment invoiceAdjustment)
    {
        return new InvoiceAdjustmentEntity
        {
            InvoiceAdjustmentId = invoiceAdjustment.InvoiceAdjustmentId,
            OrganizationId = invoiceAdjustment.OrganizationId,
            Month = invoiceAdjustment.Month,
            Year = invoiceAdjustment.Year,
            Description = invoiceAdjustment.Description,
            Amount = invoiceAdjustment.Amount,
            AppliedToInvoiceId = invoiceAdjustment.AppliedToInvoiceId.HasValue ? invoiceAdjustment.AppliedToInvoiceId.Value : null,
            CreatedByUserId = invoiceAdjustment.CreatedByUserId.HasValue ? invoiceAdjustment.CreatedByUserId.Value : null,
            CreatedAt = invoiceAdjustment.CreatedAt,
            UpdatedAt = invoiceAdjustment.UpdatedAt,
            IsDeleted = invoiceAdjustment.IsDeleted,
            DeletedAt = invoiceAdjustment.DeletedAt
        };
    }
}
