using Core.Configs;
using Core.Interfaces.Services;
using Microsoft.Extensions.Options;
using ILogger = Serilog.ILogger;

namespace Presentation.Availability;

// Not a Quartz job: the tally lives in this process's memory.
public sealed class AvailabilityFlushService(
    AvailabilityRecorder availabilityRecorder,
    IServiceScopeFactory serviceScopeFactory,
    IOptions<AppConfig> appConfig,
    ILogger logger) : BackgroundService
{
    private readonly StatusConfig _statusConfig = appConfig.Value.StatusConfig;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_statusConfig.AvailabilityFlushSeconds));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await FlushAsync(stoppingToken);
        }
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        await FlushAsync(CancellationToken.None);
        await base.StopAsync(stoppingToken);
    }

    private async Task FlushAsync(CancellationToken stoppingToken)
    {
        try
        {
            var pending = availabilityRecorder.Drain();

            if (pending.Count == 0)
            {
                return;
            }

            using var scope = serviceScopeFactory.CreateScope();
            var availabilityService = scope.ServiceProvider.GetRequiredService<IAvailabilityService>();

            await availabilityService.MergeAsync(pending, stoppingToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // Don't re-record drained counts: the merge may have succeeded before throwing.
            logger.Error(exception, "Flushing availability measurements failed: {Message}", exception.Message);
        }
    }
}
