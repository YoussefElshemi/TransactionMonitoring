using System.Diagnostics;
using Core.Configs;
using Core.Enums;
using Core.Helpers;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Models.VelocityObservations;
using Core.Models.VelocityProfiles;
using Core.ValueObjects;
using Microsoft.Extensions.Options;
using Quartz;
using Serilog;

namespace Core.Jobs;

[DisallowConcurrentExecution]
public class VelocityBackfillJob(
    IVelocityBackfillRepository velocityBackfillRepository,
    IVelocityProfileRepository velocityProfileRepository,
    IVelocityObservationRepository velocityObservationRepository,
    IUnitOfWork unitOfWork,
    IOptions<AppConfig> appConfig,
    TimeProvider timeProvider,
    IModeContext modeContext,
    ILogger logger) : IJob
{
    public async ValueTask Execute(IJobExecutionContext context)
    {
        using var activity = OpenTelemetry.OpenTelemetry.MyActivitySource.StartActivity(GetType().Name);

        var ct = context.CancellationToken;
        var chunkSize = appConfig.Value.BackgroundServiceConfig.VelocityBackfillChunkSize;

        var velocityBackfillId = new VelocityBackfillId(Guid.Parse(context.JobDetail.Key.Name));
        var velocityBackfill = await velocityBackfillRepository.GetAsync(velocityBackfillId, ct);

        modeContext.Current = velocityBackfill.Mode;

        if (velocityBackfill.Status is VelocityBackfillStatus.Completed or VelocityBackfillStatus.Failed)
        {
            return;
        }

        var totalTransactions = velocityBackfill.TotalTransactions;
        var processedCount = velocityBackfill.ProcessedCount;

        try
        {
            var velocityProfile = await velocityProfileRepository.FindAsync(velocityBackfill.OrganizationId, new IncludeDeleted(true), ct);
            var metrics = Metrics(velocityProfile, velocityBackfill);

            if (metrics.Count == 0)
            {
                await CompleteAsync(velocityBackfill, totalTransactions, processedCount, ct);

                return;
            }

            if (velocityBackfill.Status == VelocityBackfillStatus.Pending)
            {
                totalTransactions = await velocityBackfillRepository.CountTransactionsAsync(velocityBackfill.OrganizationId, ct);
                processedCount = new ProcessedCount(0);

                await velocityBackfillRepository.UpdateAsync(
                    velocityBackfillId, VelocityBackfillStatus.Running, totalTransactions, processedCount, null, ct);

                await unitOfWork.SaveChangesAsync(ct);
            }

            while (processedCount < totalTransactions)
            {
                var chunk = await velocityBackfillRepository.ListTransactionChunkAsync(
                    velocityBackfill.OrganizationId, processedCount, chunkSize, ct);

                if (chunk.Count == 0)
                {
                    break;
                }

                var now = timeProvider.GetUtcNow();

                var observations = chunk.SelectMany(transaction =>
                {
                    var values = VelocityMetricReader.Read(transaction.Payload, metrics);

                    return transaction.TransactionParties.SelectMany(link => values.Select(value => new VelocityObservation
                    {
                        Mode = modeContext.CurrentOrLive,
                        VelocityObservationId = new VelocityObservationId(Guid.NewGuid()),
                        OrganizationId = velocityBackfill.OrganizationId,
                        PartyId = link.PartyId,
                        Role = link.Role,
                        Metric = value.Name,
                        TransactionId = transaction.TransactionId,
                        ObservedAt = new ObservedAt(transaction.CreatedAt),
                        Text = value.Text,
                        Number = value.Number,
                        CreatedAt = new CreatedAt(now),
                        UpdatedAt = new UpdatedAt(now)
                    }));
                }).ToList();

                await velocityObservationRepository.AppendAsync(observations, ct);

                processedCount = new ProcessedCount(processedCount + chunk.Count);

                await velocityBackfillRepository.UpdateAsync(
                    velocityBackfillId, VelocityBackfillStatus.Running, totalTransactions, processedCount, null, ct);

                await unitOfWork.SaveChangesAsync(ct);
            }

            await CompleteAsync(velocityBackfill, totalTransactions, processedCount, ct);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error processing velocity backfill {VelocityBackfillId}: {Error}", velocityBackfillId, ex.Message);
            activity?.AddException(ex);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

            await velocityBackfillRepository.UpdateAsync(
                velocityBackfillId, VelocityBackfillStatus.Failed, totalTransactions, processedCount, null, CancellationToken.None);

            await unitOfWork.SaveChangesAsync(CancellationToken.None);
        }
    }

    private static List<VelocityMetric> Metrics(VelocityProfile? velocityProfile, VelocityBackfill velocityBackfill)
    {
        var wanted = velocityBackfill.Metrics.Select(x => (string)x).ToHashSet(StringComparer.OrdinalIgnoreCase);

        return (velocityProfile?.Metrics ?? []).Where(x => wanted.Contains(x.Name)).ToList();
    }

    private async Task CompleteAsync(VelocityBackfill velocityBackfill, TotalCount totalTransactions, ProcessedCount processedCount, CancellationToken stoppingToken)
    {
        await velocityBackfillRepository.UpdateAsync(
            velocityBackfill.VelocityBackfillId,
            VelocityBackfillStatus.Completed,
            totalTransactions,
            processedCount,
            new CompletedAt(timeProvider.GetUtcNow()),
            stoppingToken);

        await unitOfWork.SaveChangesAsync(stoppingToken);
    }
}
