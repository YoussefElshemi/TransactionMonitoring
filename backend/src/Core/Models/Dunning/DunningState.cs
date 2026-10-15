using Core.Enums;
using Core.Models.BaseModels;
using Core.Models.Invoices;
using Core.ValueObjects;

namespace Core.Models.Dunning;

public record DunningState : BaseModel
{
    public required DunningStateId DunningStateId { get; init; }
    public required InvoiceId InvoiceId { get; init; }
    public required Invoice? Invoice { get; init; }
    public required OrganizationId OrganizationId { get; init; }

    public required DunningStage Stage { get; init; }

    public required NextAttemptAt NextAttemptAt { get; init; }
    public required NoticedAt? LastNoticedAt { get; init; }

    public required Attempts CollectionAttempts { get; init; }
    public required LastAttemptedAt? LastAttemptedAt { get; init; }

    public required ProposedAt? RestrictionProposedAt { get; init; }

    public required ResolvedAt? ResolvedAt { get; init; }
}
