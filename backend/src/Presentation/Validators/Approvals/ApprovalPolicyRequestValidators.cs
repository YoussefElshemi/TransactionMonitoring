using System.Net;
using Core.Enums;
using Core.Helpers;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Mappers.Rules;
using Presentation.Models.Approvals;
using Presentation.Validators.Rules;

namespace Presentation.Validators.Approvals;

public class OrganizationApprovalPolicyRequestValidator : AbstractValidator<OrganizationApprovalPolicyRequestDto>
{
    public OrganizationApprovalPolicyRequestValidator(IOrganizationService organizationService)
    {
        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, ct) => await organizationService.ExistsAsync(new OrganizationId(x), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");
    }
}

public class SetOrganizationApprovalPolicyRequestValidator : AbstractValidator<SetOrganizationApprovalPolicyRequestDto>
{
    public SetOrganizationApprovalPolicyRequestValidator(IOrganizationService organizationService)
    {
        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, ct) => await organizationService.ExistsAsync(new OrganizationId(x), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");

        RuleFor(x => x.Body.CaseDecisionMinimumSeverity)
            .IsEnumName(typeof(RuleSeverity), false)
            .When(x => !string.IsNullOrWhiteSpace(x.Body.CaseDecisionMinimumSeverity));

        RuleFor(x => x.Body.CaseDecisionMatcher!)
            .SetValidator(new RuleBodyValidator())
            .When(x => x.Body.CaseDecisionMatcher is not null);

        RuleFor(x => x.Body.CaseDecisionMatcher!)
            .Must(x => !RuleHelper.ContainsVelocity(x.ToModel()))
            .WithMessage("CaseDecisionMatcher cannot contain a velocity condition")
            .When(x => x.Body.CaseDecisionMatcher is not null);
    }
}
