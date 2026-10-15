using System.Net;
using Core.Enums;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.Rules;

namespace Presentation.Validators.Rules;

public class CreateRuleVersionRequestValidator : AbstractValidator<CreateRuleVersionRequestDto>
{
    public CreateRuleVersionRequestValidator(IRuleService ruleService)
    {
        RuleFor(x => x.RuleId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await ruleService.ExistsAsync(new OrganizationId(x.OrganizationId), new RuleId(x.RuleId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Rule not found");

        RuleFor(x => x.Body.RuleBody)
            .NotNull()
            .SetValidator(new RuleBodyValidator()!)
            .When(x => x.Body.RuleBody != null);

        RuleFor(x => x.Body.Action)
            .Must(action => Enum.TryParse<RuleAction>(action, true, out _))
            .WithMessage($"Action must be one of: {string.Join(", ", Enum.GetNames<RuleAction>())}");

        RuleFor(x => x.Body.Severity)
            .Must(severity => Enum.TryParse<RuleSeverity>(severity, true, out _))
            .WithMessage($"Severity must be one of: {string.Join(", ", Enum.GetNames<RuleSeverity>())}");

        RuleFor(x => x.Body.RiskWeight)
            .InclusiveBetween(0, 100);
    }
}
