using Core.Enums;
using Core.Interfaces.Services;
using Core.Models.Cases;
using Core.Models.Parties;
using Core.Models.Search;
using Core.ValueObjects;

namespace Core.Services;

public class SearchService(IPartyService partyService, ICaseService caseService) : ISearchService
{
    public async Task<SearchResults> SearchAsync(SearchRequest searchRequest, CancellationToken stoppingToken)
    {
        // Sequential: both reads share one scoped DbContext, which cannot run two queries at once.
        var parties = searchRequest.Types.Contains(SearchResultType.Party)
            ? await partyService.ListAsync(ToListPartyRequest(searchRequest), stoppingToken)
            : null;

        var cases = searchRequest.Types.Contains(SearchResultType.Case)
            ? await caseService.ListAsync(ToListCaseRequest(searchRequest), stoppingToken)
            : null;

        return new SearchResults
        {
            Query = searchRequest.Query,
            Parties = parties,
            Cases = cases
        };
    }

    private static ListPartyRequest ToListPartyRequest(SearchRequest searchRequest)
    {
        return new ListPartyRequest
        {
            OrganizationId = searchRequest.OrganizationId,
            Query = searchRequest.Query,
            Identifier = null,
            DisplayName = null,
            Role = null,
            PageNumber = new PageNumber(1),
            PageSize = searchRequest.Limit,
            FromDate = new DateRange(DateOnly.MinValue),
            ToDate = new DateRange(DateOnly.MaxValue)
        };
    }

    private static ListCaseRequest ToListCaseRequest(SearchRequest searchRequest)
    {
        return new ListCaseRequest
        {
            OrganizationId = searchRequest.OrganizationId,
            Query = searchRequest.Query,
            PermissionFilter = searchRequest.CasePermissionFilter,
            FriendlyName = null,
            Status = null,
            Disposition = null,
            Severity = null,
            AssignedToUserId = null,
            QueueId = null,
            Unrouted = null,
            QueueMemberUserId = null,
            Breached = null,
            DueBefore = null,
            Escalated = null,
            PartyId = null,
            PageNumber = new PageNumber(1),
            PageSize = searchRequest.Limit,
            FromDate = new DateRange(DateOnly.MinValue),
            ToDate = new DateRange(DateOnly.MaxValue)
        };
    }
}
