using Microsoft.AspNetCore.Mvc;
using Presentation.Models.Organizations;

namespace Presentation.Models.OrganizationUsers;

public record OrganizationUserRequestDto : OrganizationRequestDto
{
    [FromRoute]
    public required Guid UserId { get; init; }
}