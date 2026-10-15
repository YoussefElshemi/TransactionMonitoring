using Core.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Swashbuckle.AspNetCore.Annotations;

namespace Presentation.Models.Plans;

public record CreatePlanRequestDto
{
    [FromBody]
    public required CreatePlanRequestBodyDto Body { get; init; }

    [BindNever]
    [SwaggerSchema(ReadOnly = true)]
    public Guid? CreatedByUserId { get; set; }
}

public record CreatePlanRequestBodyDto
{
    public required string Name { get; init; }
    public string? Description { get; init; }

    public Guid? OrganizationId { get; init; }

    public required decimal CostPerRequest { get; init; }

    public required Currency Currency { get; init; }

    public decimal PlatformFee { get; init; }
    public decimal MinimumCharge { get; init; }

    public List<PlanPriceRequestDto> Prices { get; init; } = [];
}
