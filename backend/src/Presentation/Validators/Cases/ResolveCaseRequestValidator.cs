using System.Net;
using Core.Enums;
using Core.Extensions;
using Core.Helpers;
using Core.Interfaces.Services;
using Core.Models.Cases;
using Core.Models.Organizations;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.Cases;

namespace Presentation.Validators.Cases;

public class ResolveCaseRequestValidator : AbstractValidator<ResolveCaseRequestDto>
{
    public ResolveCaseRequestValidator(
        ICaseService caseService,
        ICaseReasonCodeService caseReasonCodeService,
        IOrganizationUserService organizationUserService)
    {
        RuleFor(x => x.CaseId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await caseService.ExistsAsync(new OrganizationId(x.OrganizationId), new CaseId(x.CaseId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Case not found")
            .MustAsync(async (x, _, ct) =>
            {
                var @case = await caseService.GetAsync(new CaseId(x.CaseId), ct);

                return !@case.Status.IsTerminal();
            })
            .WithMessage($"{nameof(Case)} has already been resolved");

        RuleFor(x => x.Body.UserId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await organizationUserService.ExistsAsync(new OrganizationId(x.OrganizationId), new ApplicationUserId(x.Body.UserId), new IncludeDeleted(false), ct))
            .WithMessage($"User was not found in {nameof(Organization)}")
            .MustAsync(async (x, _, ct) =>
            {
                var @case = await caseService.GetAsync(new CaseId(x.CaseId), ct);

                return @case.AssignedToUserId == x.Body.UserId;
            })
            .WithMessage($"{nameof(Case)} must be assigned to user attempting to resolve");

        RuleFor(x => x.Body.Status)
            .Cascade(CascadeMode.Stop)
            .IsEnumName(typeof(CaseStatus), false)
            .Must(x => Enum.Parse<CaseStatus>(x, true) is CaseStatus.Approved or CaseStatus.Rejected)
            .WithMessage($"Status must be one of '{nameof(CaseStatus.Approved)}', '{nameof(CaseStatus.Rejected)}'");

        RuleFor(x => x.Body.Disposition)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .IsEnumName(typeof(CaseDisposition), false)
            .WithMessage($"Disposition must be one of {DispositionNames}");

        RuleFor(x => x.Body.Note)
            .NotEmpty()
            .WithMessage("Note is required when rejecting a case or closing it as a true positive")
            .When(x => IsValidClose(x.Body) && CaseClosurePolicy.RequiresNote(ParseStatus(x.Body), ParseDisposition(x.Body)));

        RuleFor(x => x.Body.ReasonCodeId)
            .Cascade(CascadeMode.Stop)
            .MustAsync(async (x, reasonCodeId, ct) =>
            {
                if (reasonCodeId.HasValue)
                {
                    return true;
                }

                var anyEnabled = await caseReasonCodeService.AnyEnabledAsync(
                    new OrganizationId(x.OrganizationId), TryParseDisposition(x.Body), ct);

                return !anyEnabled;
            })
            .WithMessage("ReasonCode is required because this organization has declared a reason code taxonomy")
            .MustAsync(async (x, reasonCodeId, ct) =>
            {
                if (!reasonCodeId.HasValue)
                {
                    return true;
                }

                return await caseReasonCodeService.ExistsAsync(new OrganizationId(x.OrganizationId), new CaseReasonCodeId(reasonCodeId.Value), new IncludeDeleted(false), ct);
            })
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Case reason code not found")
            .MustAsync(async (x, reasonCodeId, ct) =>
            {
                if (!reasonCodeId.HasValue || !IsValidClose(x.Body))
                {
                    return true;
                }

                var caseReasonCode = await caseReasonCodeService.GetAsync(new CaseReasonCodeId(reasonCodeId.Value), new IncludeDeleted(false), ct);

                return caseReasonCode.IsEnabled && caseReasonCode.PermitsDisposition(ParseDisposition(x.Body));
            })
            .WithMessage("Case reason code is retired or does not apply to this disposition");
    }

    private static readonly string DispositionNames = string.Join(", ", Enum.GetNames<CaseDisposition>().Select(x => $"'{x}'"));

    private static bool IsValidClose(ResolveCaseRequestBodyDto body)
    {
        return Enum.TryParse<CaseStatus>(body.Status, true, out _) &&
               Enum.TryParse<CaseDisposition>(body.Disposition, true, out _);
    }

    private static CaseStatus ParseStatus(ResolveCaseRequestBodyDto body)
    {
        return Enum.Parse<CaseStatus>(body.Status, true);
    }

    private static CaseDisposition ParseDisposition(ResolveCaseRequestBodyDto body)
    {
        return Enum.Parse<CaseDisposition>(body.Disposition, true);
    }

    private static CaseDisposition? TryParseDisposition(ResolveCaseRequestBodyDto body)
    {
        return Enum.TryParse<CaseDisposition>(body.Disposition, true, out var disposition) ? disposition : null;
    }
}
