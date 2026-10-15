using Microsoft.AspNetCore.Mvc;

namespace Presentation.Models.Payments;

public record CreatePaymentMethodSessionRequestDto
{
    [FromRoute]
    public required Guid OrganizationId { get; init; }

    [FromBody]
    public required CreatePaymentMethodSessionRequestBodyDto Body { get; init; }
}

public record CreatePaymentMethodSessionRequestBodyDto
{
    public required string ReturnUrl { get; init; }
    public required string CancelUrl { get; init; }
}
