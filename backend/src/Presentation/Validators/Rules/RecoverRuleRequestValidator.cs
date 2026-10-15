using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.Rules;

namespace Presentation.Validators.Rules;

public class RecoverRuleRequestValidator : AbstractValidator<RecoverRuleRequestDto>
{
    public RecoverRuleRequestValidator(IRuleService ruleService)
    {
        RuleFor(x => x.RuleId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await ruleService.ExistsAsync(new OrganizationId(x.OrganizationId), new RuleId(x.RuleId), new IncludeDeleted(true), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Rule not found");
    }
}