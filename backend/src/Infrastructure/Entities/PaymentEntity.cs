using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record PaymentEntity : DeletableEntity
{
    public required Guid PaymentId { get; init; }
    public required Guid OrganizationId { get; init; }
    public OrganizationEntity Organization { get; init; } = null!;
    public required decimal Amount { get; init; }
    public required int CurrencyId { get; init; }
    public CurrencyEntity Currency { get; init; } = null!;

    public required int PaymentStatusId { get; set; }
    public PaymentStatusEntity PaymentStatus { get; init; } = null!;
    public required int PaymentMethodKindId { get; set; }
    public PaymentMethodKindEntity PaymentMethodKind { get; init; } = null!;

    public required string? ProviderPaymentReference { get; set; }
    public required string? PaymentMethodReference { get; set; }
    public required string? PaymentMethodLabel { get; set; }

    public required string? RemittanceReference { get; init; }
    public required string? ReceiptNumber { get; set; }
    public required Guid? RaisedForInvoiceId { get; init; }
    public InvoiceEntity? RaisedForInvoice { get; init; }

    public required DateTimeOffset ReceivedAt { get; init; }
    public required DateTimeOffset? SettledAt { get; set; }
    public required DateTimeOffset? ReturnedAt { get; set; }

    public required string? FailureCode { get; set; }
    public required string? FailureReason { get; set; }

    public required Guid? RecordedByUserId { get; init; }
    public ApplicationUserEntity? RecordedByUser { get; init; }
}
