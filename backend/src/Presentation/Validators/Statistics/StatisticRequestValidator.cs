using FluentValidation;
using Presentation.Models.Statistics;

namespace Presentation.Validators.Statistics;

public class StatisticRequestValidator : AbstractValidator<StatisticRequestDto>
{
    public StatisticRequestValidator(TimeProvider timeProvider)
    {
        RuleFor(x => x.FromDate)
            .LessThanOrEqualTo(DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime))
            .WithMessage("From Date cannot be in the future.");

        RuleFor(x => new { x.FromDate, x.ToDate })
            .Must(x => x.FromDate <= x.ToDate)
            .WithMessage("From Date must be before or equal to To Date.");
    }
}