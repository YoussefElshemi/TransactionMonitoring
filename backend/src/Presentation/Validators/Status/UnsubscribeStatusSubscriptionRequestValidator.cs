using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.Status;

namespace Presentation.Validators.Status;

public class UnsubscribeStatusSubscriptionRequestValidator : AbstractValidator<UnsubscribeStatusSubscriptionRequestDto>
{
    public UnsubscribeStatusSubscriptionRequestValidator()
    {
        RuleFor(x => x.StatusSubscriptionId)
            .NotEmpty();

        RuleFor(x => x.Token)
            .NotEmpty()
            .MaximumLength(StatusSubscriptionToken.MaxLength);
    }
}
