using Core.ValueObjects;

namespace Core.Models.Common;

public record PagedRequest : DatedRequest
{
    public required PageNumber PageNumber { get; init; }
    public required PageSize PageSize { get; init; }

    public PageCount GetPageCount(TotalCount totalCount)
    {
        return new PageCount(((totalCount == 0 ? 1 : totalCount) + PageSize - 1) / PageSize);
    }
}