using System.Text.Json.Serialization;
using Presentation.Models.Organizations;
using Presentation.Models.Rules;
using Presentation.Models.Users;

namespace Presentation.Models.Transactions;

public record TransactionResourceDto
{
    public required TransactionResponseDto Transaction { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OrganizationResponseDto? Organization { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public RuleResponseDto[]? Rules { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public UserResponseDto[]? Users { get; init; }
}
