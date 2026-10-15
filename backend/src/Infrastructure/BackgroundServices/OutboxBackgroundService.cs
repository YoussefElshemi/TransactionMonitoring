using System.Diagnostics;
using Core.Configs;
using Core.Interfaces.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;

namespace Infrastructure.BackgroundServices;

public class OutboxBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<AppConfig> appConfig,
    ILogger logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested && appConfig.Value.BackgroundServiceConfig.OutboxEnabled)
        {
            var currentActivity = OpenTelemetry.OpenTelemetry.MyActivitySource.StartActivity(GetType().Name);

            try
            {
                using var scope = scopeFactory.CreateScope();
                var outboxMessageService = scope.ServiceProvider.GetRequiredService<IOutboxMessageService>();

                var pendingMessages = await outboxMessageService.ListPendingAsync(stoppingToken);

                foreach (var message in pendingMessages)
                {
                    try
                    {
                        await outboxMessageService.ProcessAsync(message, stoppingToken);
                    }
                    catch (Exception messageException)
                    {
                        logger.Error(messageException, "Error while processing outbox message: {error}", messageException.Message);
                        currentActivity?.AddException(messageException);
                        currentActivity?.SetStatus(ActivityStatusCode.Error, messageException.Message);
                    }
                }
            }
            catch (Exception exception)
            {
                logger.Error(exception, "Error while processing outbox batch: {error}", exception.Message);
                currentActivity?.AddException(exception);
                currentActivity?.SetStatus(ActivityStatusCode.Error, exception.Message);
            }
            finally
            {
                currentActivity?.Stop();
                await Task.Delay(appConfig.Value.BackgroundServiceConfig.OutboxIntervalInMs, stoppingToken);
            }
        }
    }
}