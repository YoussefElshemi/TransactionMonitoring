using Microsoft.AspNetCore.Mvc;
using Presentation.Models.Common;
using Presentation.Models.Organizations;

namespace Presentation.Models.Invitations;

public record OrganizationInvitationRequestDto : OrganizationRequestDto
{
    [FromRoute]
    public required Guid InvitationId { get; init; }
}

// Not a subclass: ListInvitationRequestDto marks OrganizationId [BindNever], and this one binds it from the route.
public record OrganizationListInvitationRequestDto : PagedRequestDto
{
    [FromRoute]
    public new required Guid OrganizationId
    {
        get => base.OrganizationId;
        init => base.OrganizationId = value;
    }

    public string? Email { get; init; }

    public bool IncludeResolved { get; init; }
}
