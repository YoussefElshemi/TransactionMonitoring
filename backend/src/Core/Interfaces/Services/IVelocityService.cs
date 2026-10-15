using Core.Models.Rules;
using Core.Models.VelocityObservations;
using Core.ValueObjects;

namespace Core.Interfaces.Services;

public interface IVelocityService
{
    Task<VelocityFacts> RecordAndReadAsync(
        RecordVelocityObservationsRequest recordVelocityObservationsRequest,
        IReadOnlyList<RuleBodyModel> ruleBodies,
        EvaluatedAt evaluatedAt,
        CancellationToken stoppingToken);

    Task<List<VelocityFacts>> PeekManyAsync(
        OrganizationId organizationId,
        IReadOnlyList<VelocityPeekSample> samples,
        IReadOnlyList<RuleBodyModel> ruleBodies,
        CancellationToken stoppingToken);
}
