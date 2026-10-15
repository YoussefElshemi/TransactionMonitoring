using System.Diagnostics;
using Core.Enums;
using Core.Interfaces.Services;
using Core.Models.Status;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Probes;

public class ApiProbe(TransactionMonitoringDbContext context) : IServiceComponentProbe
{
    public ServiceComponent Component => ServiceComponent.Api;

    public async Task<RecordComponentProbeRequest> ProbeAsync(CancellationToken stoppingToken)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await context.Database.ExecuteSqlRawAsync("SELECT 1", stoppingToken);
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
