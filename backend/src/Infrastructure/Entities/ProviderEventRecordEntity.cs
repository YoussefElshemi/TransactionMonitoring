using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record ProviderEventRecordEntity : ProcessableEntity
{
    public required Guid ProviderEventRecordId { get; init; }

    public required string ProviderEventReference { get; init; }
    public required string ProviderEventType { get; init; }

    public required string Payload { get; init; }

    public required Guid? PaymentId { get; set; }
    public PaymentEntity? Payment { get; init; }
    public required string? FailureReason { get; set; }
}
