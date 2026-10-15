using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using Presentation.Constants;

namespace Presentation.Models.RuleSets;

public record EvaluateAllRuleSetRequestDto
{
    [FromBody]
    public required JsonObject Payload { get; init; }

    [FromHeader(Name = HeaderNames.TransactionReference)]
    public string? TransactionReference { get; init; }

    [FromHeader(Name = HeaderNames.CreateCase)]
    public bool CreateCase { get; init; } = false;
}