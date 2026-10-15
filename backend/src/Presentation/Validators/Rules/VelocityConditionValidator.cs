using Core.Enums;
using Core.Helpers;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.Rules;

namespace Presentation.Validators.Rules;

public class VelocityConditionValidator : AbstractValidator<VelocityConditionDto>
{
    public VelocityConditionValidator()
    {
        RuleFor(x => x.Role)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("A velocity condition must name the role whose history it reads.")
            .IsEnumName(typeof(PartyRole), false);

        RuleFor(x => x.Metric)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("A velocity condition must name the metric it aggregates.")
            .MaximumLength(VelocityMetricName.MaxLength);

        RuleFor(x => x.Function)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("A velocity condition must name the function it applies over the window.")
            .IsEnumName(typeof(VelocityFunction), false);

        RuleFor(x => x.IncludeCurrent)
            .NotNull()
            .WithMessage("A velocity condition must state whether the payment being judged counts towards its own window.");

        RuleFor(x => x.Band)
            .InclusiveBetween(0.0000001m, 1m)
            .When(x => x.Band.HasValue)
            .WithMessage("Band must be a fraction of the threshold greater than zero and no greater than one.");

        RuleFor(x => x.Threshold)
            .MaximumLength(VelocityThresholdName.MaxLength)
            .When(x => !string.IsNullOrWhiteSpace(x.Threshold));

        RuleFor(x => x.Window)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithMessage("A velocity condition must state the window it aggregates over.")
            .DependentRules(() => { RuleFor(x => x.Window!).SetValidator(new VelocityWindowValidator()); });
    }
}

public class VelocityWindowValidator : AbstractValidator<VelocityWindowDto>
{
    public VelocityWindowValidator()
    {
        RuleFor(x => x.Kind)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("A window must state whether it is Rolling or Calendar.")
            .IsEnumName(typeof(VelocityWindowKind), false);

        RuleFor(x => x.Unit)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("A window must state the unit it is measured in.")
            .IsEnumName(typeof(VelocityWindowUnit), false);

        RuleFor(x => x.TimeZone)
            .Must(x => WindowTimeZone.IsKnown(x!))
            .When(x => !string.IsNullOrWhiteSpace(x.TimeZone))
            .WithMessage("TimeZone must be a time zone this host knows, such as 'Europe/London'.");

        RuleFor(x => x)
            .Custom(Validate);
    }

    private static void Validate(VelocityWindowDto window, ValidationContext<VelocityWindowDto> context)
    {
        if (!Enum.TryParse<VelocityWindowKind>(window.Kind, true, out var kind) ||
            !Enum.TryParse<VelocityWindowUnit>(window.Unit, true, out var unit))
        {
            return;
        }

        if (kind == VelocityWindowKind.Calendar)
        {
            if (window.Length.HasValue)
            {
                context.AddFailure("A Calendar window covers the current period, so a Length means nothing on it.");
            }

            return;
        }

        if (!string.IsNullOrWhiteSpace(window.TimeZone))
        {
            context.AddFailure("A Rolling window counts back from the moment of judgement, so it has no boundary for a TimeZone to place.");
        }

        if (!window.Length.HasValue || window.Length.Value < 1)
        {
            context.AddFailure("A Rolling window must state a Length of at least one.");

            return;
        }

        var maxLength = VelocityWindowResolver.MaxLength(unit);

        if (window.Length.Value > maxLength)
        {
            context.AddFailure($"A Rolling window measured in {unit.ToString().ToLowerInvariant()}s must not be longer than {maxLength}.");
        }
    }
}
