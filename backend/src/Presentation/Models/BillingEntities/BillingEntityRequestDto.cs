using Microsoft.AspNetCore.Mvc;

namespace Presentation.Models.BillingEntities;

public record BillingEntityRequestDto
{
    [FromRoute]
    public required Guid BillingEntityId { get; init; }
}
