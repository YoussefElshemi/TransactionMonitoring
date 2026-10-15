using Microsoft.AspNetCore.Mvc;

namespace Presentation.Models.Discounts;

public record ContractDiscountRequestDto
{
    [FromRoute]
    public required Guid OrganizationId { get; init; }

    [FromRoute]
    public required Guid ContractDiscountId { get; init; }
}
