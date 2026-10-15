using Core.Enums;
using Core.Models.BaseModels;
using Core.ValueObjects;

namespace Core.Models.ServiceCredits;

public record ServiceCredit : BaseModel
{
    public required ServiceCreditId ServiceCreditId { get; init; }
    public required OrganizationId OrganizationId { get; init; }
    public required AvailabilityCommitmentId AvailabilityCommitmentId { get; init; }
    public required Month Month { get; init; }
    public required Year Year { get; init; }

    public required AvailabilityTarget AvailabilityTarget { get; init; }

    public required UptimePercentage MeasuredUptimePercentage { get; init; }
    public required RequestCount TotalRequests { get; init; }
    public required RequestCount FailedRequests { get; init; }

    public required CreditPercentage CreditPercentage { get; init; }

    public required ServiceCreditAmount Amount { get; init; }
    public required Currency Currency { get; init; }

    public required InvoiceId? InvoiceId { get; init; }

    public required CreditNoteId? CreditNoteId { get; init; }
}
