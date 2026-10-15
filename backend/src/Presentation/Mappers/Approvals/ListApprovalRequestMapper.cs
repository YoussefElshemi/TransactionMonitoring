using Core.Enums;
using Core.Models.Approvals;
using Core.ValueObjects;
using Presentation.Models.Approvals;

namespace Presentation.Mappers.Approvals;

public static class ListApprovalRequestMapper
{
    public static ListApprovalRequest ToModel(this ListApprovalRequestDto listApprovalRequestDto)
    {
        return new ListApprovalRequest
        {
            OrganizationId = new OrganizationId(listApprovalRequestDto.OrganizationId),
            Status = !string.IsNullOrWhiteSpace(listApprovalRequestDto.Status)
                ? Enum.Parse<ApprovalStatus>(listApprovalRequestDto.Status, true)
                : null,
            Subject = !string.IsNullOrWhiteSpace(listApprovalRequestDto.Subject)
                ? Enum.Parse<ApprovalSubject>(listApprovalRequestDto.Subject, true)
                : null,
            RequestedByUserId = listApprovalRequestDto.RequestedByUserId.HasValue
                ? new ApplicationUserId(listApprovalRequestDto.RequestedByUserId.Value)
                : null,
            PageNumber = new PageNumber(listApprovalRequestDto.PageNumber),
            PageSize = new PageSize(listApprovalRequestDto.PageSize),
            FromDate = new DateRange(listApprovalRequestDto.FromDate),
            ToDate = new DateRange(listApprovalRequestDto.ToDate)
        };
    }
}
