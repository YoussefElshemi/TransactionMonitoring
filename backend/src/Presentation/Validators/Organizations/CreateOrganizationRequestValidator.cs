using FluentValidation;
using Presentation.Models.Organizations;

namespace Presentation.Validators.Organizations;

public class CreateOrganizationRequestValidator : AbstractValidator<CreateOrganizationRequestDto>
{
    public CreateOrganizationRequestValidator()
    {
        RuleFor(x => x.FriendlyName)
            .NotEmpty()
            .MaximumLength(50);
    }
}