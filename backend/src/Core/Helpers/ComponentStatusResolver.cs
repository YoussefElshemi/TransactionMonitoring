using Core.Enums;
using Core.Models.Status;

namespace Core.Helpers;

public static class ComponentStatusResolver
{
    public static ComponentStatus Resolve(
        IReadOnlyList<ComponentProbe> recentProbes,
        IReadOnlyList<Incident> activeIncidents,
        bool underAnnouncedMaintenance,
        int unhealthyProbeThreshold,
        int degradedLatencyMilliseconds)
    {
        var probed = FromProbes(recentProbes, unhealthyProbeThreshold, degradedLatencyMilliseconds);
        var declared = FromIncidents(activeIncidents);

        var status = Worst(probed, declared);

        return status == ComponentStatus.Operational && underAnnouncedMaintenance
            ? ComponentStatus.UnderMaintenance
            : status;
    }

    public static ComponentStatus Worst(IEnumerable<ComponentStatus> statuses)
    {
        return statuses.Aggregate(ComponentStatus.Operational, Worst);
    }

    public static ComponentStatus Worst(ComponentStatus first, ComponentStatus second)
    {
        return Severity(first) >= Severity(second) ? first : second;
    }

    // Not the enum's order: UnderMaintenance is numbered last only to avoid renumbering, but ranks mildest.
    public static int Severity(ComponentStatus componentStatus)
    {
        return componentStatus switch
        {
            ComponentStatus.Operational => 0,
            ComponentStatus.UnderMaintenance => 1,
            ComponentStatus.Degraded => 2,
            ComponentStatus.PartialOutage => 3,
            ComponentStatus.MajorOutage => 4,
            _ => 0
        };
    }

    public static ComponentStatus FromImpact(IncidentImpact incidentImpact)
    {
        return incidentImpact switch
        {
            IncidentImpact.None => ComponentStatus.Operational,
            IncidentImpact.Minor => ComponentStatus.Degraded,
            IncidentImpact.Major => ComponentStatus.PartialOutage,
            IncidentImpact.Critical => ComponentStatus.MajorOutage,
            _ => ComponentStatus.Operational
        };
    }

    private static ComponentStatus FromProbes(
        IReadOnlyList<ComponentProbe> recentProbes,
        int unhealthyProbeThreshold,
        int degradedLatencyMilliseconds)
    {
        if (recentProbes.Count == 0)
        {
            return ComponentStatus.Operational;
        }

        var consecutiveFailures = 0;

        foreach (var probe in recentProbes)
        {
            if (probe.IsHealthy)
            {
                break;
            }

            consecutiveFailures++;
        }

        if (consecutiveFailures >= unhealthyProbeThreshold)
        {
            return ComponentStatus.MajorOutage;
        }

        if (consecutiveFailures > 0)
        {
            return ComponentStatus.Degraded;
        }

        return recentProbes[0].LatencyMilliseconds > degradedLatencyMilliseconds
            ? ComponentStatus.Degraded
            : ComponentStatus.Operational;
    }

    private static ComponentStatus FromIncidents(IReadOnlyList<Incident> activeIncidents)
    {
        return activeIncidents.Count == 0
            ? ComponentStatus.Operational
            : Worst(activeIncidents.Select(x => FromImpact(x.Impact)));
    }
}
