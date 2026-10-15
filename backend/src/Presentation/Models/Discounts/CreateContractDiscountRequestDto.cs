using Core.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Swashbuckle.AspNetCore.Annotations;

namespace Presentation.Models.Discounts;

public record CreateContractDiscountRequestDto
{
    [FromRoute]
    public required Guid OrganizationId { get; init; }

    [FromBody]
    public required CreateContractDiscountRequestBodyDto Body { get; init; }

    [BindNever]
    [SwaggerSchema(ReadOnly = true)]
    public Guid? CreatedByUserId { get; set; }
}

public record CreateContractDiscountRequestBodyDto
{
    public required DiscountType DiscountType { get; init; }

    public required decimal Value { get; init; }

    public required string Description { get; init; }

    public DateTimeOffset? EffectiveFrom { get; init; }

    public DateTimeOffset? EffectiveTo { get; init; }
}
