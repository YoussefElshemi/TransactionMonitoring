using Microsoft.AspNetCore.Mvc;
using Presentation.Models.Common;

namespace Presentation.Models.AuditLogs;

public record OrganizationExportAuditLogRequestDto : DatedRequestDto
{
    [FromRoute]
    public required Guid OrganizationId { get; init; }

    [FromQuery]
    public string? EntityType { get; init; }

    [FromQuery]
    public Guid? EntityId { get; init; }
}

public record OrganizationVerifyAuditChainRequestDto : DatedRequestDto
{
    [FromRoute]
    public required Guid OrganizationId { get; init; }
}
