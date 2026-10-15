using Core.Enums;
using Core.Models.Dunning;
using Core.ValueObjects;
using Infrastructure.Entities;

namespace Infrastructure.Mappers;

public static class DunningStateMapper
{
    public static DunningState ToModel(this DunningStateEntity dunningStateEntity)
    {
        return new DunningState
        {
            DunningStateId = new DunningStateId(dunningStateEntity.DunningStateId),
            InvoiceId = new InvoiceId(dunningStateEntity.InvoiceId),

            Invoice = dunningStateEntity.Invoice?.ToModel(),
            OrganizationId = new OrganizationId(dunningStateEntity.OrganizationId),
            Stage = (DunningStage)dunningStateEntity.DunningStageId,
            NextAttemptAt = new NextAttemptAt(dunningStateEntity.NextAttemptAt),
            LastNoticedAt = dunningStateEntity.LastNoticedAt.HasValue
                ? new NoticedAt(dunningStateEntity.LastNoticedAt.Value)
                : null,
            CollectionAttempts = new Attempts(dunningStateEntity.CollectionAttempts),
            LastAttemptedAt = dunningStateEntity.LastAttemptedAt.HasValue
                ? new LastAttemptedAt(dunningStateEntity.LastAttemptedAt.Value)
                : null,
            RestrictionProposedAt = dunningStateEntity.RestrictionProposedAt.HasValue
                ? new ProposedAt(dunningStateEntity.RestrictionProposedAt.Value)
                : null,
            ResolvedAt = dunningStateEntity.ResolvedAt.HasValue
                ? new ResolvedAt(dunningStateEntity.ResolvedAt.Value)
                : null,
            CreatedAt = new CreatedAt(dunningStateEntity.CreatedAt),
            UpdatedAt = new UpdatedAt(dunningStateEntity.UpdatedAt)
        };
    }

    public static DunningStateEntity ToEntity(this DunningState dunningState)
    {
        return new DunningStateEntity
        {
            DunningStateId = dunningState.DunningStateId,
            InvoiceId = dunningState.InvoiceId,
            OrganizationId = dunningState.OrganizationId,
            DunningStageId = (int)dunningState.Stage,
            NextAttemptAt = dunningState.NextAttemptAt,
            LastNoticedAt = dunningState.LastNoticedAt.HasValue ? dunningState.LastNoticedAt.Value : null,
            CollectionAttempts = dunningState.CollectionAttempts,
            LastAttemptedAt = dunningState.LastAttemptedAt.HasValue ? dunningState.LastAttemptedAt.Value : null,
            RestrictionProposedAt = dunningState.RestrictionProposedAt.HasValue
                ? dunningState.RestrictionProposedAt.Value
                : null,
            ResolvedAt = dunningState.ResolvedAt.HasValue ? dunningState.ResolvedAt.Value : null,
            CreatedAt = dunningState.CreatedAt,
            UpdatedAt = dunningState.UpdatedAt
        };
    }
}
