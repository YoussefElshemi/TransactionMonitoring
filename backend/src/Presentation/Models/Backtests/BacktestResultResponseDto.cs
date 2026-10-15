using System.Text.Json.Nodes;

namespace Presentation.Models.BackTests;

public record BackTestResultResponseDto
{
    public required Guid BackTestResultId { get; init; }
    public required Guid RuleEvaluationId { get; init; }
    public required JsonObject Payload { get; init; }
    public required bool PayloadMasked { get; init; }
    public required bool OriginalMatched { get; init; }
    public required bool NewMatched { get; init; }
    public required bool HasChanged { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}
