using Core.Configs;
using Core.Enums;
using Core.Interfaces.Services;
using Core.Jobs;
using Core.Models.Status;
using Core.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Quartz;
using UnitTests.TestHelpers;

namespace UnitTests.Tests.Core.Jobs;

public class ProbeServiceComponentJobTests : BaseTestClass
{
    private readonly Mock<IStatusService> _mockStatusService = new();
    private readonly Mock<IIncidentService> _mockIncidentService = new();
    private readonly Mock<IOptions<AppConfig>> _mockConfig = new();
    private readonly Mock<IJobExecutionContext> _mockJobExecutionContext = new();

    public ProbeServiceComponentJobTests()
    {
        _mockConfig.Setup(x => x.Value).Returns(new AppConfig
        {
            StatusConfig = new StatusConfig { RaiseIncidentsFromProbes = false }
        });

        _mockJobExecutionContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);
    }

    [Fact]
    public async Task Execute_GivenRecordingAProbeThrows_StillProbesEveryOtherComponent()
    {
        // Arrange
        var probes = Enum.GetValues<ServiceComponent>().Select(StubProbe).ToList();

        _mockStatusService
            .Setup(x => x.RecordProbeAsync(It.IsAny<RecordComponentProbeRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("The database is unreachable."));

        var job = CreateJob(probes.Select(x => x.Object));

        // Act
        var act = async () => await job.Execute(_mockJobExecutionContext.Object);

        // Assert
        await act.Should().NotThrowAsync();

        foreach (var probe in probes)
        {
            probe.Verify(x => x.ProbeAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    [Fact]
    public async Task Execute_GivenHealthyProbes_RecordsOnePerComponent()
    {
        // Arrange
        var probes = Enum.GetValues<ServiceComponent>().Select(StubProbe).ToList();
        var job = CreateJob(probes.Select(x => x.Object));

        // Act
        await job.Execute(_mockJobExecutionContext.Object);

        // Assert
        _mockStatusService.Verify(
            x => x.RecordProbeAsync(It.IsAny<RecordComponentProbeRequest>(), It.IsAny<CancellationToken>()),
            Times.Exactly(Enum.GetValues<ServiceComponent>().Length));
    }

    private ProbeServiceComponentJob CreateJob(IEnumerable<IServiceComponentProbe> probes)
    {
        return new ProbeServiceComponentJob(
            probes,
            _mockStatusService.Object,
            _mockIncidentService.Object,
            _mockConfig.Object,
            MockLogger.Object);
    }

    private static Mock<IServiceComponentProbe> StubProbe(ServiceComponent component)
    {
        var mockProbe = new Mock<IServiceComponentProbe>();

        mockProbe.Setup(x => x.Component).Returns(component);
        mockProbe
            .Setup(x => x.ProbeAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RecordComponentProbeRequest
            {
                Component = component,
                Source = ProbeSource.Internal,
                ObservedAt = null,
                IsHealthy = new IsHealthy(true),
                LatencyMilliseconds = new LatencyMilliseconds(5),
                Detail = null
            });

        return mockProbe;
    }
}
