using Core.Interfaces.Services;
using Core.Models.Invoices;
using Core.ValueObjects;
using Quartz;

namespace Core.Jobs;

[DisallowConcurrentExecution]
public class GenerateInvoiceJob(
    IOrganizationService organizationService,
    IInvoiceService invoiceService,
    ISubscriptionService subscriptionService,
    IBillingProfileService billingProfileService,
    TimeProvider timeProvider) : IJob
{
    public async ValueTask Execute(IJobExecutionContext context)
    {
        using var activity = OpenTelemetry.OpenTelemetry.MyActivitySource.StartActivity(GetType().Name);

        var organizations = await organizationService.GetAllAsync(context.CancellationToken);
        var previousMonth = timeProvider.GetUtcNow().AddMonths(-1);
        var endOfPreviousMonth = new DateTimeOffset(
            previousMonth.Year,
            previousMonth.Month,
            DateTime.DaysInMonth(previousMonth.Year, previousMonth.Month),
            23, 59, 59, TimeSpan.Zero);

        foreach (var organization in organizations)
        {
            if (await invoiceService.ExistsByDateAsync(
                    organization.OrganizationId,
                    new Month(previousMonth.Month),
                    new Year(previousMonth.Year),
                    new IncludeDeleted(false),
                    context.CancellationToken))
            {
                continue;
            }

            var subscription = await subscriptionService.GetInEffectAsync(
                organization.OrganizationId, endOfPreviousMonth, context.CancellationToken);

            if (subscription is null)
            {
                continue;
            }

            if (!await billingProfileService.ExistsAsync(organization.OrganizationId, context.CancellationToken))
            {
                continue;
            }

            var generateInvoiceRequest = new GenerateInvoiceRequest
            {
                OrganizationId = organization.OrganizationId,
                Month = new Month(previousMonth.Month),
                Year = new Year(previousMonth.Year),
                CreatedByUserId = null
            };

            var invoice = await invoiceService.GenerateAsync(generateInvoiceRequest, context.CancellationToken);

            await invoiceService.IssueAsync(invoice.InvoiceId, context.CancellationToken);
        }
    }
}