using Core.Configs;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Models.Payments;
using Microsoft.Extensions.Options;
using Quartz;
using Serilog;

namespace Core.Jobs;

[DisallowConcurrentExecution]
public class CollectPaymentJob(
    IInvoiceRepository invoiceRepository,
    IPaymentRepository paymentRepository,
    IPaymentService paymentService,
    IOptions<AppConfig> appConfig,
    ILogger logger) : IJob
{
    public async ValueTask Execute(IJobExecutionContext context)
    {
        using var activity = OpenTelemetry.OpenTelemetry.MyActivitySource.StartActivity(GetType().Name);

        if (!appConfig.Value.PaymentConfig.CollectionEnabled)
        {
            return;
        }

        var invoices = await invoiceRepository.GetOverdueAsync(context.CancellationToken);

        foreach (var invoice in invoices)
        {
            var attempts = await paymentRepository.GetAttemptCountAsync(invoice.InvoiceId, context.CancellationToken);

            if (attempts > 0)
            {
                continue;
            }

            try
            {
                await paymentService.CollectAsync(
                    new CollectPaymentRequest
                    {
                        InvoiceId = invoice.InvoiceId,
                        PaymentMethodReference = null
                    },
                    context.CancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.Warning(
                    exception,
                    "Collecting invoice {InvoiceNumber} failed: {Message}",
                    invoice.InvoiceNumber?.ToString() ?? invoice.InvoiceId.ToString(),
                    exception.Message);
            }
        }
    }
}
