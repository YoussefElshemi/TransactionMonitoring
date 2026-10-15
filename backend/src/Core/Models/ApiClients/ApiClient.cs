using Core.Enums;
using Core.Models.BaseModels;
using Core.Models.Organizations;
using Core.ValueObjects;

namespace Core.Models.ApiClients;

public record ApiClient : DeletableModel
{
    public required ApiClientId ApiClientId { get; init; }
    public required ClientId ClientId { get; init; }
    public required FriendlyName FriendlyName { get; init; }
    public required IsEnabled IsEnabled { get; init; }
    public required OrganizationId OrganizationId { get; init; }
    public required Organization? Organization { get; init; }
    public required ApplicationUserId CreatedByUserId { get; init; }
    public required List<ApiScope> Scopes { get; init; }
    public required List<ApiClientSecret> Secrets { get; init; }

    public required Mode Mode { get; init; }

    public required ApiVersion ApiVersion { get; init; }
}
