using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Swashbuckle.AspNetCore.Annotations;

namespace Presentation.Models.Adjustments;

public record CreateInvoiceAdjustmentRequestDto
{
    [FromRoute]
    public required Guid OrganizationId { get; init; }

    [FromBody]
    public required CreateInvoiceAdjustmentRequestBodyDto Body { get; init; }

    [BindNever]
    [SwaggerSchema(ReadOnly = true)]
    public Guid? CreatedByUserId { get; set; }
}

public record CreateInvoiceAdjustmentRequestBodyDto
{
    public required int Month { get; init; }
    public required int Year { get; init; }

    public required string Description { get; init; }

    public required decimal Amount { get; init; }
}
