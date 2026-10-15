using System.Text.Json.Serialization;
using Presentation.Models.Organizations;
using Presentation.Models.Rules;
using Presentation.Models.SuspiciousActivityReports;
using Presentation.Models.Users;

namespace Presentation.Models.Approvals;

public record ApprovalResourceDto
{
    public required ApprovalResponseDto Approval { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OrganizationResponseDto? Organization { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public RuleDeploymentResponseDto? RuleDeployment { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public RuleResponseDto? Rule { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public SuspiciousActivityReportResponseDto? SuspiciousActivityReport { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public UserResponseDto? RequestedByUser { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public UserResponseDto? ReviewedByUser { get; init; }
}
