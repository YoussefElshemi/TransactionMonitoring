using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Swashbuckle.AspNetCore.Annotations;

namespace Presentation.Models.Approvals;

public record ApprovalRequestDto
{
    [FromRoute]
    public required Guid ApprovalId { get; init; }

    [BindNever]
    [SwaggerSchema(ReadOnly = true)]
    public Guid OrganizationId { get; set; }
}

public record ApproveApprovalRequestDto : ApprovalRequestDto
{
    [FromBody]
    public ReviewApprovalRequestBodyDto? Body { get; init; }
}

// Separate type so it gets its own validator.
public record RejectApprovalRequestDto : ApprovalRequestDto
{
    [FromBody]
    public required ReviewApprovalRequestBodyDto Body { get; init; }
}

public record WithdrawApprovalRequestDto : ApprovalRequestDto
{
    [FromBody]
    public ReviewApprovalRequestBodyDto? Body { get; init; }
}

public record ReviewApprovalRequestBodyDto
{
    public string? Note { get; init; }
}
