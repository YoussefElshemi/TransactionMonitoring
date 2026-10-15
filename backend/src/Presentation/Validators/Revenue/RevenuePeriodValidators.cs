using Core.Helpers;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.Revenue;

namespace Presentation.Validators.Revenue;

public class ListRecurringRevenueRequestValidator : AbstractValidator<ListRecurringRevenueRequestDto>
{
    public ListRecurringRevenueRequestValidator(TimeProvider timeProvider)
    {
        RuleFor(x => x.FromMonth).InclusiveBetween(1, 12);
        RuleFor(x => x.ToMonth).InclusiveBetween(1, 12);
        RuleFor(x => x.FromYear).InclusiveBetween(1, timeProvider.GetUtcNow().Year);
        RuleFor(x => x.ToYear).InclusiveBetween(1, timeProvider.GetUtcNow().Year);

        RuleFor(x => x)
            .Must(x => RevenuePeriod.IsOrdered(x.FromMonth, x.FromYear, x.ToMonth, x.ToYear))
            .WithMessage("The first month must not be after the last")
            .When(x => RevenuePeriod.IsValid(x.FromMonth, x.FromYear) && RevenuePeriod.IsValid(x.ToMonth, x.ToYear));
    }
}

public class GetRevenueConcentrationRequestValidator : AbstractValidator<GetRevenueConcentrationRequestDto>
{
    private const int MaxTopCount = 500;

    public GetRevenueConcentrationRequestValidator(TimeProvider timeProvider)
    {
        RuleFor(x => x.Month).InclusiveBetween(1, 12);
        RuleFor(x => x.Year).InclusiveBetween(1, timeProvider.GetUtcNow().Year);
        RuleFor(x => x.TopCount).InclusiveBetween(1, MaxTopCount);
    }
}

public class ListCustomerMarginRequestValidator : AbstractValidator<ListCustomerMarginRequestDto>
{
    public ListCustomerMarginRequestValidator(TimeProvider timeProvider)
    {
        RuleFor(x => x.Month).InclusiveBetween(1, 12);
        RuleFor(x => x.Year).InclusiveBetween(1, timeProvider.GetUtcNow().Year);
    }
}

public class GetRecoveryStatisticRequestValidator : AbstractValidator<GetRecoveryStatisticRequestDto>
{
    public GetRecoveryStatisticRequestValidator(TimeProvider timeProvider)
    {
        RuleFor(x => x.PeriodStart)
            .LessThanOrEqualTo(x => x.PeriodEnd)
            .WithMessage("The window must start before it ends");

        RuleFor(x => x.PeriodStart)
            .LessThanOrEqualTo(timeProvider.GetUtcNow())
            .WithMessage("A cohort cannot be opened in the future");
    }
}

public class ListCustomerHealthRequestValidator : AbstractValidator<ListCustomerHealthRequestDto>
{
    private const int MaxWindowDays = 365;

    public ListCustomerHealthRequestValidator()
    {
        RuleFor(x => x.WindowDays).InclusiveBetween(1, MaxWindowDays);
    }
}

public class ExportRevenueRecognitionRequestValidator : AbstractValidator<ExportRevenueRecognitionRequestDto>
{
    public ExportRevenueRecognitionRequestValidator(TimeProvider timeProvider)
    {
        RuleFor(x => x.FromMonth).InclusiveBetween(1, 12);
        RuleFor(x => x.ToMonth).InclusiveBetween(1, 12);
        RuleFor(x => x.FromYear).InclusiveBetween(1, timeProvider.GetUtcNow().Year);
        RuleFor(x => x.ToYear).InclusiveBetween(1, timeProvider.GetUtcNow().Year);

        RuleFor(x => x)
            .Must(x => RevenuePeriod.IsOrdered(x.FromMonth, x.FromYear, x.ToMonth, x.ToYear))
            .WithMessage("The first month must not be after the last")
            .When(x => RevenuePeriod.IsValid(x.FromMonth, x.FromYear) && RevenuePeriod.IsValid(x.ToMonth, x.ToYear));
    }
}

internal static class RevenuePeriod
{
    internal static bool IsValid(int month, int year)
    {
        return month is >= 1 and <= 12 && year >= 1;
    }

    internal static bool IsOrdered(int fromMonth, int fromYear, int toMonth, int toYear)
    {
        return BillingPeriod.Key(new Month(fromMonth), new Year(fromYear)) <=
               BillingPeriod.Key(new Month(toMonth), new Year(toYear));
    }
}
