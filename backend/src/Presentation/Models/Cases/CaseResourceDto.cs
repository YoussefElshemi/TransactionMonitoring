using System.Text.Json.Serialization;
using Presentation.Models.Organizations;
using Presentation.Models.Rules;
using Presentation.Models.Transactions;
using Presentation.Models.Users;

namespace Presentation.Models.Cases;

public record CaseResourceDto
{
    public required CaseResponseDto Case { get; init; }

    public required CaseAgeingResponseDto Ageing { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OrganizationResponseDto? Organization { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public UserResponseDto? AssignedToUser { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public TransactionSummaryResponseDto? Transaction { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public RuleResponseDto[]? Rules { get; init; }
}
