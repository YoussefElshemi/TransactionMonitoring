namespace Presentation.Models.ApiVersions;

public record ApiVersionCatalogueResponseDto
{
    public required string Current { get; init; }
    public required ApiVersionDefinitionResponseDto[] Versions { get; init; }
    public required int MinimumNoticeMonths { get; init; }
    public required string[] BreakingChanges { get; init; }
    public required string[] NonBreakingChanges { get; init; }
}

public record ApiVersionDefinitionResponseDto
{
    public required string Version { get; init; }
    public required string Status { get; init; }
    public required DateOnly ReleasedOn { get; init; }
    public required DateOnly? DeprecatedOn { get; init; }
    public required DateOnly? SunsetOn { get; init; }
    public required string Summary { get; init; }
}
