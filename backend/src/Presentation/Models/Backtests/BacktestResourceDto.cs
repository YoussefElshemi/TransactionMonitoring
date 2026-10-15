using System.Text.Json.Serialization;
using Presentation.Models.Rules;
using Presentation.Models.Users;

namespace Presentation.Models.BackTests;

public record BackTestResourceDto
{
    public required BackTestResponseDto BackTest { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public RuleResponseDto? Rule { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public RuleVersionResponseDto? RuleVersion { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public UserResponseDto? CreatedByUser { get; init; }
}
