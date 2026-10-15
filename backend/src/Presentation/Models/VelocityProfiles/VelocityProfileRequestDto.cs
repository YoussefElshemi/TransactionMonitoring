using Microsoft.AspNetCore.Mvc;
using Presentation.Models.Common;
using Presentation.Models.Organizations;

namespace Presentation.Models.VelocityProfiles;

public record VelocityProfileRequestDto : OrganizationRequestDto;

public record GetVelocityProfileRequestDto : OrganizationRequestDto
{
    [FromQuery]
    public bool IncludeDeleted { get; init; }
}

public record RecoverVelocityProfileRequestDto : OrganizationRequestDto;

public record VelocityProfileHistoryRequestDto : OrganizationRequestDto;

public record ListVelocityBackfillRequestDto : PagedRequestDto
{
    [FromRoute]
    public new required Guid OrganizationId
    {
        get => base.OrganizationId;
        init => base.OrganizationId = value;
    }
}

public record ListMyVelocityBackfillRequestDto : PagedRequestDto;
