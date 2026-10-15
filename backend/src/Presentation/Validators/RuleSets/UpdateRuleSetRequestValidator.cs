using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.RuleSets;

namespace Presentation.Validators.RuleSets;

public class UpdateRuleSetRequestValidator : AbstractValidator<UpdateRuleSetRequestDto>
{
    public UpdateRuleSetRequestValidator(IRuleSetService ruleSetService, ISubOrganizationService subOrganizationService)
    {
        RuleFor(x => x.RuleSetId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await ruleSetService.ExistsAsync(new OrganizationId(x.OrganizationId), new RuleSetId(x.RuleSetId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Rule Set not found");

        RuleFor(x => x.Body.FriendlyName)
            .NotEmpty()
            .MaximumLength(50)
            .When(x => x.Body.FriendlyName != null);

        RuleFor(x => x.Body.ReviewThreshold)
            .GreaterThanOrEqualTo(0)
            .When(x => x.Body.ReviewThreshold.HasValue);

        RuleFor(x => x.Body.SubOrganizationId)
            .MustAsync(async (x, _, ct) => await subOrganizationService.ExistsAsync(
                new OrganizationId(x.OrganizationId), new SubOrganizationId(x.Body.SubOrganizationId!.Value), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Sub-organization not found")
            .When(x => x.Body.SubOrganizationId.HasValue);

        RuleFor(x => x.Body.ClearSubOrganization)
            .Must(x => x != true)
            .WithMessage("ClearSubOrganization cannot be sent alongside a SubOrganizationId")
            .When(x => x.Body.SubOrganizationId.HasValue);
    }
}