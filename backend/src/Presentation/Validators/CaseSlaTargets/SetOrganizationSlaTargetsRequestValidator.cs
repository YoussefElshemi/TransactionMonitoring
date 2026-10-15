using System.Net;
using Core.Enums;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.CaseSlaTargets;

namespace Presentation.Validators.CaseSlaTargets;

public class SetOrganizationSlaTargetsRequestValidator : AbstractValidator<SetOrganizationSlaTargetsRequestDto>
{
    public SetOrganizationSlaTargetsRequestValidator(IOrganizationService organizationService)
    {
        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, ct) => await organizationService.ExistsAsync(new OrganizationId(x), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");

        RuleFor(x => x.Body.Targets)
            .NotNull();

        // Guarded both sides: these run before the per-target rules, and a null severity would 500 inside the validator.
        RuleFor(x => x.Body.Targets)
            .Must(x => x.Select(target => target.Severity?.ToUpperInvariant()).Distinct().Count() == x.Length)
            .WithMessage("Targets cannot name the same severity twice")
            .When(x => x.Body.Targets is not null);

        RuleForEach(x => x.Body.Targets).ChildRules(target =>
        {
            target.RuleFor(x => x.Severity)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .IsEnumName(typeof(RuleSeverity), false);

            target.RuleFor(x => x.TargetMinutes)
                .InclusiveBetween(1, SlaTarget.MaxValue);
        });
    }
}
