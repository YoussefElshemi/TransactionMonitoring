using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.Localization;

namespace Presentation.Validators.Localization;

public class UpdateDisplayPreferenceRequestValidator : AbstractValidator<UpdateDisplayPreferenceRequestDto>
{
    public UpdateDisplayPreferenceRequestValidator()
    {
        RuleFor(x => x.Locale)
            .Must(locale => Locale.IsKnown(locale.Value!))
            .WithMessage("'Locale' must be a language tag this host knows, such as 'fr-FR'.")
            .When(x => x.Locale is { IsSet: true, Value: not null });

        RuleFor(x => x.TimeZone)
            .Must(timeZone => DisplayTimeZone.IsKnown(timeZone.Value!))
            .WithMessage("'Time Zone' must be a time zone this host knows, such as 'Europe/London'.")
            .When(x => x.TimeZone is { IsSet: true, Value: not null });
    }
}
