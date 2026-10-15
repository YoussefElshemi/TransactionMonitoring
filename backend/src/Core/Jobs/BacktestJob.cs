using System.Diagnostics;
using Core.Configs;
using Core.Enums;
using Core.Helpers;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Mappers;
using Core.Models.BackTests;
using Core.Models.Usages;
using Core.Models.VelocityObservations;
using Core.ValueObjects;
using Microsoft.Extensions.Options;
using Quartz;
using Serilog;

namespace Core.Jobs;

[DisallowConcurrentExecution]
public class BackTestJob(
    IBackTestRepository backTestRepository,
    IBackTestResultRepository backTestResultRepository,
    IRuleVersionRepository ruleVersionRepository,
    IRuleEvaluationRepository ruleEvaluationRepository,
    IUsageRepository usageRepository,
    IPartyService partyService,
    IVelocityService velocityService,
    IUnitOfWork unitOfWork,
    IOptions<AppConfig> appConfig,
    TimeProvider timeProvider,
    IModeContext modeContext,
    ISandboxCreditService sandboxCreditService,
    ILogger logger) : IJob
{
    public async ValueTask Execute(IJobExecutionContext context)
    {
        using var activity = OpenTelemetry.OpenTelemetry.MyActivitySource.StartActivity(GetType().Name);

        var ct = context.CancellationToken;
        var config = appConfig.Value.BackgroundServiceConfig;

        var backTestId = new BackTestId(Guid.Parse(context.JobDetail.Key.Name));
        var backTest = await backTestRepository.GetAsync(backTestId, ct);

        // Jobs have no ambient mode; pin to the backtest's mode or a sandbox backtest would sample live evaluations.
        modeContext.Current = backTest.Mode;

        if (backTest.Status is BackTestStatus.Completed or BackTestStatus.Failed)
        {
            return;
        }

        var totalEvaluations = backTest.TotalEvaluations;
        var processedCount = backTest.ProcessedCount;

        try
        {
            var ruleVersion = await ruleVersionRepository.GetAsync(backTest.RuleVersionId, ct);
            var ruleBody = ruleVersion.RuleBody.ToModel();

            if (backTest.Status == BackTestStatus.Pending)
            {
                var availableEvaluations = await ruleEvaluationRepository.CountByRuleAsync(backTest.OrganizationId, backTest.RuleId, ct);
                totalEvaluations = new TotalCount(availableEvaluations * backTest.SampleSize / 100);
                processedCount = new ProcessedCount(0);

                await backTestRepository.UpdateAsync(new UpdateBackTestRequest
                {
                    BackTestId = backTest.BackTestId,
                    Status = BackTestStatus.Running,
                    TotalEvaluations = totalEvaluations,
                    ProcessedCount = processedCount,
                    CompletedAt = null
                }, ct);

                await unitOfWork.SaveChangesAsync(ct);
            }

            while (true)
            {
                var remaining = totalEvaluations - processedCount;
                if (remaining <= 0)
                {
                    break;
                }

                var take = Math.Min(config.BackTestChunkSize, remaining);
                var chunk = await ruleEvaluationRepository.ListChunkForBackTestAsync(
                    backTest.OrganizationId,
                    backTest.RuleId,
                    backTest.BackTestId,
                    take,
                    ct);

                if (chunk.Count == 0)
                {
                    break;
                }

                // Charged before replaying: the charge writes the ledger row, and charging after would commit results inside the credit transaction.
                await sandboxCreditService.EnforceAsync(
                    backTest.OrganizationId, BillableResource.Backtest, new Resource(backTest.RuleId.ToString()), new Count(chunk.Count), ct);

                var utcNow = timeProvider.GetUtcNow();

                var partiesPerEvaluation = await partyService.PeekManyAsync(
                    backTest.OrganizationId, chunk.ConvertAll(x => x.Payload), ct);

                var velocityPerEvaluation = await velocityService.PeekManyAsync(
                    backTest.OrganizationId,
                    chunk.Select((evaluation, index) => new VelocityPeekSample
                    {
                        Payload = evaluation.Payload,
                        Parties = partiesPerEvaluation[index],
                        EvaluatedAt = new EvaluatedAt(evaluation.CreatedAt),
                        TransactionId = evaluation.TransactionId
                    }).ToList(),
                    [ruleBody],
                    ct);

                var results = chunk.Select((evaluation, index) =>
                {
                    var newMatched = RuleHelper.Matches(
                        ruleBody, evaluation.Payload, ruleVersion.MatchPolarity, evaluation.CreatedAt, partiesPerEvaluation[index], velocityPerEvaluation[index]);

                    return new BackTestResult
                    {
                        BackTestResultId = new BackTestResultId(Guid.NewGuid()),
                        BackTestId = backTest.BackTestId,
                        RuleEvaluationId = evaluation.RuleEvaluationId,
                        Payload = evaluation.Payload,
                        OriginalMatched = evaluation.Matched,
                        NewMatched = newMatched,
                        HasChanged = new HasChanged(newMatched != evaluation.Matched),
                        CreatedAt = new CreatedAt(utcNow),
                        UpdatedAt = new UpdatedAt(utcNow)
                    };
                }).ToList();

                await backTestResultRepository.AddRangeAsync(results, ct);

                var usage = new CreateUsageRequest
                {
                    Resource = new Resource(backTest.RuleId.ToString()),
                    BillableResource = BillableResource.Backtest,
                    OrganizationId = backTest.OrganizationId,
                    Count = new Count(chunk.Count)
                }.ToModel(timeProvider, modeContext.CurrentOrLive);

                if (modeContext.CurrentOrLive != Mode.Test)
                {
                    await usageRepository.AddAsync(usage, ct);
                }

                processedCount = new ProcessedCount(processedCount + chunk.Count);

                await backTestRepository.UpdateAsync(new UpdateBackTestRequest
                {
                    BackTestId = backTest.BackTestId,
                    Status = BackTestStatus.Running,
                    TotalEvaluations = totalEvaluations,
                    ProcessedCount = processedCount,
                    CompletedAt = null
                }, ct);

                await unitOfWork.SaveChangesAsync(ct);
            }

            await backTestRepository.UpdateAsync(new UpdateBackTestRequest
            {
                BackTestId = backTest.BackTestId,
                Status = BackTestStatus.Completed,
                TotalEvaluations = totalEvaluations,
                ProcessedCount = processedCount,
                CompletedAt = new CompletedAt(timeProvider.GetUtcNow())
            }, ct);

            await unitOfWork.SaveChangesAsync(ct);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error processing backTest {BackTestId}: {Error}", backTest.BackTestId, ex.Message);
            activity?.AddException(ex);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

            await backTestRepository.UpdateAsync(new UpdateBackTestRequest
            {
                BackTestId = backTest.BackTestId,
                Status = BackTestStatus.Failed,
                TotalEvaluations = totalEvaluations,
                ProcessedCount = processedCount,
                CompletedAt = null
            }, CancellationToken.None);

            await unitOfWork.SaveChangesAsync(CancellationToken.None);
        }
    }
}
