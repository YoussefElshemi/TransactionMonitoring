using Core.Models.Invitations;
using Core.ValueObjects;
using Presentation.Models.Invitations;

namespace Presentation.Mappers.Invitations;

public static class OrganizationListInvitationRequestMapper
{
    public static ListInvitationRequest ToModel(this OrganizationListInvitationRequestDto organizationListInvitationRequestDto)
    {
        return new ListInvitationRequest
        {
            OrganizationId = new OrganizationId(organizationListInvitationRequestDto.OrganizationId),
            Email = !string.IsNullOrWhiteSpace(organizationListInvitationRequestDto.Email) ? new Email(organizationListInvitationRequestDto.Email) : null,
            IncludeResolved = new IncludeResolved(organizationListInvitationRequestDto.IncludeResolved),
            PageNumber = new PageNumber(organizationListInvitationRequestDto.PageNumber),
            PageSize = new PageSize(organizationListInvitationRequestDto.PageSize),
            FromDate = new DateRange(organizationListInvitationRequestDto.FromDate),
            ToDate = new DateRange(organizationListInvitationRequestDto.ToDate)
        };
    }
}
