using Core.Models.ApiClients;
using Core.ValueObjects;
using Presentation.Models.ApiClients;

namespace Presentation.Mappers.ApiClients;

public static class ListApiClientRequestMapper
{
    public static ListApiClientRequest ToModel(this ListApiClientRequestDto listApiClientRequestDto)
    {
        return new ListApiClientRequest
        {
            OrganizationId = new OrganizationId(listApiClientRequestDto.OrganizationId),
            FriendlyName = !string.IsNullOrWhiteSpace(listApiClientRequestDto.FriendlyName) ? new FriendlyName(listApiClientRequestDto.FriendlyName) : null,
            PageNumber = new PageNumber(listApiClientRequestDto.PageNumber),
            PageSize = new PageSize(listApiClientRequestDto.PageSize),
            FromDate = new DateRange(listApiClientRequestDto.FromDate),
            ToDate = new DateRange(listApiClientRequestDto.ToDate)
        };
    }
}
