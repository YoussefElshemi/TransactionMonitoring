using Microsoft.AspNetCore.Mvc;

namespace Presentation.Models.Adjustments;

public record InvoiceAdjustmentRequestDto
{
    [FromRoute]
    public required Guid OrganizationId { get; init; }

    [FromRoute]
    public required Guid InvoiceAdjustmentId { get; init; }
}
