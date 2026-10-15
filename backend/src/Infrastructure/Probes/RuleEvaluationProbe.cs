using System.Diagnostics;
using Core.Enums;
using Core.Interfaces.Services;
using Core.Models.Status;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Probes;

public class RuleEvaluationProbe(TransactionMonitoringDbContext context) : IServiceComponentProbe
{
    public ServiceComponent Component => ServiceComponent.RuleEvaluation;

    public async Task<RecordComponentProbeRequest> ProbeAsync(CancellationToken stoppingToken)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await context.RuleDeployments
                .IgnoreQueryFilters()
                .Where(x => x.StatusId == (int)RuleDeploymentStatus.Completed)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => x.RuleDeploymentId)
                .FirstOrDefaultAsync(stoppingToken);

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
