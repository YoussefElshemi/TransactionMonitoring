using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Swashbuckle.AspNetCore.Annotations;

namespace Presentation.Models.Accounts;

public record RefundBalanceRequestDto
{
    [FromRoute]
    public required Guid OrganizationId { get; init; }

    [FromBody]
    public required RefundBalanceRequestBodyDto Body { get; init; }

    [BindNever]
    [SwaggerSchema(ReadOnly = true)]
    public Guid? RefundedByUserId { get; set; }
}

public record RefundBalanceRequestBodyDto
{
    public required string Currency { get; init; }
    public required decimal Amount { get; init; }

    public required string Reason { get; init; }
}
