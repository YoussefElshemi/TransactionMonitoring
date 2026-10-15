using Core.Enums;
using Core.Models.VelocityProfiles;
using Core.ValueObjects;
using Infrastructure.Entities;

namespace Infrastructure.Mappers;

public static class VelocityBackfillMapper
{
    public static VelocityBackfill ToModel(this VelocityBackfillEntity velocityBackfillEntity)
    {
        return new VelocityBackfill
        {
            Mode = (Mode)velocityBackfillEntity.ModeId,
            VelocityBackfillId = new VelocityBackfillId(velocityBackfillEntity.VelocityBackfillId),
            OrganizationId = new OrganizationId(velocityBackfillEntity.OrganizationId),
            Metrics = velocityBackfillEntity.Metrics.MetricNamesToModel(),
            Status = (VelocityBackfillStatus)velocityBackfillEntity.StatusId,
            TotalTransactions = new TotalCount(velocityBackfillEntity.TotalTransactions),
            ProcessedCount = new ProcessedCount(velocityBackfillEntity.ProcessedCount),
            CompletedAt = velocityBackfillEntity.CompletedAt.HasValue ? new CompletedAt(velocityBackfillEntity.CompletedAt.Value) : null,
            CreatedAt = new CreatedAt(velocityBackfillEntity.CreatedAt),
            UpdatedAt = new UpdatedAt(velocityBackfillEntity.UpdatedAt)
        };
    }

    public static VelocityBackfillEntity ToEntity(this VelocityBackfill velocityBackfill)
    {
        return new VelocityBackfillEntity
        {
            ModeId = (int)velocityBackfill.Mode,
            VelocityBackfillId = velocityBackfill.VelocityBackfillId,
            OrganizationId = velocityBackfill.OrganizationId,
            Metrics = velocityBackfill.Metrics.ToBase64String(),
            StatusId = (int)velocityBackfill.Status,
            TotalTransactions = velocityBackfill.TotalTransactions,
            ProcessedCount = velocityBackfill.ProcessedCount,
            CompletedAt = velocityBackfill.CompletedAt,
            CreatedAt = velocityBackfill.CreatedAt,
            UpdatedAt = velocityBackfill.UpdatedAt
        };
    }
}
