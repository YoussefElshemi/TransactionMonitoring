using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.Attachments;

namespace Presentation.Validators.Attachments;

public class RescanAttachmentRequestValidator : AbstractValidator<RescanAttachmentRequestDto>
{
    public RescanAttachmentRequestValidator(ICaseService caseService, IAttachmentService attachmentService)
    {
        RuleFor(x => x.CaseId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await caseService.ExistsAsync(new OrganizationId(x.OrganizationId), new CaseId(x.CaseId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Case not found");

        RuleFor(x => x.AttachmentId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await attachmentService.ExistsAsync(new OrganizationId(x.OrganizationId), new CaseId(x.CaseId), new AttachmentId(x.AttachmentId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Attachment not found");
    }
}
