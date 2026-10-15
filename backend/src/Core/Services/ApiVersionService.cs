using Core.Constants;
using Core.Interfaces.Services;
using Core.Models.ApiVersions;
using Core.ValueObjects;

namespace Core.Services;

public class ApiVersionService : IApiVersionService
{
    private readonly IReadOnlyList<ApiVersionDefinition> _versions;

    public ApiVersionService() : this(ApiVersions.All)
    {
    }

    public ApiVersionService(IReadOnlyList<ApiVersionDefinition> versions)
    {
        if (versions.Count == 0)
        {
            throw new ArgumentException("At least one API version has to be served.", nameof(versions));
        }

        _versions = versions.OrderBy(x => x.Version).ToList();
    }

    public ApiVersionDefinition Current => _versions[^1];

    public ApiVersionDefinition Earliest => _versions[0];

    public ApiVersionDefinition? Find(ApiVersion apiVersion)
    {
        return _versions.FirstOrDefault(x => x.Version == apiVersion);
    }

    public ApiVersionDefinition Resolve(ApiVersion? pinned)
    {
        if (!pinned.HasValue)
        {
            return Current;
        }

        return Find(pinned.Value) ?? Earliest;
    }

    public ApiVersionCatalogue GetCatalogue()
    {
        return new ApiVersionCatalogue
        {
            Current = Current.Version,
            Versions = _versions,
            MinimumNotice = new NoticeMonths(ApiVersions.MinimumNoticeMonths),
            BreakingChanges = ApiVersions.BreakingChanges,
            NonBreakingChanges = ApiVersions.NonBreakingChanges
        };
    }
}
