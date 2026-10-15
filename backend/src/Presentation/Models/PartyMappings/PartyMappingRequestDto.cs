using Microsoft.AspNetCore.Mvc;
using Presentation.Models.Common;
using Presentation.Models.Organizations;

namespace Presentation.Models.PartyMappings;

public record PartyMappingVersionRequestDto : OrganizationRequestDto
{
    [FromRoute]
    public required int VersionNumber { get; init; }
}

public record PartyMappingRequestDto : OrganizationRequestDto;

public record GetPartyMappingRequestDto : OrganizationRequestDto
{
    [FromQuery]
    public bool IncludeDeleted { get; init; }
}

public record RecoverPartyMappingRequestDto : OrganizationRequestDto;

public record PartyMappingHistoryRequestDto : OrganizationRequestDto;

public record ListPartyMappingVersionRequestDto : PagedRequestDto
{
    [FromRoute]
    public new required Guid OrganizationId
    {
        get => base.OrganizationId;
        init => base.OrganizationId = value;
    }
}

public record ListMyPartyMappingVersionRequestDto : PagedRequestDto;
