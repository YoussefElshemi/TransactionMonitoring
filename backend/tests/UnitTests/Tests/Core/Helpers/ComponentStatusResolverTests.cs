using Core.Enums;
using Core.Helpers;
using Core.Models.Status;
using Core.ValueObjects;
using FluentAssertions;
using UnitTests.TestHelpers;

namespace UnitTests.Tests.Core.Helpers;

public class ComponentStatusResolverTests : BaseTestClass
{
    private const int Threshold = 3;
    private const int DegradedLatency = 2000;

    [Fact]
    public void Resolve_GivenHealthyProbesAndNothingElse_IsOperational()
    {
        // Arrange
        var probes = Probes(true, true, true);

        // Act
        var status = ComponentStatusResolver.Resolve(probes, [], false, Threshold, DegradedLatency);

        // Assert
        status.Should().Be(ComponentStatus.Operational);
    }

    [Fact]
    public void Resolve_GivenFewerConsecutiveFailuresThanTheThreshold_IsDegraded()
    {
        // Arrange
        var probes = Probes(false, true, true);

        // Act
        var status = ComponentStatusResolver.Resolve(probes, [], false, Threshold, DegradedLatency);

        // Assert
        status.Should().Be(ComponentStatus.Degraded);
    }

    [Fact]
    public void Resolve_GivenAFullRunOfFailures_IsAMajorOutage()
    {
        // Arrange
        var probes = Probes(false, false, false);

        // Act
        var status = ComponentStatusResolver.Resolve(probes, [], false, Threshold, DegradedLatency);

        // Assert
        status.Should().Be(ComponentStatus.MajorOutage);
    }

    [Fact]
    public void Resolve_GivenAHealthyButSlowProbe_IsDegraded()
    {
        // Arrange
        var probes = new List<ComponentProbe> { Probe(true, DegradedLatency + 1) };

        // Act
        var status = ComponentStatusResolver.Resolve(probes, [], false, Threshold, DegradedLatency);

        // Assert
        status.Should().Be(ComponentStatus.Degraded);
    }

    [Fact]
    public void Resolve_GivenAnOpenIncidentWorseThanTheProbes_TakesTheIncident()
    {
        // Arrange
        var probes = Probes(true, true, true);

        // Act
        var status = ComponentStatusResolver.Resolve(probes, [Incident(IncidentImpact.Critical)], false, Threshold, DegradedLatency);

        // Assert
        status.Should().Be(ComponentStatus.MajorOutage);
    }

    [Fact]
    public void Resolve_GivenAnnouncedMaintenanceAndNothingWrong_IsUnderMaintenance()
    {
        // Arrange
        var probes = Probes(true, true, true);

        // Act
        var status = ComponentStatusResolver.Resolve(probes, [], true, Threshold, DegradedLatency);

        // Assert
        status.Should().Be(ComponentStatus.UnderMaintenance);
    }

    [Fact]
    public void Resolve_GivenAnnouncedMaintenanceAndARunOfFailures_StillReportsTheOutage()
    {
        // Arrange
        var probes = Probes(false, false, false);

        // Act
        var status = ComponentStatusResolver.Resolve(probes, [], true, Threshold, DegradedLatency);

        // Assert
        status.Should().Be(ComponentStatus.MajorOutage);
    }

    [Fact]
    public void Resolve_GivenNoProbesAtAll_IsOperational()
    {
        // Act
        var status = ComponentStatusResolver.Resolve([], [], false, Threshold, DegradedLatency);

        // Assert
        status.Should().Be(ComponentStatus.Operational);
    }

    [Fact]
    public void Worst_GivenAMixOfStatuses_ReturnsTheMostSevere()
    {
        // Act
        var worst = ComponentStatusResolver.Worst(
        [
            ComponentStatus.Operational,
            ComponentStatus.UnderMaintenance,
            ComponentStatus.Degraded,
            ComponentStatus.PartialOutage
        ]);

        // Assert
        worst.Should().Be(ComponentStatus.PartialOutage);
    }

    [Fact]
    public void Worst_GivenOnlyMaintenanceAndOperational_PrefersMaintenance()
    {
        // Act
        var worst = ComponentStatusResolver.Worst([ComponentStatus.Operational, ComponentStatus.UnderMaintenance]);

        // Assert
        worst.Should().Be(ComponentStatus.UnderMaintenance);
    }

    private static List<ComponentProbe> Probes(params bool[] healthy)
    {
        return healthy.Select(x => Probe(x, 10)).ToList();
    }

    private static ComponentProbe Probe(bool healthy, int latencyMilliseconds)
    {
        return new ComponentProbe
        {
            ComponentProbeId = new ComponentProbeId(Guid.NewGuid()),
            Component = ServiceComponent.Api,
            Source = ProbeSource.Internal,
            ObservedAt = new ObservedAt(TestTimeConstants.Today),
            IsHealthy = new IsHealthy(healthy),
            LatencyMilliseconds = new LatencyMilliseconds(latencyMilliseconds),
            Detail = null,
            CreatedAt = new CreatedAt(TestTimeConstants.Today),
            UpdatedAt = new UpdatedAt(TestTimeConstants.Today)
        };
    }

    private static Incident Incident(IncidentImpact impact)
    {
        return new Incident
        {
            IncidentId = new IncidentId(Guid.NewGuid()),
            IncidentNumber = new IncidentNumber("INC0000000001"),
            Title = new IncidentTitle("Elevated error rates"),
            Summary = new IncidentSummary("Some payments are being refused."),
            Status = IncidentStatus.Investigating,
            Impact = impact,
            Components = [ServiceComponent.Api],
            StartedAt = new StartedAt(TestTimeConstants.Today),
            ResolvedAt = null,
            RootCause = null,
            Remediation = null,
            ReviewPublishedAt = null,
            CreatedByUserId = null,
            Updates = [],
            CreatedAt = new CreatedAt(TestTimeConstants.Today),
            UpdatedAt = new UpdatedAt(TestTimeConstants.Today),
            IsDeleted = new IsDeleted(false),
            DeletedAt = null
        };
    }
}
