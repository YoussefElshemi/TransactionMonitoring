using Core.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Swashbuckle.AspNetCore.Annotations;

namespace Presentation.Models.Revenue;

public record CreateServiceCostRateRequestDto
{
    [FromBody]
    public required CreateServiceCostRateRequestBodyDto Body { get; init; }

    [BindNever]
    [SwaggerSchema(ReadOnly = true)]
    public Guid? CreatedByUserId { get; set; }
}

public record CreateServiceCostRateRequestBodyDto
{
    public required BillableResource BillableResource { get; init; }

    public required decimal UnitCost { get; init; }

    public required Currency Currency { get; init; }

    public DateTimeOffset? EffectiveFrom { get; init; }
}
