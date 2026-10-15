using Core.Models.Invitations;
using Core.ValueObjects;
using Presentation.Models.Invitations;

namespace Presentation.Mappers.Invitations;

public static class ListInvitationRequestMapper
{
    public static ListInvitationRequest ToModel(this ListInvitationRequestDto listInvitationRequestDto)
    {
        return new ListInvitationRequest
        {
            OrganizationId = new OrganizationId(listInvitationRequestDto.OrganizationId),
            Email = !string.IsNullOrWhiteSpace(listInvitationRequestDto.Email) ? new Email(listInvitationRequestDto.Email) : null,
            IncludeResolved = new IncludeResolved(listInvitationRequestDto.IncludeResolved),
            PageNumber = new PageNumber(listInvitationRequestDto.PageNumber),
            PageSize = new PageSize(listInvitationRequestDto.PageSize),
            FromDate = new DateRange(listInvitationRequestDto.FromDate),
            ToDate = new DateRange(listInvitationRequestDto.ToDate)
        };
    }
}
