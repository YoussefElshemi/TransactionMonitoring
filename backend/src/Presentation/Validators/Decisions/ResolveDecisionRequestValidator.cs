using System.Net;
using Core.Enums;
using Core.Helpers;
using Core.Interfaces.Services;
using Core.Models.Organizations;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.Decisions;

namespace Presentation.Validators.Decisions;

public class ResolveDecisionRequestValidator : AbstractValidator<ResolveDecisionRequestDto>
{
    public ResolveDecisionRequestValidator(
        IDecisionService decisionService,
        ICaseReasonCodeService caseReasonCodeService,
        IOrganizationUserService organizationUserService)
    {
        RuleFor(x => x.DecisionId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await decisionService.ExistsAsync(new OrganizationId(x.OrganizationId), new DecisionId(x.DecisionId), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Decision not found");

        RuleFor(x => x.Body.UserId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await organizationUserService.ExistsAsync(new OrganizationId(x.OrganizationId), new ApplicationUserId(x.Body.UserId), new IncludeDeleted(false), ct))
            .WithMessage($"User was not found in {nameof(Organization)}");

        RuleFor(x => x.Body.Status)
            .Cascade(CascadeMode.Stop)
            .IsEnumName(typeof(CaseStatus), false)
            .Must(x => Enum.Parse<CaseStatus>(x, true) is CaseStatus.Approved or CaseStatus.Rejected)
            .WithMessage($"Status must be one of '{nameof(CaseStatus.Approved)}', '{nameof(CaseStatus.Rejected)}'");

        RuleFor(x => x.Body.Disposition)
            .IsEnumName(typeof(CaseDisposition), false)
            .WithMessage($"Disposition must be one of {DispositionNames}")
            .When(x => !string.IsNullOrWhiteSpace(x.Body.Disposition));

        RuleFor(x => x.Body.Disposition)
            .MustAsync(async (x, disposition, context, ct) =>
            {
                var settlesCase = await SettlesCaseAsync(decisionService, x, context, ct);

                return settlesCase == !string.IsNullOrWhiteSpace(disposition);
            })
            .WithMessage("Disposition is required on the settle that closes the case, and is not accepted on any other");

        RuleFor(x => x.Body.Note)
            .NotEmpty()
            .WithMessage("Note is required when blocking a payment or closing a case as a true positive")
            .When(x => Enum.TryParse<CaseStatus>(x.Body.Status, true, out var status) &&
                       CaseClosurePolicy.RequiresNote(status, TryParseDisposition(x.Body)));

        RuleFor(x => x.Body.ReasonCodeId)
            .Cascade(CascadeMode.Stop)
            .MustAsync(async (x, reasonCodeId, context, ct) =>
            {
                if (!reasonCodeId.HasValue)
                {
                    return true;
                }

                return await SettlesCaseAsync(decisionService, x, context, ct);
            })
            .WithMessage("ReasonCode is only accepted on the settle that closes the case")
            .MustAsync(async (x, reasonCodeId, context, ct) =>
            {
                if (reasonCodeId.HasValue || !await SettlesCaseAsync(decisionService, x, context, ct))
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
                var disposition = TryParseDisposition(x.Body);

                if (!reasonCodeId.HasValue || !disposition.HasValue)
                {
                    return true;
                }

                var caseReasonCode = await caseReasonCodeService.GetAsync(new CaseReasonCodeId(reasonCodeId.Value), new IncludeDeleted(false), ct);

                return caseReasonCode.IsEnabled && caseReasonCode.PermitsDisposition(disposition.Value);
            })
            .WithMessage("Case reason code is retired or does not apply to this disposition");
    }

    private static readonly string DispositionNames = string.Join(", ", Enum.GetNames<CaseDisposition>().Select(x => $"'{x}'"));

    private static CaseDisposition? TryParseDisposition(ResolveDecisionRequestBodyDto body)
    {
        return Enum.TryParse<CaseDisposition>(body.Disposition, true, out var disposition) ? disposition : null;
    }

    // Cached on the validation context so the three rules share one lookup.
    private static async Task<bool> SettlesCaseAsync(
        IDecisionService decisionService,
        ResolveDecisionRequestDto resolveDecisionRequestDto,
        ValidationContext<ResolveDecisionRequestDto> context,
        CancellationToken stoppingToken)
    {
        const string key = nameof(SettlesCaseAsync);

        if (context.RootContextData.TryGetValue(key, out var cached))
        {
            return (bool)cached;
        }

        var settlesCase = await decisionService.SettlesCaseAsync(new DecisionId(resolveDecisionRequestDto.DecisionId), stoppingToken);

        context.RootContextData[key] = (bool)settlesCase;

        return settlesCase;
    }
}
