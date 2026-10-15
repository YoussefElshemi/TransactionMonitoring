using Core.Models.Common;
using Core.ValueObjects;

namespace Core.Models.CaseLinks;

public record ListCaseLinkRequest : PagedRequest
{
    public required OrganizationId OrganizationId { get; init; }

    public required CaseId CaseId { get; init; }
}
