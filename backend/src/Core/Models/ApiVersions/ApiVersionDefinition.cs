using Core.ValueObjects;

namespace Core.Models.ApiVersions;

public record ApiVersionDefinition
{
    public required ApiVersion Version { get; init; }
    public required ReleasedOn ReleasedOn { get; init; }
    public required DeprecatedOn? DeprecatedOn { get; init; }
    public required SunsetOn? SunsetOn { get; init; }
    public required ApiVersionSummary Summary { get; init; }
}
