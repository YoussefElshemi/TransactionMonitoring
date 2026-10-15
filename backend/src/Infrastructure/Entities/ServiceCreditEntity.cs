using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record ServiceCreditEntity : BaseEntity
{
    public required Guid ServiceCreditId { get; init; }
    public required Guid OrganizationId { get; init; }
    public OrganizationEntity Organization { get; init; } = null!;
    public required Guid AvailabilityCommitmentId { get; init; }
    public AvailabilityCommitmentEntity AvailabilityCommitment { get; init; } = null!;
    public required int Month { get; init; }
    public required int Year { get; init; }
    public required decimal AvailabilityTarget { get; init; }
    public required decimal MeasuredUptimePercentage { get; init; }
    public required int TotalRequests { get; init; }
    public required int FailedRequests { get; init; }
    public required decimal CreditPercentage { get; init; }
    public required decimal Amount { get; init; }
    public required int CurrencyId { get; init; }
    public CurrencyEntity Currency { get; init; } = null!;
    public required Guid? InvoiceId { get; init; }
    public InvoiceEntity? Invoice { get; init; }
    public required Guid? CreditNoteId { get; set; }
    public CreditNoteEntity? CreditNote { get; init; }
}
