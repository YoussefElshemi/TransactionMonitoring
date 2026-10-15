using Core.Enums;
using Core.Models.VelocityObservations;
using Core.ValueObjects;
using Infrastructure.Entities;

namespace Infrastructure.Mappers;

public static class VelocityObservationMapper
{
    public static VelocityObservation ToModel(this VelocityObservationEntity velocityObservationEntity)
    {
        return new VelocityObservation
        {
            Mode = (Mode)velocityObservationEntity.ModeId,
            VelocityObservationId = new VelocityObservationId(velocityObservationEntity.VelocityObservationId),
            OrganizationId = new OrganizationId(velocityObservationEntity.OrganizationId),
            PartyId = new PartyId(velocityObservationEntity.PartyId),
            Role = (PartyRole)velocityObservationEntity.PartyRoleId,
            Metric = new VelocityMetricName(velocityObservationEntity.Metric),
            TransactionId = new TransactionId(velocityObservationEntity.TransactionId),
            ObservedAt = new ObservedAt(velocityObservationEntity.ObservedAt),
            Text = new MetricText(velocityObservationEntity.Text),
            Number = velocityObservationEntity.Number.HasValue ? new MetricNumber(velocityObservationEntity.Number.Value) : null,
            CreatedAt = new CreatedAt(velocityObservationEntity.CreatedAt),
            UpdatedAt = new UpdatedAt(velocityObservationEntity.UpdatedAt)
        };
    }

    public static VelocityObservationEntity ToEntity(this VelocityObservation velocityObservation)
    {
        return new VelocityObservationEntity
        {
            ModeId = (int)velocityObservation.Mode,
            VelocityObservationId = velocityObservation.VelocityObservationId,
            OrganizationId = velocityObservation.OrganizationId,
            PartyId = velocityObservation.PartyId,
            PartyRoleId = (int)velocityObservation.Role,
            Metric = velocityObservation.Metric,
            TransactionId = velocityObservation.TransactionId,
            ObservedAt = velocityObservation.ObservedAt,
            Text = velocityObservation.Text,
            Number = velocityObservation.Number,
            CreatedAt = velocityObservation.CreatedAt,
            UpdatedAt = velocityObservation.UpdatedAt
        };
    }
}
