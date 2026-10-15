using Core.Models.ApiVersions;
using Core.ValueObjects;

namespace Core.Constants;

public static class ApiVersions
{
    public const string V20261014 = "2026-10-14";

    public const int MinimumNoticeMonths = 12;

    public static readonly IReadOnlyList<ApiVersionDefinition> All =
    [
        new ApiVersionDefinition
        {
            Version = new ApiVersion(V20261014),
            ReleasedOn = new ReleasedOn(new DateOnly(2026, 10, 14)),
            DeprecatedOn = null,
            SunsetOn = null,
            Summary = new ApiVersionSummary(
                "The first versioned contract. Every client created before versioning existed is pinned here, " +
                "because this is the API they were written against.")
        }
    ];

    public static readonly ApiVersion Current = All.Max(x => x.Version);

    public static readonly IReadOnlyList<ContractRule> BreakingChanges =
    [
        new("Removing or renaming an endpoint, a request field, a response field, a query parameter or a header."),
        new("Changing the type or format of a field, or what an existing value of it means."),
        new("Adding a required request field or parameter, or making an optional one required."),
        new("Tightening validation so that a request accepted before is refused."),
        new("Removing or renaming a value of an enumeration."),
        new("Removing or renaming an error code, or changing the HTTP status it arrives with."),
        new("Changing a default: a page size, a sort order, the mode an absent header selects."),
        new("Requiring a permission or a scope an endpoint did not require before.")
    ];

    public static readonly IReadOnlyList<ContractRule> NonBreakingChanges =
    [
        new("Adding an endpoint."),
        new("Adding an optional request field, query parameter or header."),
        new("Adding a field to a response, or a header to one. Ignore members you do not recognise."),
        new("Adding a value to an enumeration. Treat a value you do not recognise as unknown rather than failing."),
        new("Adding an error code. Fall back on the HTTP status for a code you do not recognise."),
        new("Rewording a title, a detail or a validation message. Branch on the code, never on the sentence."),
        new("Loosening validation so that a request refused before is accepted."),
        new("Changing the order of members in an object or of entries in the error catalogue.")
    ];
}
