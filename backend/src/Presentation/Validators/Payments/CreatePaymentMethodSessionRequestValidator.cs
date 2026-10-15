using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.Payments;

namespace Presentation.Validators.Payments;

public class CreatePaymentMethodSessionRequestValidator : AbstractValidator<CreatePaymentMethodSessionRequestDto>
{
    public CreatePaymentMethodSessionRequestValidator(IOrganizationService organizationService)
    {
        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await organizationService.ExistsAsync(new OrganizationId(x.OrganizationId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");

        RuleFor(x => x.Body.ReturnUrl)
            .Must(BeAnHttpsUrl)
            .WithMessage("Return URL must be an absolute https URL.");

        RuleFor(x => x.Body.CancelUrl)
            .Must(BeAnHttpsUrl)
            .WithMessage("Cancel URL must be an absolute https URL.");
    }

    private static bool BeAnHttpsUrl(string value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps;
    }
}
