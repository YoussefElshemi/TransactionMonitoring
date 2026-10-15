using Core.Helpers;
using FluentValidation;
using Presentation.Models.ApiClients;

namespace Presentation.Validators.ApiClients;

public class CreateApiClientRequestValidator : AbstractValidator<CreateApiClientRequestDto>
{
    public CreateApiClientRequestValidator()
    {
        RuleFor(x => x.FriendlyName)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.Scopes)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("At least one scope is required.")
            .Must(x => x.All(scope => ApiScopeHelper.TryParse(scope, out _)))
            .WithMessage("One or more scopes are not recognised.");
    }
}
