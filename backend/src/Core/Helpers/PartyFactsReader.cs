using Core.Models.Parties;
using Core.Models.Rules;

namespace Core.Helpers;

public static class PartyFactsReader
{
    public static object? ReadSingle(PartyConditionModel party, IReadOnlyList<PartyFacts>? parties, DateTimeOffset now)
    {
        return ReadAll(party, parties, now).FirstOrDefault();
    }

    public static List<object?> ReadAll(PartyConditionModel party, IReadOnlyList<PartyFacts>? parties, DateTimeOffset now)
    {
        return (parties ?? [])
            .Where(x => x.Role == party.Role)
            .Select(x => x.Read(party.Attribute, now))
            .ToList();
    }

    public static string Describe(PartyConditionModel party)
    {
        var attribute = char.ToLowerInvariant(party.Attribute.ToString()[0]) + party.Attribute.ToString()[1..];

        return $"party.{party.Role.ToString().ToLowerInvariant()}.{attribute}";
    }
}
