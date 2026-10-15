using System.Net;
using Core.Models.PartyMappings;

namespace Core.Exceptions;

public class PartyMappingInUseException(IReadOnlyList<PartyMappingUsage> usages)
    : DomainException(
        $"The party mapping cannot be changed while {usages.Count} rule(s) still read a party: {Summarise(usages)}. " +
        "Change or delete those rules first.")
{
    public IReadOnlyList<PartyMappingUsage> Usages { get; } = usages;

    public override HttpStatusCode GetStatusCode()
    {
        return HttpStatusCode.Conflict;
    }

    public override string GetTitle()
    {
        return "Party mapping is in use";
    }

    public override object GetExtensions()
    {
        return new
        {
            Usages = Usages.Select(x => new
            {
                RuleId = x.RuleId.ToString(),
                FriendlyName = x.FriendlyName.ToString(),
                Roles = x.Roles.ConvertAll(role => role.ToString())
            }).ToList()
        };
    }

    private static string Summarise(IReadOnlyList<PartyMappingUsage> usages)
    {
        return string.Join(", ", usages.Select(x => $"'{x.FriendlyName}'"));
    }
}
