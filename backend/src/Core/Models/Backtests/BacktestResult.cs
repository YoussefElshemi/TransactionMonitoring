using System.Text.Json.Nodes;
using Core.Models.BaseModels;
using Core.ValueObjects;

namespace Core.Models.BackTests;

public record BackTestResult : BaseModel
{
    public required BackTestResultId BackTestResultId { get; init; }
    public required BackTestId BackTestId { get; init; }
    public required RuleEvaluationId RuleEvaluationId { get; init; }
    public required JsonObject Payload { get; init; }
    public required Matched OriginalMatched { get; init; }
    public required Matched NewMatched { get; init; }
    public required HasChanged HasChanged { get; init; }
}
