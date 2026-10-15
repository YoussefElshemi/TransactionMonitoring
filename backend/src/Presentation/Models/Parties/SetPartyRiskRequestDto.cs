using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Swashbuckle.AspNetCore.Annotations;

namespace Presentation.Models.Parties;

public record SetPartyRiskRequestDto
{
    [FromRoute]
    public required Guid PartyId { get; init; }

    [BindNever]
    [SwaggerSchema(ReadOnly = true)]
    public Guid OrganizationId { get; set; }

    [FromBody]
    public required SetPartyRiskRequestBodyDto Body { get; init; }
}

public record SetPartyRiskRequestBodyDto
{
    public int? RiskScore { get; init; }

    public required string Note { get; init; }
}
