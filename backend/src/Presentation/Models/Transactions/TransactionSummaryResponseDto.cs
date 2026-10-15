using System.Text.Json.Nodes;

namespace Presentation.Models.Transactions;

public record TransactionSummaryResponseDto
{
    public required Guid TransactionId { get; init; }
    public required string FriendlyName { get; init; }
    public required string? ExternalReference { get; init; }
    public required JsonObject Payload { get; init; }

    public required bool PayloadMasked { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }

    public required string Mode { get; init; }
}
