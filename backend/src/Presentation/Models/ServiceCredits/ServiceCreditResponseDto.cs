namespace Presentation.Models.ServiceCredits;

public record ServiceCreditResponseDto
{
    public required Guid ServiceCreditId { get; init; }
    public required Guid OrganizationId { get; init; }
    public required Guid AvailabilityCommitmentId { get; init; }
    public required int Month { get; init; }
    public required int Year { get; init; }
    public required decimal AvailabilityTarget { get; init; }
    public required decimal MeasuredUptimePercentage { get; init; }
    public required int TotalRequests { get; init; }
    public required int FailedRequests { get; init; }

    public required decimal CreditPercentage { get; init; }

    public required decimal Amount { get; init; }
    public required string Currency { get; init; }
    public required Guid? InvoiceId { get; init; }
    public required Guid? CreditNoteId { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
}
