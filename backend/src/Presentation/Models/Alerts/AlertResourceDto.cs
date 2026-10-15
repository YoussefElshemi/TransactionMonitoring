using System.Text.Json.Serialization;
using Presentation.Models.Organizations;
using Presentation.Models.Rules;
using Presentation.Models.Transactions;

namespace Presentation.Models.Alerts;

public record AlertResourceDto
{
    public required AlertResponseDto Alert { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OrganizationResponseDto? Organization { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public RuleResponseDto? Rule { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public TransactionSummaryResponseDto? Transaction { get; init; }
}
