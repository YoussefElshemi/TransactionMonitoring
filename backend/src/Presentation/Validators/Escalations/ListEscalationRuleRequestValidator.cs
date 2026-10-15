using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.Escalations;

namespace Presentation.Validators.Escalations;

public class ListEscalationRuleRequestValidator : AbstractValidator<ListEscalationRuleRequestDto>
{
    public ListEscalationRuleRequestValidator(IOrganizationService organizationService)
    {
        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, ct) => await organizationService.ExistsAsync(new OrganizationId(x), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");

        RuleFor(x => x.Name)
            .MaximumLength(EscalationRuleName.MaxLength)
            .When(x => !string.IsNullOrWhiteSpace(x.Name));
    }
}
