using FluentValidation;
using Presentation.Models.Status;

namespace Presentation.Validators.Status;

public class CreateStatusSubscriptionRequestValidator : AbstractValidator<CreateStatusSubscriptionRequestDto>
{
    private const int MaxEmailLength = 320;

    public CreateStatusSubscriptionRequestValidator()
    {
        RuleFor(x => x.Body.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MaximumLength(MaxEmailLength)
            .EmailAddress();
    }
}
