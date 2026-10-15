using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Swashbuckle.AspNetCore.Annotations;

namespace Presentation.Models.Accounts;

public record AllocatePaymentRequestDto
{
    [FromRoute]
    public required Guid OrganizationId { get; init; }

    [FromBody]
    public required AllocatePaymentRequestBodyDto Body { get; init; }

    [BindNever]
    [SwaggerSchema(ReadOnly = true)]
    public Guid? AllocatedByUserId { get; set; }
}

public record AllocatePaymentRequestBodyDto
{
    public required Guid InvoiceId { get; init; }

    public decimal? Amount { get; init; }
}
