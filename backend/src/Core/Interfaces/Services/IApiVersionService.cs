using Core.Models.ApiVersions;
using Core.ValueObjects;

namespace Core.Interfaces.Services;

public interface IApiVersionService
{
    ApiVersionDefinition Current { get; }
    ApiVersionDefinition Earliest { get; }
    ApiVersionDefinition? Find(ApiVersion apiVersion);
    ApiVersionDefinition Resolve(ApiVersion? pinned);
    ApiVersionCatalogue GetCatalogue();
}
