using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.RuleSets;

namespace Presentation.Validators.RuleSets;

public class EvaluateRuleSetRequestValidator : AbstractValidator<EvaluateRuleSetRequestDto>
{
    public EvaluateRuleSetRequestValidator(IRuleSetService ruleSetService)
    {
        RuleFor(x => x.RuleSetId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await ruleSetService.ExistsAsync(new OrganizationId(x.OrganizationId), new RuleSetId(x.RuleSetId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Rule Set not found")
            .MustAsync(async (x, ct) =>
            {
                var ruleSet = await ruleSetService.GetAsync(new RuleSetId(x), ct);
                return ruleSet.IsEnabled == true;
            })
            .WithMessage($"{nameof(RuleSet)} is not enabled");
    }
}