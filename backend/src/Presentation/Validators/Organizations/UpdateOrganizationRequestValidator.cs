using System.Net;
using Core.Enums;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.Organizations;

namespace Presentation.Validators.Organizations;

public class UpdateOrganizationRequestValidator : AbstractValidator<UpdateOrganizationRequestDto>
{
    public UpdateOrganizationRequestValidator(IOrganizationService organizationService)
    {
        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await organizationService.ExistsAsync(new OrganizationId(x.OrganizationId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");

        RuleFor(x => x.Body.FriendlyName)
            .NotEmpty()
            .MaximumLength(50)
            .When(x => x.Body.FriendlyName != null);

        RuleFor(x => x.Body.Status)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .IsEnumName(typeof(OrganizationStatus), false)
            .Must(x => Enum.Parse<OrganizationStatus>(x, true) is OrganizationStatus.Enabled or OrganizationStatus.Disabled)
            .WithMessage($"Status must be one of '{nameof(OrganizationStatus.Enabled)}', '{nameof(OrganizationStatus.Disabled)}'")
            .When(x => x.Body.Status != null);

        RuleFor(x => x.Body.HoldWindow)
            .Must(holdWindow => holdWindow.Value > 0)
            .WithMessage("'Hold Window' must be greater than '0'.")
            .When(x => x.Body.HoldWindow is { IsSet: true, Value: not null });

        RuleFor(x => x.Body.HoldExpiryVerdict)
            .Must(holdExpiryVerdict => Enum.GetNames<HoldExpiryVerdict>().Contains(holdExpiryVerdict.Value, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"HoldExpiryVerdict must be one of: {string.Join(", ", Enum.GetNames<HoldExpiryVerdict>())}")
            .When(x => x.Body.HoldExpiryVerdict is { IsSet: true, Value: not null });

        // Refused here so a negative is a 400 rather than a 500 from the value object.
        RuleFor(x => x.Body.SandboxMonthlyCredits)
            .Must(sandboxMonthlyCredits => sandboxMonthlyCredits.Value >= 0)
            .WithMessage("'Sandbox Monthly Credits' must be greater than or equal to '0'.")
            .When(x => x.Body.SandboxMonthlyCredits is { IsSet: true, Value: not null });

        // Refused here so a typo is a 400 rather than a 500 from the value object.
        RuleFor(x => x.Body.DefaultLocale)
            .Must(defaultLocale => Locale.IsKnown(defaultLocale.Value!))
            .WithMessage("'Default Locale' must be a language tag this host knows, such as 'en-GB'.")
            .When(x => x.Body.DefaultLocale is { IsSet: true, Value: not null });

        RuleFor(x => x.Body.DefaultTimeZone)
            .Must(defaultTimeZone => DisplayTimeZone.IsKnown(defaultTimeZone.Value!))
            .WithMessage("'Default Time Zone' must be a time zone this host knows, such as 'Europe/London'.")
            .When(x => x.Body.DefaultTimeZone is { IsSet: true, Value: not null });
    }
}