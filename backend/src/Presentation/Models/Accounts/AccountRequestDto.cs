using Microsoft.AspNetCore.Mvc;

namespace Presentation.Models.Accounts;

public record AccountRequestDto
{
    [FromRoute]
    public required Guid OrganizationId { get; init; }
}
