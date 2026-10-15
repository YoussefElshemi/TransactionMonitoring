using Core.Configs;
using Core.Helpers;
using Core.Interfaces.Services;
using Microsoft.Extensions.Options;
using Quartz;

namespace Core.Jobs;

[DisallowConcurrentExecution]
public class RecordExchangeRateJob(
    IExchangeRateFeed exchangeRateFeed,
    IExchangeRateService exchangeRateService,
    IOptions<AppConfig> appConfig) : IJob
{
    public async ValueTask Execute(IJobExecutionContext context)
    {
        using var activity = OpenTelemetry.OpenTelemetry.MyActivitySource.StartActivity(GetType().Name);

        if (!appConfig.Value.ExchangeRateFeedConfig.Enabled)
        {
            return;
        }

        var exchangeRateQuote = await exchangeRateFeed.GetLatestAsync(context.CancellationToken);

        if (exchangeRateQuote is null)
        {
            return;
        }

        foreach (var createExchangeRateRequest in ExchangeRateDeriver.Derive(exchangeRateQuote))
        {
            // The feed republishes the last working day's rates on weekends and holidays.
            if (await exchangeRateService.ExistsAsync(
                    createExchangeRateRequest.FromCurrency,
                    createExchangeRateRequest.ToCurrency,
                    createExchangeRateRequest.EffectiveFrom!.Value,
                    context.CancellationToken))
            {
                continue;
            }

            await exchangeRateService.CreateAsync(createExchangeRateRequest, context.CancellationToken);
        }
    }
}
