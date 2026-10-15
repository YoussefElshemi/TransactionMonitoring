using Core.Enums;
using Core.Models.Common;
using Core.Models.Permissions;
using Core.Models.Search;
using Core.ValueObjects;
using Presentation.Models.Search;

namespace Presentation.Mappers.Search;

public static class SearchDtoMapper
{
    public static SearchRequest ToModel(
        this SearchRequestDto searchRequestDto,
        List<SearchResultType> types,
        PermissionFilter? casePermissionFilter)
    {
        return new SearchRequest
        {
            OrganizationId = new OrganizationId(searchRequestDto.OrganizationId),
            Query = new SearchQuery(searchRequestDto.Query!),
            Types = types,
            Limit = new PageSize(searchRequestDto.Limit),
            CasePermissionFilter = casePermissionFilter
        };
    }

    public static List<SearchResultType> ToTypes(this SearchRequestDto searchRequestDto)
    {
        return searchRequestDto.Types is { Length: > 0 }
            ? searchRequestDto.Types.Select(x => Enum.Parse<SearchResultType>(x, true)).Distinct().ToList()
            : Enum.GetValues<SearchResultType>().ToList();
    }

    public static SearchSectionDto<TDto> ToSection<TModel, TDto>(this PagedResponse<TModel> pagedResponse, TDto[] data)
    {
        return new SearchSectionDto<TDto>
        {
            TotalCount = pagedResponse.TotalCount,
            HasMore = pagedResponse.HasMore,
            Data = data
        };
    }
}
