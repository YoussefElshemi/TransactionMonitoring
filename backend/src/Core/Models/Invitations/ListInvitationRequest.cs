using Core.Models.Common;
using Core.ValueObjects;

namespace Core.Models.Invitations;

public record ListInvitationRequest : PagedRequest
{
    public required OrganizationId OrganizationId { get; init; }
    public required Email? Email { get; init; }

    public required IncludeResolved IncludeResolved { get; init; }
}
