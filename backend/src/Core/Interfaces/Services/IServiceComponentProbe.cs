using Core.Enums;
using Core.Models.Status;

namespace Core.Interfaces.Services;

public interface IServiceComponentProbe
{
    ServiceComponent Component { get; }

    // Must never throw: one throwing probe would take down the sweep for every component.
    Task<RecordComponentProbeRequest> ProbeAsync(CancellationToken stoppingToken);
}
