using System.Net;
using Core.Helpers;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.SuspiciousActivityReports;

namespace Presentation.Validators.SuspiciousActivityReports;

public class SuspiciousActivityReportRequestValidator : AbstractValidator<SuspiciousActivityReportRequestDto>
{
    public SuspiciousActivityReportRequestValidator(ISuspiciousActivityReportService suspiciousActivityReportService)
    {
        RuleFor(x => x.SuspiciousActivityReportId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await suspiciousActivityReportService.ExistsAsync(
                new OrganizationId(x.OrganizationId), new SuspiciousActivityReportId(x.SuspiciousActivityReportId), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Suspicious activity report not found");
    }
}

public class GetCaseSuspiciousActivityReportRequestValidator : AbstractValidator<GetCaseSuspiciousActivityReportRequestDto>
{
    public GetCaseSuspiciousActivityReportRequestValidator(ICaseService caseService)
    {
        RuleFor(x => x.CaseId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await caseService.ExistsAsync(
                new OrganizationId(x.OrganizationId), new CaseId(x.CaseId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Case not found");
    }
}

public class OpenSuspiciousActivityReportRequestValidator : AbstractValidator<OpenSuspiciousActivityReportRequestDto>
{
    public OpenSuspiciousActivityReportRequestValidator(ICaseService caseService, IOrganizationService organizationService)
    {
        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, ct) => await organizationService.ExistsAsync(new OrganizationId(x), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");

        RuleFor(x => x.CaseId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await caseService.ExistsAsync(
                new OrganizationId(x.OrganizationId), new CaseId(x.CaseId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Case not found");

        RuleFor(x => x.Body!.Narrative)
            .MaximumLength(Narrative.MaxLength)
            .When(x => !string.IsNullOrWhiteSpace(x.Body?.Narrative));
    }
}

public class UpdateSuspiciousActivityReportRequestValidator : AbstractValidator<UpdateSuspiciousActivityReportRequestDto>
{
    public UpdateSuspiciousActivityReportRequestValidator(ISuspiciousActivityReportService suspiciousActivityReportService)
    {
        RuleFor(x => x.SuspiciousActivityReportId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await suspiciousActivityReportService.ExistsAsync(
                new OrganizationId(x.OrganizationId), new SuspiciousActivityReportId(x.SuspiciousActivityReportId), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Suspicious activity report not found");

        RuleFor(x => x.Body.Narrative)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("A narrative is required")
            .MaximumLength(Narrative.MaxLength);
    }
}

public class RecomposeSuspiciousActivityReportRequestValidator : AbstractValidator<RecomposeSuspiciousActivityReportRequestDto>
{
    public RecomposeSuspiciousActivityReportRequestValidator(ISuspiciousActivityReportService suspiciousActivityReportService)
    {
        RuleFor(x => x.SuspiciousActivityReportId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await suspiciousActivityReportService.ExistsAsync(
                new OrganizationId(x.OrganizationId), new SuspiciousActivityReportId(x.SuspiciousActivityReportId), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Suspicious activity report not found");
    }
}

public class SubmitSuspiciousActivityReportRequestValidator : AbstractValidator<SubmitSuspiciousActivityReportRequestDto>
{
    public SubmitSuspiciousActivityReportRequestValidator(ISuspiciousActivityReportService suspiciousActivityReportService)
    {
        RuleFor(x => x.SuspiciousActivityReportId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await suspiciousActivityReportService.ExistsAsync(
                new OrganizationId(x.OrganizationId), new SuspiciousActivityReportId(x.SuspiciousActivityReportId), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Suspicious activity report not found");

        RuleFor(x => x.Body!.Reason)
            .MaximumLength(SuspiciousActivityReportConstants.MaxReasonLength)
            .When(x => !string.IsNullOrWhiteSpace(x.Body?.Reason));
    }
}

public class FileSuspiciousActivityReportRequestValidator : AbstractValidator<FileSuspiciousActivityReportRequestDto>
{
    public FileSuspiciousActivityReportRequestValidator(ISuspiciousActivityReportService suspiciousActivityReportService)
    {
        RuleFor(x => x.SuspiciousActivityReportId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await suspiciousActivityReportService.ExistsAsync(
                new OrganizationId(x.OrganizationId), new SuspiciousActivityReportId(x.SuspiciousActivityReportId), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Suspicious activity report not found");

        RuleFor(x => x.Body.FilingReference)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("A filing reference is required")
            .MaximumLength(FilingReference.MaxLength);
    }
}

public class AcknowledgeSuspiciousActivityReportRequestValidator : AbstractValidator<AcknowledgeSuspiciousActivityReportRequestDto>
{
    public AcknowledgeSuspiciousActivityReportRequestValidator(ISuspiciousActivityReportService suspiciousActivityReportService)
    {
        RuleFor(x => x.SuspiciousActivityReportId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await suspiciousActivityReportService.ExistsAsync(
                new OrganizationId(x.OrganizationId), new SuspiciousActivityReportId(x.SuspiciousActivityReportId), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Suspicious activity report not found");

        RuleFor(x => x.Body!.AcknowledgementReference)
            .MaximumLength(FilingReference.MaxLength)
            .When(x => !string.IsNullOrWhiteSpace(x.Body?.AcknowledgementReference));
    }
}

public class DeclineSuspiciousActivityReportRequestValidator : AbstractValidator<DeclineSuspiciousActivityReportRequestDto>
{
    public DeclineSuspiciousActivityReportRequestValidator(ISuspiciousActivityReportService suspiciousActivityReportService)
    {
        RuleFor(x => x.SuspiciousActivityReportId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await suspiciousActivityReportService.ExistsAsync(
                new OrganizationId(x.OrganizationId), new SuspiciousActivityReportId(x.SuspiciousActivityReportId), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Suspicious activity report not found");

        RuleFor(x => x.Body.Reason)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("A reason is required when recording a decision not to file")
            .MaximumLength(SuspiciousActivityReportConstants.MaxReasonLength);
    }
}

public class ExportSuspiciousActivityReportRequestValidator : AbstractValidator<ExportSuspiciousActivityReportRequestDto>
{
    public ExportSuspiciousActivityReportRequestValidator(ISuspiciousActivityReportService suspiciousActivityReportService)
    {
        RuleFor(x => x.SuspiciousActivityReportId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await suspiciousActivityReportService.ExistsAsync(
                new OrganizationId(x.OrganizationId), new SuspiciousActivityReportId(x.SuspiciousActivityReportId), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Suspicious activity report not found");
    }
}

public class OrganizationSuspiciousActivityReportPolicyRequestValidator
    : AbstractValidator<OrganizationSuspiciousActivityReportPolicyRequestDto>
{
    public OrganizationSuspiciousActivityReportPolicyRequestValidator(IOrganizationService organizationService)
    {
        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, ct) => await organizationService.ExistsAsync(new OrganizationId(x), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");
    }
}

public class SetOrganizationSuspiciousActivityReportPolicyRequestValidator
    : AbstractValidator<SetOrganizationSuspiciousActivityReportPolicyRequestDto>
{
    public SetOrganizationSuspiciousActivityReportPolicyRequestValidator(IOrganizationService organizationService)
    {
        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, ct) => await organizationService.ExistsAsync(new OrganizationId(x), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");

        RuleFor(x => x.Body.ReportingEntityId)
            .MaximumLength(ReportingEntityId.MaxLength)
            .When(x => !string.IsNullOrWhiteSpace(x.Body.ReportingEntityId));

        RuleFor(x => x.Body.FilingDeadlineDays)
            .InclusiveBetween(1, FilingDeadline.MaxValue)
            .When(x => x.Body.FilingDeadlineDays.HasValue);

        // Refused here so a mistyped path is a 400 rather than a 500 from the value object.
        RuleFor(x => x.Body.AmountPath!)
            .Cascade(CascadeMode.Stop)
            .MaximumLength(PayloadFieldPath.MaxLength)
            .Must(PayloadPath.IsAddressable)
            .WithMessage("AmountPath is not a path the engine can read")
            .Must(x => !PayloadPath.HasWildcard(x))
            .WithMessage("AmountPath resolves to many values, so it cannot name a field")
            .When(x => !string.IsNullOrWhiteSpace(x.Body.AmountPath));

        RuleFor(x => x.Body.CurrencyPath!)
            .Cascade(CascadeMode.Stop)
            .MaximumLength(PayloadFieldPath.MaxLength)
            .Must(PayloadPath.IsAddressable)
            .WithMessage("CurrencyPath is not a path the engine can read")
            .Must(x => !PayloadPath.HasWildcard(x))
            .WithMessage("CurrencyPath resolves to many values, so it cannot name a field")
            .When(x => !string.IsNullOrWhiteSpace(x.Body.CurrencyPath));
    }
}

internal static class SuspiciousActivityReportConstants
{
    public const int MaxReasonLength = 2048;
}
