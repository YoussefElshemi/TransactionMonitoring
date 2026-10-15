using Core.Models.VelocityObservations;
using Core.ValueObjects;

namespace Core.Interfaces.Repositories;

public interface IVelocityObservationRepository
{
    Task AppendAsync(List<VelocityObservation> observations, CancellationToken stoppingToken);

    Task<List<VelocityAggregateRow>> AggregateAsync(
        OrganizationId organizationId,
        IReadOnlyList<PartyId> partyIds,
        VelocityWindowQuery query,
        CancellationToken stoppingToken);

    Task<List<VelocityObservation>> ListForWindowAsync(
        OrganizationId organizationId,
        IReadOnlyCollection<PartyId> partyIds,
        IReadOnlyCollection<VelocityMetricName> metrics,
        ObservedAt from,
        ObservedAt to,
        CancellationToken stoppingToken);
}
