using Core.Helpers;
using FluentValidation;
using Presentation.Mappers.Rules;
using Presentation.Models.Rules;
using Presentation.Validators.Rules;

namespace Presentation.Validators.Escalations;

public class EscalationMatcherValidator : AbstractValidator<RuleBodyDto>
{
    public EscalationMatcherValidator()
    {
        Include(new RuleBodyValidator());

        RuleFor(x => x)
            .Must(x => !RuleHelper.ContainsVelocity(x.ToModel()))
            .WithMessage("Matcher cannot contain a velocity condition");
    }
}
