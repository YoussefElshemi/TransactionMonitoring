using Core.Interfaces.Repositories;
using Core.Models.VelocityObservations;
using Core.ValueObjects;
using Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class VelocityObservationRepository(
    TransactionMonitoringDbContext context) : IVelocityObservationRepository
{
    public Task AppendAsync(List<VelocityObservation> observations, CancellationToken stoppingToken)
    {
        if (observations.Count == 0)
        {
            return Task.CompletedTask;
        }

        var transactionIds = observations.Select(x => (Guid)x.TransactionId).Distinct().ToList();

        return IdempotentAppender.AppendAsync(
            context,
            observations.ConvertAll(x => x.ToEntity()),
            async ct => (await context.VelocityObservations
                    .Where(x => transactionIds.Contains(x.TransactionId))
                    .Select(x => new { x.TransactionId, x.PartyId, x.PartyRoleId, x.Metric })
                    .ToListAsync(ct))
                .ConvertAll(x => (x.TransactionId, x.PartyId, x.PartyRoleId, x.Metric)),
            x => (x.TransactionId, x.PartyId, x.PartyRoleId, x.Metric),
            stoppingToken);
    }

    public async Task<List<VelocityAggregateRow>> AggregateAsync(
        OrganizationId organizationId,
        IReadOnlyList<PartyId> partyIds,
        VelocityWindowQuery query,
        CancellationToken stoppingToken)
    {
        if (partyIds.Count == 0)
        {
            return [];
        }

        var partyIdValues = partyIds.Select(x => (Guid)x).Distinct().ToList();
        var roleId = query.Role;
        var metric = query.Metric;
        var from = query.From;
        var to = query.To;
        var exclude = query.ExcludeTransactionId;
        var floor = query.BandFloor;
        var ceiling = query.BandCeiling;

        var rows = await context.VelocityObservations
            .Where(x => x.OrganizationId == organizationId &&
                        partyIdValues.Contains(x.PartyId) &&
                        x.PartyRoleId == (int)roleId &&
                        x.Metric == metric &&
                        x.ObservedAt >= from &&
                        x.ObservedAt <= to &&
                        (exclude == null || x.TransactionId != exclude))
            .GroupBy(x => x.PartyId)
            .Select(x => new
            {
                PartyId = x.Key,
                Count = x.Count(),
                NumberCount = x.Count(y => y.Number != null),
                DistinctCount = x.Select(y => y.Text).Distinct().Count(),
                BandCount = x.Count(y => floor != null && ceiling != null && y.Number >= floor && y.Number < ceiling),
                Sum = x.Sum(y => y.Number),
                Min = x.Min(y => y.Number),
                Max = x.Max(y => y.Number)
            })
            .ToListAsync(stoppingToken);

        return rows.ConvertAll(x => new VelocityAggregateRow
        {
            PartyId = new PartyId(x.PartyId),
            Aggregate = new VelocityAggregate
            {
                Count = new Count(x.Count),
                NumberCount = new Count(x.NumberCount),
                DistinctCount = new Count(x.DistinctCount),
                BandCount = new Count(x.BandCount),
                Sum = x.Sum.HasValue ? new MetricNumber(x.Sum.Value) : null,
                Min = x.Min.HasValue ? new MetricNumber(x.Min.Value) : null,
                Max = x.Max.HasValue ? new MetricNumber(x.Max.Value) : null
            }
        });
    }

    public async Task<List<VelocityObservation>> ListForWindowAsync(
        OrganizationId organizationId,
        IReadOnlyCollection<PartyId> partyIds,
        IReadOnlyCollection<VelocityMetricName> metrics,
        ObservedAt from,
        ObservedAt to,
        CancellationToken stoppingToken)
    {
        var partyIdValues = partyIds.Select(x => (Guid)x).ToList();
        var metricValues = metrics.Select(x => (string)x).ToList();
        var fromValue = (DateTimeOffset)from;
        var toValue = (DateTimeOffset)to;

        var observationEntities = await context.VelocityObservations
            .Where(x => x.OrganizationId == organizationId &&
                        partyIdValues.Contains(x.PartyId) &&
                        metricValues.Contains(x.Metric) &&
                        x.ObservedAt >= fromValue &&
                        x.ObservedAt <= toValue)
            .ToListAsync(stoppingToken);

        return observationEntities.ConvertAll(x => x.ToModel());
    }
}
