using Core.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Swashbuckle.AspNetCore.Annotations;

namespace Presentation.Models.Plans;

public record PublishPlanVersionRequestDto
{
    [FromRoute]
    public required Guid PlanId { get; init; }

    [FromBody]
    public required PublishPlanVersionRequestBodyDto Body { get; init; }

    [BindNever]
    [SwaggerSchema(ReadOnly = true)]
    public Guid? PublishedByUserId { get; set; }
}

public record PublishPlanVersionRequestBodyDto
{
    public required decimal CostPerRequest { get; init; }

    public required Currency Currency { get; init; }

    public decimal PlatformFee { get; init; }
    public decimal MinimumCharge { get; init; }
    public List<PlanPriceRequestDto> Prices { get; init; } = [];
}
