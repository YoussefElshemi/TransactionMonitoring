using Core.Models.BaseModels;
using Core.ValueObjects;

namespace Core.Models.Payments;

public record ProviderEventRecord : ProcessableModel
{
    public required ProviderEventRecordId ProviderEventRecordId { get; init; }

    // Unique, which is what makes provider redelivery harmless.
    public required ProviderEventReference ProviderEventReference { get; init; }
    public required ProviderEventType ProviderEventType { get; init; }

    public required Payload Payload { get; init; }

    public required PaymentId? PaymentId { get; init; }

    public required FailureReason? FailureReason { get; init; }
}
