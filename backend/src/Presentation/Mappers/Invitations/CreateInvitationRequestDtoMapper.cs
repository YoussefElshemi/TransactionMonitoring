using Core.Helpers;
using Core.Models.Invitations;
using Core.ValueObjects;
using Presentation.Models.Invitations;

namespace Presentation.Mappers.Invitations;

public static class CreateInvitationRequestDtoMapper
{
    public static CreateInvitationRequest ToModel(
        this CreateInvitationRequestBodyDto createInvitationRequestDto,
        OrganizationId organizationId,
        ApplicationUserId invitedByUserId)
    {
        return new CreateInvitationRequest
        {
            OrganizationId = organizationId,
            Email = new Email(createInvitationRequestDto.Email.Trim()),
            UserRole = RoleHelper.Parse(createInvitationRequestDto.Role),
            SubOrganizationId = createInvitationRequestDto.SubOrganizationId.HasValue
                ? new SubOrganizationId(createInvitationRequestDto.SubOrganizationId.Value)
                : null,
            InvitedByUserId = invitedByUserId
        };
    }

    public static AcceptInvitationRequest ToModel(this AcceptInvitationRequestDto acceptInvitationRequestDto)
    {
        return new AcceptInvitationRequest
        {
            InvitationToken = new InvitationToken(acceptInvitationRequestDto.Token),
            UserName = !string.IsNullOrWhiteSpace(acceptInvitationRequestDto.UserName)
                ? new UserName(acceptInvitationRequestDto.UserName)
                : null,
            Password = !string.IsNullOrWhiteSpace(acceptInvitationRequestDto.Password)
                ? new Password(acceptInvitationRequestDto.Password)
                : null
        };
    }
}
