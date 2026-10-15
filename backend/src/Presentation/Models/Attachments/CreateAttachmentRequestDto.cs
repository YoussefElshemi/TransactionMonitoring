using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Swashbuckle.AspNetCore.Annotations;

namespace Presentation.Models.Attachments;

public record CreateAttachmentRequestDto
{
    [FromRoute]
    public required Guid CaseId { get; init; }

    [FromForm]
    public required IFormFile File { get; init; }

    [BindNever]
    [SwaggerSchema(ReadOnly = true)]
    public Guid OrganizationId { get; set; }
}
