using Core.Models.ApiVersions;
using Presentation.Models.ApiVersions;

namespace Presentation.Mappers.ApiVersions;

public static class ApiVersionCatalogueDtoMapper
{
    public const string Current = "Current";
    public const string Supported = "Supported";
    public const string Deprecated = "Deprecated";

    public static ApiVersionCatalogueResponseDto ToDto(this ApiVersionCatalogue apiVersionCatalogue)
    {
        return new ApiVersionCatalogueResponseDto
        {
            Current = apiVersionCatalogue.Current,
            Versions = apiVersionCatalogue.Versions
                .OrderByDescending(x => x.Version)
                .Select(x => x.ToDto(apiVersionCatalogue))
                .ToArray(),
            MinimumNoticeMonths = apiVersionCatalogue.MinimumNotice,
            BreakingChanges = apiVersionCatalogue.BreakingChanges.Select(x => x.ToString()).ToArray(),
            NonBreakingChanges = apiVersionCatalogue.NonBreakingChanges.Select(x => x.ToString()).ToArray()
        };
    }

    private static ApiVersionDefinitionResponseDto ToDto(this ApiVersionDefinition apiVersionDefinition, ApiVersionCatalogue apiVersionCatalogue)
    {
        return new ApiVersionDefinitionResponseDto
        {
            Version = apiVersionDefinition.Version,
            Status = ToStatus(apiVersionDefinition, apiVersionCatalogue),
            ReleasedOn = apiVersionDefinition.ReleasedOn,
            DeprecatedOn = apiVersionDefinition.DeprecatedOn,
            SunsetOn = apiVersionDefinition.SunsetOn,
            Summary = apiVersionDefinition.Summary
        };
    }

    private static string ToStatus(ApiVersionDefinition apiVersionDefinition, ApiVersionCatalogue apiVersionCatalogue)
    {
        if (apiVersionDefinition.DeprecatedOn.HasValue)
        {
            return Deprecated;
        }

        return apiVersionDefinition.Version == apiVersionCatalogue.Current ? Current : Supported;
    }
}
