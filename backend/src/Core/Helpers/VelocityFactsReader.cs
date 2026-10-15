using System.Globalization;
using Core.Enums;
using Core.Models.Parties;
using Core.Models.Rules;
using Core.Models.VelocityObservations;

namespace Core.Helpers;

public static class VelocityFactsReader
{
    public static object? ReadSingle(VelocityConditionModel velocity, IReadOnlyList<PartyFacts>? parties, VelocityFacts? velocityFacts, DateTimeOffset now)
    {
        return ReadAll(velocity, parties, velocityFacts, now).FirstOrDefault();
    }

    public static List<object?> ReadAll(VelocityConditionModel velocity, IReadOnlyList<PartyFacts>? parties, VelocityFacts? velocityFacts, DateTimeOffset now)
    {
        var facts = velocityFacts ?? VelocityFacts.Empty;
        var query = VelocityWindowResolver.Query(velocity, facts.Thresholds, now, facts.CurrentTransactionId);

        return (parties ?? [])
            .Where(x => x.Role == velocity.Role)
            .Select(party => VelocityAggregator.Aggregate(velocity.Function, query, Read(facts, query, party)))
            .ToList();
    }

    public static string Describe(VelocityConditionModel velocity)
    {
        var function = Camel(velocity.Function.ToString());
        var subject = $"velocity.{velocity.Role.ToString().ToLowerInvariant()}.{velocity.Metric}";

        return $"{function}({subject}, {DescribeWindow(velocity.Window)}, {(velocity.IncludeCurrent ? "including" : "excluding")} current)";
    }

    private static VelocityAggregate Read(VelocityFacts facts, VelocityWindowQuery query, PartyFacts party)
    {
        foreach (var window in facts.Windows)
        {
            if (window.PartyId == party.PartyId && window.Query == query)
            {
                return window.Aggregate;
            }
        }

        return VelocityAggregate.Empty;
    }

    private static string DescribeWindow(VelocityWindowModel? window)
    {
        if (window == null)
        {
            return "no window";
        }

        var unit = window.Unit.ToString().ToLowerInvariant();

        if (window.Kind == VelocityWindowKind.Calendar)
        {
            return $"this calendar {unit}{(string.IsNullOrWhiteSpace(window.TimeZone) ? string.Empty : $" in {window.TimeZone}")}";
        }

        var length = window.Length ?? 1;

        return $"last {length.ToString(CultureInfo.InvariantCulture)} {unit}{(length == 1 ? string.Empty : "s")}";
    }

    private static string Camel(string name)
    {
        return char.ToLowerInvariant(name[0]) + name[1..];
    }
}
