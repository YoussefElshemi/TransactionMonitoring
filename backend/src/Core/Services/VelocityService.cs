using Core.Helpers;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Mappers;
using Core.Models.Rules;
using Core.Models.VelocityObservations;
using Core.Models.VelocityProfiles;
using Core.ValueObjects;

namespace Core.Services;

public class VelocityService(
    IVelocityProfileRepository velocityProfileRepository,
    IVelocityObservationRepository velocityObservationRepository,
    TimeProvider timeProvider,
    IModeContext modeContext) : IVelocityService
{
    public async Task<VelocityFacts> RecordAndReadAsync(
        RecordVelocityObservationsRequest recordVelocityObservationsRequest,
        IReadOnlyList<RuleBodyModel> ruleBodies,
        EvaluatedAt evaluatedAt,
        CancellationToken stoppingToken)
    {
        var velocityProfile = await velocityProfileRepository.FindAsync(
            recordVelocityObservationsRequest.OrganizationId, new IncludeDeleted(false), stoppingToken);

        if (velocityProfile == null)
        {
            return VelocityFacts.Empty;
        }

        await RecordAsync(velocityProfile, recordVelocityObservationsRequest, stoppingToken);

        var thresholds = Thresholds(velocityProfile);
        var conditions = Declared(Conditions(ruleBodies), velocityProfile);
        var queries = Distinct(conditions, thresholds, evaluatedAt, recordVelocityObservationsRequest.TransactionId);

        if (queries.Count == 0)
        {
            return VelocityFacts.Empty;
        }

        var windows = new List<VelocityWindowFact>();

        foreach (var query in queries)
        {
            var partyIds = recordVelocityObservationsRequest.Parties
                .Where(x => x.Role == query.Role)
                .Select(x => x.PartyId)
                .Distinct()
                .ToList();

            var rows = await velocityObservationRepository.AggregateAsync(
                recordVelocityObservationsRequest.OrganizationId, partyIds, query, stoppingToken);

            windows.AddRange(rows.ConvertAll(x => new VelocityWindowFact
            {
                Query = query,
                PartyId = x.PartyId,
                Aggregate = x.Aggregate
            }));
        }

        return new VelocityFacts
        {
            Windows = windows,
            Thresholds = thresholds,
            CurrentTransactionId = recordVelocityObservationsRequest.TransactionId
        };
    }

    public async Task<List<VelocityFacts>> PeekManyAsync(
        OrganizationId organizationId,
        IReadOnlyList<VelocityPeekSample> samples,
        IReadOnlyList<RuleBodyModel> ruleBodies,
        CancellationToken stoppingToken)
    {
        var conditions = Conditions(ruleBodies);
        var velocityProfile = conditions.Count == 0
            ? null
            : await velocityProfileRepository.FindAsync(organizationId, new IncludeDeleted(false), stoppingToken);

        if (velocityProfile == null || samples.Count == 0)
        {
            return samples.Select(_ => VelocityFacts.Empty).ToList();
        }

        var thresholds = Thresholds(velocityProfile);
        var declared = Declared(conditions, velocityProfile);

        var observations = await ReadAsync(organizationId, velocityProfile, samples, declared, stoppingToken);

        return samples.Select(sample => Facts(sample, velocityProfile, thresholds, declared, observations)).ToList();
    }

    private static VelocityFacts Facts(
        VelocityPeekSample sample,
        VelocityProfile velocityProfile,
        Dictionary<string, decimal> thresholds,
        List<VelocityConditionModel> conditions,
        List<VelocityObservation> observations)
    {
        var facts = Observations(sample, velocityProfile, observations);
        var windows = new List<VelocityWindowFact>();

        foreach (var query in Distinct(conditions, thresholds, sample.EvaluatedAt, sample.TransactionId))
        {
            foreach (var party in sample.Parties.Where(x => x.Role == query.Role))
            {
                windows.Add(new VelocityWindowFact
                {
                    Query = query,
                    PartyId = party.PartyId,
                    Aggregate = VelocityAggregator.Reduce(facts.Where(x => x.PartyId == party.PartyId).ToList(), query)
                });
            }
        }

        return new VelocityFacts
        {
            Windows = windows,
            Thresholds = thresholds,
            CurrentTransactionId = sample.TransactionId
        };
    }

    private static List<VelocityObservationFact> Observations(
        VelocityPeekSample sample,
        VelocityProfile velocityProfile,
        List<VelocityObservation> observations)
    {
        var partyIds = sample.Parties.Select(x => (Guid)x.PartyId).ToHashSet();

        var facts = observations
            .Where(x => partyIds.Contains(x.PartyId))
            .Select(x => x.ToFact(new IsCurrent(sample.TransactionId.HasValue && x.TransactionId == sample.TransactionId.Value)))
            .ToList();

        if (sample.TransactionId.HasValue)
        {
            return facts;
        }

        facts.AddRange(Synthesise(sample, velocityProfile));

        return facts;
    }

    private static IEnumerable<VelocityObservationFact> Synthesise(VelocityPeekSample sample, VelocityProfile velocityProfile)
    {
        var values = VelocityMetricReader.Read(sample.Payload, velocityProfile.Metrics);

        return sample.Parties.SelectMany(party => values.Select(value => new VelocityObservationFact
        {
            PartyId = party.PartyId,
            Role = party.Role,
            Metric = value.Name,
            ObservedAt = new ObservedAt(sample.EvaluatedAt),
            Text = value.Text,
            Number = value.Number,
            IsCurrent = new IsCurrent(true)
        }));
    }

    private static List<VelocityConditionModel> Conditions(IReadOnlyList<RuleBodyModel> ruleBodies)
    {
        return ruleBodies.SelectMany(VelocityHelper.CollectConditions).ToList();
    }

    private static Dictionary<string, decimal> Thresholds(VelocityProfile velocityProfile)
    {
        return velocityProfile.Thresholds.ToDictionary(x => (string)x.Name, x => (decimal)x.Value, StringComparer.OrdinalIgnoreCase);
    }

    private static List<VelocityWindowQuery> Distinct(
        List<VelocityConditionModel> conditions,
        Dictionary<string, decimal> thresholds,
        EvaluatedAt evaluatedAt,
        TransactionId? currentTransactionId)
    {
        return conditions
            .Select(x => VelocityWindowResolver.Query(x, thresholds, evaluatedAt, currentTransactionId))
            .Distinct()
            .ToList();
    }

    // Map back to the declared spelling: rules match metrics case-insensitively but the database filter is an equality.
    private static List<VelocityConditionModel> Declared(List<VelocityConditionModel> conditions, VelocityProfile velocityProfile)
    {
        var declared = new List<VelocityConditionModel>();

        foreach (var condition in conditions)
        {
            var metric = velocityProfile.Metrics.FirstOrDefault(x => string.Equals(x.Name, condition.Metric, StringComparison.OrdinalIgnoreCase));

            if (metric != null)
            {
                declared.Add(condition with { Metric = metric.Name });
            }
        }

        return declared;
    }

    private async Task<List<VelocityObservation>> ReadAsync(
        OrganizationId organizationId,
        VelocityProfile velocityProfile,
        IReadOnlyList<VelocityPeekSample> samples,
        List<VelocityConditionModel> conditions,
        CancellationToken stoppingToken)
    {
        var partyIds = samples.SelectMany(x => x.Parties).Select(x => x.PartyId).Distinct().ToList();

        if (partyIds.Count == 0 || conditions.Count == 0)
        {
            return [];
        }

        var metrics = velocityProfile.Metrics
            .Where(x => conditions.Any(condition => string.Equals(condition.Metric, x.Name, StringComparison.OrdinalIgnoreCase)))
            .Select(x => x.Name)
            .Distinct()
            .ToList();

        if (metrics.Count == 0)
        {
            return [];
        }

        var earliest = samples.Min(x => (DateTimeOffset)x.EvaluatedAt);
        var latest = samples.Max(x => (DateTimeOffset)x.EvaluatedAt);

        var from = VelocityWindowResolver.Widest(conditions.Select(x => x.Window ?? new VelocityWindowModel()), earliest);

        return await velocityObservationRepository.ListForWindowAsync(
            organizationId, partyIds, metrics, new ObservedAt(from), new ObservedAt(latest), stoppingToken);
    }

    private async Task RecordAsync(
        VelocityProfile velocityProfile,
        RecordVelocityObservationsRequest recordVelocityObservationsRequest,
        CancellationToken stoppingToken)
    {
        var values = VelocityMetricReader.Read(recordVelocityObservationsRequest.Payload, velocityProfile.Metrics);

        if (values.Count == 0 || recordVelocityObservationsRequest.Parties.Count == 0)
        {
            return;
        }

        var now = timeProvider.GetUtcNow();

        var observations = recordVelocityObservationsRequest.Parties
            .SelectMany(party => values.Select(value => new VelocityObservation
            {
                Mode = modeContext.CurrentOrLive,
                VelocityObservationId = new VelocityObservationId(Guid.NewGuid()),
                OrganizationId = recordVelocityObservationsRequest.OrganizationId,
                PartyId = party.PartyId,
                Role = party.Role,
                Metric = value.Name,
                TransactionId = recordVelocityObservationsRequest.TransactionId,
                ObservedAt = recordVelocityObservationsRequest.ObservedAt,
                Text = value.Text,
                Number = value.Number,
                CreatedAt = new CreatedAt(now),
                UpdatedAt = new UpdatedAt(now)
            }))
            .ToList();

        await velocityObservationRepository.AppendAsync(observations, stoppingToken);
    }
}
