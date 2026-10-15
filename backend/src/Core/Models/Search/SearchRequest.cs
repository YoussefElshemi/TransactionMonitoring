using Core.Enums;
using Core.Models.Permissions;
using Core.ValueObjects;

namespace Core.Models.Search;

public record SearchRequest
{
    public required OrganizationId OrganizationId { get; init; }
    public required SearchQuery Query { get; init; }

    public required List<SearchResultType> Types { get; init; }

    public required PageSize Limit { get; init; }

    public required PermissionFilter? CasePermissionFilter { get; init; }
}
