using Microsoft.AspNetCore.Mvc;

namespace Presentation.Models.Payments;

public record PaymentRequestDto
{
    [FromRoute]
    public required Guid OrganizationId { get; init; }

    [FromRoute]
    public required Guid PaymentId { get; init; }
}
