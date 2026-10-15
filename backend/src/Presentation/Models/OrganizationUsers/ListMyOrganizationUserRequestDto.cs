using Presentation.Models.Common;

namespace Presentation.Models.OrganizationUsers;

public record ListMyOrganizationUserRequestDto : PagedRequestDto
{
    public string? UserName { get; init; }
}
