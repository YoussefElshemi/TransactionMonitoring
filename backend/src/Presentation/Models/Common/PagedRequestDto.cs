using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Swashbuckle.AspNetCore.Annotations;

namespace Presentation.Models.Common;

public record PagedRequestDto : DatedRequestDto
{
    [FromQuery]
    public int PageNumber { get; init; } = 1;

    [FromQuery]
    public int PageSize { get; init; } = 10;

    [BindNever]
    [SwaggerSchema(ReadOnly = true)]
    public Guid OrganizationId { get; set; }

    public int GetPageCount(int totalCount)
    {
        return ((totalCount == 0 ? 1 : totalCount) + PageSize - 1) / PageSize;
    }
}