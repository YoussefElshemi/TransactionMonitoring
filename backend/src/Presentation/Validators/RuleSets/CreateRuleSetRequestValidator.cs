using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.RuleSets;

namespace Presentation.Validators.RuleSets;

public class CreateRuleSetRequestValidator : AbstractValidator<CreateRuleSetRequestDto>
{
    public CreateRuleSetRequestValidator(ISubOrganizationService subOrganizationService)
    {
        RuleFor(x => x.FriendlyName)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.IsEnabled)
            .NotNull();

        RuleFor(x => x.ReviewThreshold)
            .GreaterThanOrEqualTo(0)
            .When(x => x.ReviewThreshold.HasValue);

        RuleFor(x => x.SubOrganizationId)
            .MustAsync(async (x, _, ct) => await subOrganizationService.ExistsAsync(
                new OrganizationId(x.OrganizationId), new SubOrganizationId(x.SubOrganizationId!.Value), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Sub-organization not found")
            .When(x => x.SubOrganizationId.HasValue);
    }
}