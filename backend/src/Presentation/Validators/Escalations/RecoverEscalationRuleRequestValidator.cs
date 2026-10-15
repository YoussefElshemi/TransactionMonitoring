using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.Escalations;

namespace Presentation.Validators.Escalations;

public class RecoverEscalationRuleRequestValidator : AbstractValidator<RecoverEscalationRuleRequestDto>
{
    public RecoverEscalationRuleRequestValidator(IOrganizationService organizationService, IEscalationRuleService escalationRuleService)
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
            .MustAsync(async (x, _, ct) => await escalationRuleService.ExistsAsync(new OrganizationId(x.OrganizationId), new EscalationRuleId(x.EscalationRuleId), new IncludeDeleted(true), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Escalation rule not found")
            .MustAsync(async (x, _, ct) =>
            {
                var retired = await escalationRuleService.GetAsync(new EscalationRuleId(x.EscalationRuleId), new IncludeDeleted(true), ct);
                var holder = await escalationRuleService.FindByNameAsync(new OrganizationId(x.OrganizationId), retired.Name, ct);

                return holder is null;
            })
            .WithErrorCode(HttpStatusCode.Conflict.ToString())
            .WithMessage("Escalation rule name is already declared for this organization");
    }
}
