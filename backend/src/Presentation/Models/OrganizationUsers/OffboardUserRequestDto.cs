using Microsoft.AspNetCore.Mvc;

namespace Presentation.Models.OrganizationUsers;

public record OffboardUserRequestDto : OrganizationUserRequestDto
{
    [FromBody]
    public required OffboardUserRequestBodyDto Body { get; init; }
}

public record OffboardUserRequestBodyDto
{
    public Guid? SuccessorUserId { get; init; }
}

public record OffboardedUserResponseDto
{
    public required Guid UserId { get; init; }
    public required Guid? SuccessorUserId { get; init; }
    public required int ReassignedCaseCount { get; init; }
    public required int QueueMembershipCount { get; init; }
    public required int RevokedRoleAssignmentCount { get; init; }
    public required int RevokedInvitationCount { get; init; }
}
