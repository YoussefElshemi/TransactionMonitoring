using Microsoft.AspNetCore.Mvc;

namespace Presentation.Models.PartyMappings;

public record SetPartyMappingRequestDto
{
    [FromRoute]
    public required Guid OrganizationId { get; init; }

    [FromBody]
    public required SetPartyMappingRequestBodyDto Body { get; init; }
}

public record SetPartyMappingRequestBodyDto
{
    public required List<PartyRoleMappingDto> Mappings { get; init; }
}
