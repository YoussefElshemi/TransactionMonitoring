using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Swashbuckle.AspNetCore.Annotations;

namespace Presentation.Models.Commitments;

public record CreateCommitmentRequestDto
{
    [FromRoute]
    public required Guid OrganizationId { get; init; }

    [FromBody]
    public required CreateCommitmentRequestBodyDto Body { get; init; }

    [BindNever]
    [SwaggerSchema(ReadOnly = true)]
    public Guid? CreatedByUserId { get; set; }
}

public record CreateCommitmentRequestBodyDto
{
    public required decimal Amount { get; init; }

    public DateTimeOffset? EffectiveFrom { get; init; }

    public required DateTimeOffset EffectiveTo { get; init; }
}
