using Core.Models.CaseLinks;
using Core.ValueObjects;
using Presentation.Models.CaseLinks;

namespace Presentation.Mappers.CaseLinks;

public static class ListCaseLinkRequestMapper
{
    public static ListCaseLinkRequest ToModel(this ListCaseLinkRequestDto listCaseLinkRequestDto)
    {
        return new ListCaseLinkRequest
        {
            CaseId = new CaseId(listCaseLinkRequestDto.CaseId),
            OrganizationId = new OrganizationId(listCaseLinkRequestDto.OrganizationId),
            PageNumber = new PageNumber(listCaseLinkRequestDto.PageNumber),
            PageSize = new PageSize(listCaseLinkRequestDto.PageSize),
            FromDate = new DateRange(listCaseLinkRequestDto.FromDate),
            ToDate = new DateRange(listCaseLinkRequestDto.ToDate)
        };
    }
}
