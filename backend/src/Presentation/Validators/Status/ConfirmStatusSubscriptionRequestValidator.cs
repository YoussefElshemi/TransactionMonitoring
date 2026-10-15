using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.Status;

namespace Presentation.Validators.Status;

public class ConfirmStatusSubscriptionRequestValidator : AbstractValidator<ConfirmStatusSubscriptionRequestDto>
{
    public ConfirmStatusSubscriptionRequestValidator()
    {
        RuleFor(x => x.StatusSubscriptionId)
            .NotEmpty();

        RuleFor(x => x.Token)
            .NotEmpty()
            .MaximumLength(StatusSubscriptionToken.MaxLength);
    }
}
