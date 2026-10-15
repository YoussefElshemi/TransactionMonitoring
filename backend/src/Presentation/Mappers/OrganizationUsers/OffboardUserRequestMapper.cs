using Core.Models.Users;
using Core.ValueObjects;
using Presentation.Models.OrganizationUsers;

namespace Presentation.Mappers.OrganizationUsers;

public static class OffboardUserRequestMapper
{
    public static OffboardUserRequest ToModel(
        this OffboardUserRequestDto offboardUserRequestDto, ApplicationUserId offboardedByUserId)
    {
        return new OffboardUserRequest
        {
            OrganizationId = new OrganizationId(offboardUserRequestDto.OrganizationId),
            UserId = new ApplicationUserId(offboardUserRequestDto.UserId),
            SuccessorUserId = offboardUserRequestDto.Body.SuccessorUserId.HasValue
                ? new ApplicationUserId(offboardUserRequestDto.Body.SuccessorUserId.Value)
                : null,
            OffboardedByUserId = offboardedByUserId
        };
    }

    public static OffboardedUserResponseDto ToDto(this OffboardedUser offboardedUser)
    {
        return new OffboardedUserResponseDto
        {
            UserId = offboardedUser.UserId,
            SuccessorUserId = offboardedUser.SuccessorUserId.HasValue ? (Guid)offboardedUser.SuccessorUserId.Value : null,
            ReassignedCaseCount = offboardedUser.ReassignedCaseCount,
            QueueMembershipCount = offboardedUser.QueueMembershipCount,
            RevokedRoleAssignmentCount = offboardedUser.RevokedRoleAssignmentCount,
            RevokedInvitationCount = offboardedUser.RevokedInvitationCount
        };
    }
}
