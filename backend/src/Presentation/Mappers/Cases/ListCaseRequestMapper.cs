using Core.Enums;
using Core.Models.Cases;
using Core.ValueObjects;
using Presentation.Models.Cases;

namespace Presentation.Mappers.Cases;

public static class ListCaseRequestMapper
{
    public static ListCaseRequest ToModel(this ListCaseRequestDto listCaseRequestDto)
    {
        return new ListCaseRequest
        {
            OrganizationId = new OrganizationId(listCaseRequestDto.OrganizationId),
            FriendlyName = listCaseRequestDto.FriendlyName != null ? new FriendlyName(listCaseRequestDto.FriendlyName) : null,
            Status = listCaseRequestDto.Status != null ? Enum.Parse<CaseStatus>(listCaseRequestDto.Status, true) : null,
            Disposition = listCaseRequestDto.Disposition != null ? Enum.Parse<CaseDisposition>(listCaseRequestDto.Disposition, true) : null,
            Severity = listCaseRequestDto.Severity != null ? Enum.Parse<RuleSeverity>(listCaseRequestDto.Severity, true) : null,
            AssignedToUserId = listCaseRequestDto.AssignedToUserId.HasValue ? new ApplicationUserId(listCaseRequestDto.AssignedToUserId.Value) : null,
            QueueId = listCaseRequestDto.QueueId.HasValue ? new QueueId(listCaseRequestDto.QueueId.Value) : null,
            Unrouted = listCaseRequestDto.Unrouted.HasValue ? new Unrouted(listCaseRequestDto.Unrouted.Value) : null,
            QueueMemberUserId = listCaseRequestDto.QueueMemberUserId.HasValue ? new ApplicationUserId(listCaseRequestDto.QueueMemberUserId.Value) : null,
            Breached = listCaseRequestDto.Breached.HasValue ? new IsBreached(listCaseRequestDto.Breached.Value) : null,
            DueBefore = listCaseRequestDto.DueBefore.HasValue ? new DueAt(listCaseRequestDto.DueBefore.Value) : null,
            Escalated = listCaseRequestDto.Escalated.HasValue ? new IsEscalated(listCaseRequestDto.Escalated.Value) : null,
            PartyId = listCaseRequestDto.PartyId.HasValue ? new PartyId(listCaseRequestDto.PartyId.Value) : null,
            Query = SearchQuery.IsValid(listCaseRequestDto.Query) ? new SearchQuery(listCaseRequestDto.Query!) : null,
            PageNumber = new PageNumber(listCaseRequestDto.PageNumber),
            PageSize = new PageSize(listCaseRequestDto.PageSize),
            FromDate = new DateRange(listCaseRequestDto.FromDate),
            ToDate = new DateRange(listCaseRequestDto.ToDate)
        };
    }
}