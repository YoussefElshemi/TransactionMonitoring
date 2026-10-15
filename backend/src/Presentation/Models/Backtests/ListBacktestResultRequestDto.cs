using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Presentation.Models.Common;
using Swashbuckle.AspNetCore.Annotations;

namespace Presentation.Models.BackTests;

public record ListBackTestResultRequestDto : PagedRequestDto
{
    [FromRoute]
    public required Guid BackTestId { get; init; }
    public bool? ChangedOnly { get; init; }
    [BindNever]
    [SwaggerSchema(ReadOnly = true)]
    public new required Guid OrganizationId
    {
        get => base.OrganizationId;
        set => base.OrganizationId = value;
    }
}
