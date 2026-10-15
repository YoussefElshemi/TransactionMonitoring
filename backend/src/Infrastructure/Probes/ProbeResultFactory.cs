using System.Diagnostics;
using Core.Enums;
using Core.Models.Status;
using Core.ValueObjects;

namespace Infrastructure.Probes;

public static class ProbeResultFactory
{
    public static RecordComponentProbeRequest Healthy(ServiceComponent component, Stopwatch stopwatch, string? detail = null)
    {
        return new RecordComponentProbeRequest
        {
            Component = component,
            Source = ProbeSource.Internal,
            ObservedAt = null,
            IsHealthy = new IsHealthy(true),
            LatencyMilliseconds = ToLatency(stopwatch),
            Detail = detail is null ? null : new ProbeDetail(detail)
        };
    }

    public static RecordComponentProbeRequest Unhealthy(ServiceComponent component, Stopwatch stopwatch, string detail)
    {
        return new RecordComponentProbeRequest
        {
            Component = component,
            Source = ProbeSource.Internal,
            ObservedAt = null,
            IsHealthy = new IsHealthy(false),
            LatencyMilliseconds = ToLatency(stopwatch),
            Detail = new ProbeDetail(Truncate(detail))
        };
    }

    private static LatencyMilliseconds ToLatency(Stopwatch stopwatch)
    {
        return new LatencyMilliseconds((int)Math.Min(stopwatch.ElapsedMilliseconds, int.MaxValue));
    }

    private static string Truncate(string detail)
    {
        var collapsed = detail.ReplaceLineEndings(" ").Trim();

        return collapsed.Length <= ProbeDetail.MaxLength
            ? collapsed
            : collapsed[..ProbeDetail.MaxLength];
    }
}
