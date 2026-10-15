using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Swashbuckle.AspNetCore.Annotations;

namespace Presentation.Models.Attachments;

public record AttachmentRequestDto
{
    [FromRoute]
    public required Guid CaseId { get; init; }

    [FromRoute]
    public required Guid AttachmentId { get; init; }

    [BindNever]
    [SwaggerSchema(ReadOnly = true)]
    public Guid OrganizationId { get; set; }
}
