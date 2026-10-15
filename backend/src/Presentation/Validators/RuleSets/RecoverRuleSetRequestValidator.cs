using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.RuleSets;

namespace Presentation.Validators.RuleSets;

public class RecoverRuleSetRequestValidator : AbstractValidator<RecoverRuleSetRequestDto>
{
    public RecoverRuleSetRequestValidator(IRuleSetService ruleSetService)
    {
        RuleFor(x => x.RuleSetId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await ruleSetService.ExistsAsync(new OrganizationId(x.OrganizationId), new RuleSetId(x.RuleSetId), new IncludeDeleted(true), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Rule Set not found");
    }
}