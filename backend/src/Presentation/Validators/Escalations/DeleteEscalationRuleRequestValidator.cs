using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.Escalations;

namespace Presentation.Validators.Escalations;

public class DeleteEscalationRuleRequestValidator : AbstractValidator<DeleteEscalationRuleRequestDto>
{
    public DeleteEscalationRuleRequestValidator(IOrganizationService organizationService, IEscalationRuleService escalationRuleService)
    {
        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, ct) => await organizationService.ExistsAsync(new OrganizationId(x), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");

        RuleFor(x => x.EscalationRuleId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await escalationRuleService.ExistsAsync(new OrganizationId(x.OrganizationId), new EscalationRuleId(x.EscalationRuleId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Escalation rule not found");
    }
}
