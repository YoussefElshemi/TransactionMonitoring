using System.Text.Json.Nodes;
using Core.ValueObjects;

namespace Core.Models.Transactions;

public record CreateTransactionRequest
{
    public required OrganizationId OrganizationId { get; init; }
    public required ExternalReference? ExternalReference { get; init; }
    public required JsonObject Payload { get; init; }
}
