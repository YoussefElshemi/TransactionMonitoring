namespace Presentation.Models.Adjustments;

public record InvoiceAdjustmentResponseDto
{
    public required Guid InvoiceAdjustmentId { get; init; }
    public required Guid OrganizationId { get; init; }
    public required int Month { get; init; }
    public required int Year { get; init; }
    public required string Description { get; init; }
    public required decimal Amount { get; init; }

    public required Guid? AppliedToInvoiceId { get; init; }

    public required Guid? CreatedByUserId { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
}
