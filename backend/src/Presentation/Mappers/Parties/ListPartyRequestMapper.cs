using Core.Enums;
using Core.Models.Parties;
using Core.ValueObjects;
using Presentation.Models.Parties;

namespace Presentation.Mappers.Parties;

public static class ListPartyRequestMapper
{
    public static ListPartyRequest ToModel(this ListPartyRequestDto listPartyRequestDto)
    {
        return new ListPartyRequest
        {
            OrganizationId = new OrganizationId(listPartyRequestDto.OrganizationId),
            Identifier = string.IsNullOrWhiteSpace(listPartyRequestDto.Identifier) ? null : new PartyIdentifier(listPartyRequestDto.Identifier),
            DisplayName = string.IsNullOrWhiteSpace(listPartyRequestDto.DisplayName) ? null : new PartyDisplayName(listPartyRequestDto.DisplayName),
            Role = string.IsNullOrWhiteSpace(listPartyRequestDto.Role) ? null : Enum.Parse<PartyRole>(listPartyRequestDto.Role, true),
            Query = SearchQuery.IsValid(listPartyRequestDto.Query) ? new SearchQuery(listPartyRequestDto.Query!) : null,
            PageNumber = new PageNumber(listPartyRequestDto.PageNumber),
            PageSize = new PageSize(listPartyRequestDto.PageSize),
            FromDate = new DateRange(listPartyRequestDto.FromDate),
            ToDate = new DateRange(listPartyRequestDto.ToDate)
        };
    }

    public static ListPartyTransactionRequest ToModel(this ListPartyTransactionRequestDto listPartyTransactionRequestDto)
    {
        return new ListPartyTransactionRequest
        {
            OrganizationId = new OrganizationId(listPartyTransactionRequestDto.OrganizationId),
            PartyId = new PartyId(listPartyTransactionRequestDto.PartyId),
            Role = string.IsNullOrWhiteSpace(listPartyTransactionRequestDto.Role) ? null : Enum.Parse<PartyRole>(listPartyTransactionRequestDto.Role, true),
            PageNumber = new PageNumber(listPartyTransactionRequestDto.PageNumber),
            PageSize = new PageSize(listPartyTransactionRequestDto.PageSize),
            FromDate = new DateRange(listPartyTransactionRequestDto.FromDate),
            ToDate = new DateRange(listPartyTransactionRequestDto.ToDate)
        };
    }

    public static ListPartyCaseRequest ToModel(this ListPartyCaseRequestDto listPartyCaseRequestDto)
    {
        return new ListPartyCaseRequest
        {
            OrganizationId = new OrganizationId(listPartyCaseRequestDto.OrganizationId),
            PartyId = new PartyId(listPartyCaseRequestDto.PartyId),
            Status = string.IsNullOrWhiteSpace(listPartyCaseRequestDto.Status) ? null : Enum.Parse<CaseStatus>(listPartyCaseRequestDto.Status, true),
            PageNumber = new PageNumber(listPartyCaseRequestDto.PageNumber),
            PageSize = new PageSize(listPartyCaseRequestDto.PageSize),
            FromDate = new DateRange(listPartyCaseRequestDto.FromDate),
            ToDate = new DateRange(listPartyCaseRequestDto.ToDate)
        };
    }
}
