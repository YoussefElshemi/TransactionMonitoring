using System.Net;
using Core.Models.VelocityProfiles;

namespace Core.Exceptions;

public class VelocityProfileInUseException(IReadOnlyList<VelocityProfileUsage> usages)
    : DomainException(
        $"The velocity profile cannot be changed while {usages.Count} rule(s) still read it: {Summarise(usages)}. " +
        "Change or delete those rules first.")
{
    public IReadOnlyList<VelocityProfileUsage> Usages { get; } = usages;

    public override HttpStatusCode GetStatusCode()
    {
        return HttpStatusCode.Conflict;
    }

    public override string GetTitle()
    {
        return "Velocity profile is in use";
    }

    public override object GetExtensions()
    {
        return new
        {
            Usages = Usages.Select(x => new
            {
                RuleId = x.RuleId.ToString(),
                FriendlyName = x.FriendlyName.ToString(),
                Metrics = x.Metrics.ConvertAll(metric => metric.ToString()),
                Thresholds = x.Thresholds.ConvertAll(threshold => threshold.ToString())
            }).ToList()
        };
    }

    private static string Summarise(IReadOnlyList<VelocityProfileUsage> usages)
    {
        return string.Join(", ", usages.Select(x => $"'{x.FriendlyName}'"));
    }
}
