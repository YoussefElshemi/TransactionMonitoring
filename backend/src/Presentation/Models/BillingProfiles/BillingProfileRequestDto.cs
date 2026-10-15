using Microsoft.AspNetCore.Mvc;

namespace Presentation.Models.BillingProfiles;

public record BillingProfileRequestDto
{
    [FromRoute]
    public required Guid OrganizationId { get; init; }
}
