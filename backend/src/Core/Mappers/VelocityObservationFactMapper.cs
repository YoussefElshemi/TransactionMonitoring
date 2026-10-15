using Core.Models.VelocityObservations;
using Core.ValueObjects;

namespace Core.Mappers;

public static class VelocityObservationFactMapper
{
    public static VelocityObservationFact ToFact(this VelocityObservation observation, IsCurrent isCurrent)
    {
        return new VelocityObservationFact
        {
            PartyId = observation.PartyId,
            Role = observation.Role,
            Metric = observation.Metric,
            ObservedAt = observation.ObservedAt,
            Text = observation.Text,
            Number = observation.Number,
            IsCurrent = isCurrent
        };
    }
}
