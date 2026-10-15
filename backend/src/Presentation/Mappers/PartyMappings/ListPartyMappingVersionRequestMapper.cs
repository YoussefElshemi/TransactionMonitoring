using Core.Models.PartyMappings;
using Core.ValueObjects;
using Presentation.Models.PartyMappings;

namespace Presentation.Mappers.PartyMappings;

public static class ListPartyMappingVersionRequestMapper
{
    public static ListPartyMappingVersionRequest ToModel(this ListPartyMappingVersionRequestDto listPartyMappingVersionRequestDto)
    {
        return new ListPartyMappingVersionRequest
        {
            OrganizationId = new OrganizationId(listPartyMappingVersionRequestDto.OrganizationId),
            PageNumber = new PageNumber(listPartyMappingVersionRequestDto.PageNumber),
            PageSize = new PageSize(listPartyMappingVersionRequestDto.PageSize),
            FromDate = new DateRange(listPartyMappingVersionRequestDto.FromDate),
            ToDate = new DateRange(listPartyMappingVersionRequestDto.ToDate)
        };
    }

    public static ListPartyMappingVersionRequest ToModel(this ListMyPartyMappingVersionRequestDto listMyPartyMappingVersionRequestDto)
    {
        return new ListPartyMappingVersionRequest
        {
            OrganizationId = new OrganizationId(listMyPartyMappingVersionRequestDto.OrganizationId),
            PageNumber = new PageNumber(listMyPartyMappingVersionRequestDto.PageNumber),
            PageSize = new PageSize(listMyPartyMappingVersionRequestDto.PageSize),
            FromDate = new DateRange(listMyPartyMappingVersionRequestDto.FromDate),
            ToDate = new DateRange(listMyPartyMappingVersionRequestDto.ToDate)
        };
    }
}
