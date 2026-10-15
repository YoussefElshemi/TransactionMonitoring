using Core.Models.Cases;
using Core.Models.Common;
using Core.Models.Parties;
using Core.ValueObjects;

namespace Core.Models.Search;

public record SearchResults
{
    public required SearchQuery Query { get; init; }
    public required PagedResponse<Party>? Parties { get; init; }
    public required PagedResponse<Case>? Cases { get; init; }
}
