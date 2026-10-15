using Core.Exceptions;
using Core.Interfaces.Services;
using Core.Models.ServiceCredits;
using Core.ValueObjects;
using Quartz;
using ILogger = Serilog.ILogger;

namespace Core.Jobs;

[DisallowConcurrentExecution]
public class IssueServiceCreditJob(
    IAvailabilityCommitmentService availabilityCommitmentService,
    IServiceCreditService serviceCreditService,
    TimeProvider timeProvider,
    ILogger logger) : IJob
{
    public async ValueTask Execute(IJobExecutionContext context)
    {
        using var activity = OpenTelemetry.OpenTelemetry.MyActivitySource.StartActivity(GetType().Name);

        var previousMonth = timeProvider.GetUtcNow().AddMonths(-1);
        var month = new Month(previousMonth.Month);
        var year = new Year(previousMonth.Year);

        var endOfPreviousMonth = new DateTimeOffset(previousMonth.Year, previousMonth.Month, 1, 0, 0, 0, TimeSpan.Zero)
            .AddMonths(1)
            .AddTicks(-1);

        var commitments = await availabilityCommitmentService.ListInEffectAsync(endOfPreviousMonth, context.CancellationToken);

        foreach (var commitment in commitments)
        {
            if (await serviceCreditService.ExistsByPeriodAsync(commitment.OrganizationId, month, year, context.CancellationToken))
            {
                continue;
            }

            try
            {
                await serviceCreditService.IssueAsync(new IssueServiceCreditRequest
                {
                    OrganizationId = commitment.OrganizationId,
                    Month = month,
                    Year = year
                }, context.CancellationToken);
            }
            catch (DomainException exception)
            {
                logger.Error(
                    exception,
                    "Issuing the service credit for organization {OrganizationId} for {Month}/{Year} failed: {Message}",
                    commitment.OrganizationId.ToString(),
                    month.ToString(),
                    year.ToString(),
                    exception.Message);
            }
        }
    }
}
