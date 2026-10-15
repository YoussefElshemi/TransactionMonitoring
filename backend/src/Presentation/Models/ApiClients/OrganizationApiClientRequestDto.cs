using Microsoft.AspNetCore.Mvc;
using Presentation.Models.Common;
using Presentation.Models.Organizations;

namespace Presentation.Models.ApiClients;

public record OrganizationApiClientRequestDto : OrganizationRequestDto
{
    [FromRoute]
    public required Guid ApiClientId { get; init; }
}

// Separate type: recovery must see soft-deleted rows and the other validator refuses them.
public record OrganizationRecoverApiClientRequestDto : OrganizationApiClientRequestDto;

public record OrganizationUpdateApiClientRequestDto : OrganizationApiClientRequestDto
{
    [FromBody]
    public required UpdateApiClientRequestBodyDto Body { get; init; }
}

public record OrganizationApiClientSecretRequestDto : OrganizationApiClientRequestDto
{
    [FromRoute]
    public required Guid ApiClientSecretId { get; init; }
}

public record OrganizationListApiClientRequestDto : PagedRequestDto
{
    [FromRoute]
    public new required Guid OrganizationId
    {
        get => base.OrganizationId;
        init => base.OrganizationId = value;
    }

    public string? FriendlyName { get; init; }
}
