using System.Net;
using Core.Interfaces.Services;
using Core.Models.Rules;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.Rules;

namespace Presentation.Validators.Rules;

public class EvaluateRuleRequestValidator : AbstractValidator<EvaluateRuleRequestDto>
{
    private const string RuleContextKey = "Rule";

    public EvaluateRuleRequestValidator(IRuleService ruleService, IRuleSetService ruleSetService)
    {
        RuleFor(x => x.RuleId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await ruleService.ExistsAsync(new OrganizationId(x.OrganizationId), new RuleId(x.RuleId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Rule not found")
            .MustAsync(async (_, ruleId, context, ct) =>
            {
                var rule = await ruleService.GetAsync(new RuleId(ruleId), ct);
                context.RootContextData[RuleContextKey] = rule;
                return rule.IsEnabled == true;
            })
            .WithMessage($"{nameof(Rule)} is not enabled")
            .MustAsync(async (_, _, context, ct) =>
            {
                var rule = (Rule)context.RootContextData[RuleContextKey];
                var ruleSet = await ruleSetService.GetAsync(rule.RuleSetId, ct);
                return ruleSet.IsEnabled == true;
            })
            .WithMessage($"{nameof(RuleSet)} is not enabled");
    }
}