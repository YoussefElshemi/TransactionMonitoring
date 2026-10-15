using Core.Enums;
using Core.Extensions;
using Core.Models.Cases;
using Core.ValueObjects;

namespace Core.Helpers;

public static class CaseAgeingResolver
{
    private const double AgeingFraction = 0.5;
    private const double DueSoonFraction = 0.8;

    public static CaseAgeing Resolve(Case @case, DateTimeOffset now)
    {
        var asOf = @case.Status.IsTerminal() && @case.ResolvedAt.HasValue
            ? @case.ResolvedAt.Value
            : now;

        var openedAt = (DateTimeOffset)@case.CreatedAt;
        var dueAt = (DateTimeOffset)@case.DueAt;

        var age = asOf - openedAt;
        var timeToDue = dueAt - asOf;
        var breached = timeToDue <= TimeSpan.Zero;

        return new CaseAgeing
        {
            Age = age,
            TimeToDue = timeToDue,
            Bucket = ResolveBucket(dueAt - openedAt, age, breached),
            IsBreached = new IsBreached(breached)
        };
    }

    private static CaseAgeBucket ResolveBucket(TimeSpan window, TimeSpan age, bool breached)
    {
        if (breached)
        {
            return CaseAgeBucket.Breached;
        }

        if (window <= TimeSpan.Zero)
        {
            return CaseAgeBucket.Breached;
        }

        var elapsed = age / window;

        return elapsed switch
        {
            >= DueSoonFraction => CaseAgeBucket.DueSoon,
            >= AgeingFraction => CaseAgeBucket.Ageing,
            _ => CaseAgeBucket.Fresh
        };
    }
}
