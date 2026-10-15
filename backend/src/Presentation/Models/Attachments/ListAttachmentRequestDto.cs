using Core.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Presentation.Models.Common;
using Swashbuckle.AspNetCore.Annotations;

namespace Presentation.Models.Attachments;

public record ListAttachmentRequestDto : PagedRequestDto
{
    [FromRoute]
    public required Guid CaseId { get; init; }

    [FromQuery]
    public string? FileName { get; init; }

    [FromQuery]
    public AttachmentScanStatus? ScanStatus { get; init; }

    [BindNever]
    [SwaggerSchema(ReadOnly = true)]
    public new required Guid OrganizationId
    {
        get => base.OrganizationId;
        set => base.OrganizationId = value;
    }
}
