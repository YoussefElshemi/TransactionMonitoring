using Core.Models.Common;
using Core.ValueObjects;

namespace Core.Models.BackTests;

public record ListBackTestRequest : PagedRequest
{
    public required OrganizationId OrganizationId { get; init; }
    public required RuleId? RuleId { get; init; }
}
