using System.Text.Json.Nodes;
using Core.ValueObjects;

namespace Core.Models.RuleSets;

public record EvaluateRuleSetsRequest
{
    public required OrganizationId OrganizationId { get; init; }
    public required JsonObject Payload { get; init; }
    public required ExternalReference? ExternalReference { get; init; }
    public required CreateCase CreateCase { get; init; }
}