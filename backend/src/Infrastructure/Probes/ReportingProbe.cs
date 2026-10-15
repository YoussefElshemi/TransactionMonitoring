using System.Diagnostics;
using Core.Enums;
using Core.Interfaces.Services;
using Core.Models.Status;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Probes;

public class ReportingProbe(TransactionMonitoringDbContext context, TimeProvider timeProvider) : IServiceComponentProbe
{
    public ServiceComponent Component => ServiceComponent.Reporting;

    public async Task<RecordComponentProbeRequest> ProbeAsync(CancellationToken stoppingToken)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var since = timeProvider.GetUtcNow().AddHours(-1);

            await context.RuleEvaluations
                .IgnoreQueryFilters()
                .Where(x => x.CreatedAt >= since)
                .CountAsync(stoppingToken);

            stopwatch.Stop();

            return ProbeResultFactory.Healthy(Component, stopwatch);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            stopwatch.Stop();

            return ProbeResultFactory.Unhealthy(Component, stopwatch, exception.Message);
        }
    }
}
