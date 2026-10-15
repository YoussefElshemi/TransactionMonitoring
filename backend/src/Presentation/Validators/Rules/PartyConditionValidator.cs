using Core.Enums;
using FluentValidation;
using Presentation.Models.Rules;

namespace Presentation.Validators.Rules;

public class PartyConditionValidator : AbstractValidator<PartyConditionDto>
{
    public PartyConditionValidator()
    {
        RuleFor(x => x.Role)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("A party condition must name the role it reads.")
            .IsEnumName(typeof(PartyRole), false);

        RuleFor(x => x.Attribute)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("A party condition must name the attribute it reads.")
            .IsEnumName(typeof(PartyAttribute), false);
    }
}
