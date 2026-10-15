using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Swashbuckle.AspNetCore.Annotations;

namespace Presentation.Models.Payments;

public record RecordPaymentRequestDto
{
    [FromRoute]
    public required Guid OrganizationId { get; init; }

    [FromBody]
    public required RecordPaymentRequestBodyDto Body { get; init; }

    [BindNever]
    [SwaggerSchema(ReadOnly = true)]
    public Guid? RecordedByUserId { get; set; }
}

public record RecordPaymentRequestBodyDto
{
    public required decimal Amount { get; init; }
    public required string Currency { get; init; }

    public required DateTimeOffset ReceivedAt { get; init; }

    public string? RemittanceReference { get; init; }

    public Guid? InvoiceId { get; init; }
}
