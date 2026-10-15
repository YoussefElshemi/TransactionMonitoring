using System.Text.Json.Nodes;
using Core.ValueObjects;

namespace Core.Models.Parties;

public record RecordTransactionPartiesRequest
{
    public required OrganizationId OrganizationId { get; init; }
    public required TransactionId TransactionId { get; init; }
    public required JsonObject Payload { get; init; }
}
