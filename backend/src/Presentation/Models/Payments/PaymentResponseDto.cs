namespace Presentation.Models.Payments;

public record PaymentResponseDto
{
    public required Guid PaymentId { get; init; }
    public required Guid OrganizationId { get; init; }
    public required decimal Amount { get; init; }
    public required string Currency { get; init; }
    public required string PaymentStatus { get; init; }
    public required string PaymentMethodKind { get; init; }
    public required string? PaymentMethodLabel { get; init; }
    public required string? RemittanceReference { get; init; }

    public required string? ReceiptNumber { get; init; }

    public required Guid? RaisedForInvoiceId { get; init; }

    public required DateTimeOffset ReceivedAt { get; init; }
    public required DateTimeOffset? SettledAt { get; init; }
    public required DateTimeOffset? ReturnedAt { get; init; }

    public required string? FailureReason { get; init; }

    public required Guid? RecordedByUserId { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
}
