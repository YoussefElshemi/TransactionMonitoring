using System.Diagnostics;
using Core.Configs;
using Core.Enums;
using Core.Interfaces.Services;
using Core.Models.Status;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Infrastructure.Probes;

public class WebhookProbe(
    TransactionMonitoringDbContext context,
    IOptions<AppConfig> appConfig,
    TimeProvider timeProvider) : IServiceComponentProbe
{
    private readonly StatusConfig _statusConfig = appConfig.Value.StatusConfig;

    public ServiceComponent Component => ServiceComponent.Webhooks;

    public async Task<RecordComponentProbeRequest> ProbeAsync(CancellationToken stoppingToken)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var oldest = await context.OutboxMessages
                .IgnoreQueryFilters()
                .Where(x => !x.IsProcessed && !x.IsDeleted && x.SuppressedAt == null)
                .OrderBy(x => x.CreatedAt)
                .Select(x => (DateTimeOffset?)x.CreatedAt)
                .FirstOrDefaultAsync(stoppingToken);

            stopwatch.Stop();

            if (oldest is null)
            {
                return ProbeResultFactory.Healthy(Component, stopwatch);
            }

            var age = timeProvider.GetUtcNow() - oldest.Value;

            return age.TotalMilliseconds <= _statusConfig.DegradedLatencyMilliseconds * 60d
                ? ProbeResultFactory.Healthy(Component, stopwatch)
                : ProbeResultFactory.Unhealthy(Component, stopwatch,
                    $"The oldest undelivered notification has been queued for {(int)age.TotalSeconds} seconds.");
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            stopwatch.Stop();

            return ProbeResultFactory.Unhealthy(Component, stopwatch, exception.Message);
        }
    }
}
