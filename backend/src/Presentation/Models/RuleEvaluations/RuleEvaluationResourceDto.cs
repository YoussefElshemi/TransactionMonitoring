using System.Text.Json.Serialization;
using Presentation.Models.Organizations;
using Presentation.Models.Rules;
using Presentation.Models.Transactions;

namespace Presentation.Models.RuleEvaluations;

public record RuleEvaluationResourceDto
{
    public required RuleEvaluationResponseDto RuleEvaluation { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public RuleResponseDto? Rule { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OrganizationResponseDto? Organization { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public TransactionSummaryResponseDto? Transaction { get; init; }
}
