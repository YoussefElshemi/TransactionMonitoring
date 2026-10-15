using System.Text.Json.Serialization;
using Presentation.Models.ApiClients;
using Presentation.Models.Users;

namespace Presentation.Models.AuditLogs;

public record AuditLogResourceDto
{
    public required AuditLogResponseDto AuditLog { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public UserResponseDto? ChangedByUser { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ApiClientResponseDto? ChangedByApiClient { get; init; }
}
