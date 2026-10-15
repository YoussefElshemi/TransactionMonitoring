using Core.Models.Adjustments;
using Core.ValueObjects;
using Presentation.Models.Adjustments;

namespace Presentation.Mappers.Adjustments;

public static class InvoiceAdjustmentMapper
{
    public static CreateInvoiceAdjustmentRequest ToModel(this CreateInvoiceAdjustmentRequestDto dto)
    {
        return new CreateInvoiceAdjustmentRequest
        {
            OrganizationId = new OrganizationId(dto.OrganizationId),
            Month = new Month(dto.Body.Month),
            Year = new Year(dto.Body.Year),
            Description = new InvoiceAdjustmentDescription(dto.Body.Description),
            Amount = new AdjustmentAmount(dto.Body.Amount),
            CreatedByUserId = dto.CreatedByUserId.HasValue ? new ApplicationUserId(dto.CreatedByUserId.Value) : null
        };
    }

    public static ListInvoiceAdjustmentRequest ToModel(this ListInvoiceAdjustmentRequestDto dto)
    {
        return new ListInvoiceAdjustmentRequest
        {
            OrganizationId = new OrganizationId(dto.OrganizationId),
            IncludeApplied = new IncludeApplied(dto.IncludeApplied ?? false),
            PageNumber = new PageNumber(dto.PageNumber),
            PageSize = new PageSize(dto.PageSize),
            FromDate = new DateRange(dto.FromDate),
            ToDate = new DateRange(dto.ToDate)
        };
    }

    public static InvoiceAdjustmentResponseDto ToDto(this InvoiceAdjustment invoiceAdjustment)
    {
        return new InvoiceAdjustmentResponseDto
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
            UpdatedAt = invoiceAdjustment.UpdatedAt
        };
    }
}
