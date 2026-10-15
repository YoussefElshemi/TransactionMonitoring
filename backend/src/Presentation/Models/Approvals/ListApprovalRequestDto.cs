using Microsoft.AspNetCore.Mvc.ModelBinding;
using Presentation.Models.Common;
using Swashbuckle.AspNetCore.Annotations;

namespace Presentation.Models.Approvals;

public record ListApprovalRequestDto : PagedRequestDto
{
    public string? Status { get; init; }
    public string? Subject { get; init; }

    [BindNever]
    [SwaggerSchema(ReadOnly = true)]
    public Guid? RequestedByUserId { get; set; }
}
