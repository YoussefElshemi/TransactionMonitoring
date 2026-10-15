using Core.Configs;
using Core.Interfaces.Services;
using Microsoft.Extensions.Options;
using ILogger = Serilog.ILogger;

namespace Presentation.RateLimiting;

public sealed class RateLimitOverrideRefresher(
    RateLimitOverrideSnapshot rateLimitOverrideSnapshot,
    IServiceScopeFactory serviceScopeFactory,
    IOptions<AppConfig> appConfig,
    ILogger logger) : BackgroundService
{
    private readonly RateLimitConfig _config = appConfig.Value.RateLimitConfig;

    public override async Task StartAsync(CancellationToken stoppingToken)
    {
        await RefreshAsync(stoppingToken);
        await base.StartAsync(stoppingToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_config.OverrideRefreshSeconds));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RefreshAsync(stoppingToken);
        }
    }

    private async Task RefreshAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = serviceScopeFactory.CreateScope();
            var rateLimitOverrideService = scope.ServiceProvider.GetRequiredService<IRateLimitOverrideService>();

            var rateLimitOverrides = await rateLimitOverrideService.ListActiveAsync(stoppingToken);

            rateLimitOverrideSnapshot.Replace(rateLimitOverrides);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.Warning(exception, "Refreshing rate limit overrides failed: {Message}", exception.Message);
        }
    }
}
