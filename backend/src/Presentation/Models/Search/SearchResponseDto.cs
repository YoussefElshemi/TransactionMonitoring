using Presentation.Models.Cases;
using Presentation.Models.Parties;

namespace Presentation.Models.Search;

public record SearchResponseDto
{
    public required string Query { get; init; }

    public required SearchSectionDto<PartyResourceDto>? Parties { get; init; }
    public required SearchSectionDto<CaseResourceDto>? Cases { get; init; }
}

public record SearchSectionDto<T>
{
    public required int TotalCount { get; init; }
    public required bool HasMore { get; init; }
    public required T[] Data { get; init; }
}
