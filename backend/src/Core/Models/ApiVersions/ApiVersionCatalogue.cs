using Core.ValueObjects;

namespace Core.Models.ApiVersions;

public record ApiVersionCatalogue
{
    public required ApiVersion Current { get; init; }
    public required IReadOnlyList<ApiVersionDefinition> Versions { get; init; }
    public required NoticeMonths MinimumNotice { get; init; }
    public required IReadOnlyList<ContractRule> BreakingChanges { get; init; }
    public required IReadOnlyList<ContractRule> NonBreakingChanges { get; init; }
}
