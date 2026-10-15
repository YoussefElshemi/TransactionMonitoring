using Microsoft.AspNetCore.Mvc;

namespace Presentation.Models.Payments;

public record CollectPaymentRequestDto
{
    [FromRoute]
    public required Guid OrganizationId { get; init; }

    [FromRoute]
    public required Guid InvoiceId { get; init; }

    [FromBody]
    public CollectPaymentRequestBodyDto? Body { get; init; }
}

public record CollectPaymentRequestBodyDto
{
    public string? PaymentMethodReference { get; init; }
}
