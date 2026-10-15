using System.Text.Json.Serialization;
using Presentation.Models.Organizations;
using Presentation.Models.Users;

namespace Presentation.Models.SuspiciousActivityReports;

public record SuspiciousActivityReportResourceDto
{
    public required SuspiciousActivityReportResponseDto SuspiciousActivityReport { get; init; }

    public required bool RequireFilingApproval { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OrganizationResponseDto? Organization { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public UserResponseDto? OpenedByUser { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public UserResponseDto? FiledByUser { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public UserResponseDto? DecidedByUser { get; init; }
}
