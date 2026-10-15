using Core.Models.Common;
using Presentation.Models.Common;

namespace Presentation.Mappers.Common;

public static class PagedResponseMapper
{
    public static PagedResponseDto<T2> ToDto<T1, T2>(this PagedResponse<T1> pagedResponse, Func<T1, T2> mapper)
    {
        return pagedResponse.ToDto(pagedResponse.Data.Select(mapper).ToArray());
    }

    public static PagedResponseDto<T2> ToDto<T1, T2>(this PagedResponse<T1> pagedResponse, T2[] data)
    {
        return new PagedResponseDto<T2>
        {
            Metadata = new PagedMetadataDto
            {
                PageNumber = pagedResponse.PageNumber,
                PageSize = pagedResponse.PageSize,
                PageCount = pagedResponse.PageCount,
                TotalCount = pagedResponse.TotalCount,
                HasMore = pagedResponse.HasMore
            },
            Data = data
        };
    }
}