using Core.Enums;
using FluentValidation;
using Presentation.Models.Rules;

namespace Presentation.Validators.Rules;

public class TestRuleRequestValidator : AbstractValidator<TestRuleRequestDto>
{
    public TestRuleRequestValidator()
    {
        RuleFor(x => x.RuleBody)
            .SetValidator(new RuleBodyValidator());

        RuleFor(x => x.MatchPolarity)
            .Must(matchPolarity => Enum.TryParse<RuleMatchPolarity>(matchPolarity, true, out _))
            .WithMessage($"MatchPolarity must be one of: {string.Join(", ", Enum.GetNames<RuleMatchPolarity>())}");

        RuleFor(x => x.Action)
            .Must(action => Enum.TryParse<RuleAction>(action, true, out _))
            .WithMessage($"Action must be one of: {string.Join(", ", Enum.GetNames<RuleAction>())}");

        RuleFor(x => x.Severity)
            .Must(severity => Enum.TryParse<RuleSeverity>(severity, true, out _))
            .WithMessage($"Severity must be one of: {string.Join(", ", Enum.GetNames<RuleSeverity>())}");

        RuleFor(x => x.RiskWeight)
            .InclusiveBetween(0, 100);
    }
}