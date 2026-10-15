using System.Net;
using Core.Configs;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Microsoft.Extensions.Options;
using Presentation.Models.Attachments;

namespace Presentation.Validators.Attachments;

public class CreateAttachmentRequestValidator : AbstractValidator<CreateAttachmentRequestDto>
{
    public CreateAttachmentRequestValidator(
        ICaseService caseService,
        IOrganizationService organizationService,
        IOptions<AppConfig> appConfig)
    {
        var attachmentConfig = appConfig.Value.AttachmentConfig;

        RuleFor(x => x.CaseId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await caseService.ExistsAsync(new OrganizationId(x.OrganizationId), new CaseId(x.CaseId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Case not found");

        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await organizationService.ExistsAsync(new OrganizationId(x.OrganizationId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");

        RuleFor(x => x.File)
            .NotNull()
            .WithMessage("A file is required");

        RuleFor(x => x.File.Length)
            .GreaterThan(0)
            .WithMessage("File is empty")
            .LessThanOrEqualTo(attachmentConfig.MaxByteCount)
            .WithMessage($"File exceeds the maximum size of {attachmentConfig.MaxByteCount} bytes")
            .When(x => x.File != null);

        RuleFor(x => x.File.ContentType)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(contentType => attachmentConfig.AllowedContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Content type is not accepted. Allowed types are: {string.Join(", ", attachmentConfig.AllowedContentTypes)}")
            .When(x => x.File != null);

        RuleFor(x => x.File.FileName)
            .NotEmpty()
            .MaximumLength(255)
            .When(x => x.File != null);
    }
}
