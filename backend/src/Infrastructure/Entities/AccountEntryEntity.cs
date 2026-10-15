using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record AccountEntryEntity : BaseEntity
{
    public required Guid AccountEntryId { get; init; }
    public required Guid OrganizationId { get; init; }
    public OrganizationEntity Organization { get; init; } = null!;
    public required int CurrencyId { get; init; }
    public CurrencyEntity Currency { get; init; } = null!;

    public required decimal Amount { get; init; }

    public required int AccountEntryReasonId { get; init; }
    public AccountEntryReasonEntity AccountEntryReason { get; init; } = null!;

    public required Guid? PaymentId { get; init; }
    public PaymentEntity? Payment { get; init; }
    public required Guid? InvoiceId { get; init; }
    public InvoiceEntity? Invoice { get; init; }
    public required Guid? CreditNoteId { get; init; }
    public CreditNoteEntity? CreditNote { get; init; }

    public required string Description { get; init; }
    public required Guid? CreatedByUserId { get; init; }
    public ApplicationUserEntity? CreatedByUser { get; init; }
}
