using Microsoft.AspNetCore.Mvc;
using Presentation.Models.Common;
using Presentation.Models.Organizations;

namespace Presentation.Models.Simulations;

public record StartSimulationBodyDto
{
    public required string Scenario { get; init; }
    public required int PaymentCount { get; init; }

    public int? Seed { get; init; }
}

public record StartSimulationRequestDto : OrganizationRequestDto
{
    [FromBody]
    public required StartSimulationBodyDto Body { get; init; }
}

public record GetSimulationRequestDto : OrganizationRequestDto
{
    [FromRoute]
    public required Guid SimulationId { get; init; }
}

public record ListSimulationRequestDto : PagedRequestDto
{
    [FromRoute]
    public new required Guid OrganizationId
    {
        get => base.OrganizationId;
        init => base.OrganizationId = value;
    }
}

public record ListMySimulationRequestDto : PagedRequestDto;
