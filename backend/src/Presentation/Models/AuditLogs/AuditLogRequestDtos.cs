using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Presentation.Models.Common;
using Swashbuckle.AspNetCore.Annotations;

namespace Presentation.Models.AuditLogs;

public record ExportAuditLogRequestDto : DatedRequestDto
{
    [BindNever]
    [SwaggerSchema(ReadOnly = true)]
    public Guid OrganizationId { get; set; }

    [FromQuery]
    public string? EntityType { get; init; }

    [FromQuery]
    public Guid? EntityId { get; init; }
}

public record VerifyAuditChainRequestDto : DatedRequestDto
{
    [BindNever]
    [SwaggerSchema(ReadOnly = true)]
    public Guid OrganizationId { get; set; }
}
