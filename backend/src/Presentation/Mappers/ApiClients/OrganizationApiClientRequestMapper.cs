using Core.Helpers;
using Core.Models.ApiClients;
using Core.ValueObjects;
using Presentation.Models.ApiClients;

namespace Presentation.Mappers.ApiClients;

public static class OrganizationApiClientRequestMapper
{
    public static ListApiClientRequest ToModel(this OrganizationListApiClientRequestDto organizationListApiClientRequestDto)
    {
        return new ListApiClientRequest
        {
            OrganizationId = new OrganizationId(organizationListApiClientRequestDto.OrganizationId),
            FriendlyName = !string.IsNullOrWhiteSpace(organizationListApiClientRequestDto.FriendlyName)
                ? new FriendlyName(organizationListApiClientRequestDto.FriendlyName)
                : null,
            PageNumber = new PageNumber(organizationListApiClientRequestDto.PageNumber),
            PageSize = new PageSize(organizationListApiClientRequestDto.PageSize),
            FromDate = new DateRange(organizationListApiClientRequestDto.FromDate),
            ToDate = new DateRange(organizationListApiClientRequestDto.ToDate)
        };
    }

    public static UpdateApiClientRequest ToModel(this OrganizationUpdateApiClientRequestDto organizationUpdateApiClientRequestDto)
    {
        return new UpdateApiClientRequest
        {
            ApiClientId = new ApiClientId(organizationUpdateApiClientRequestDto.ApiClientId),
            FriendlyName = organizationUpdateApiClientRequestDto.Body.FriendlyName != null
                ? new FriendlyName(organizationUpdateApiClientRequestDto.Body.FriendlyName)
                : null,
            IsEnabled = organizationUpdateApiClientRequestDto.Body.IsEnabled.HasValue
                ? new IsEnabled(organizationUpdateApiClientRequestDto.Body.IsEnabled.Value)
                : null,
            Scopes = organizationUpdateApiClientRequestDto.Body.Scopes?.Select(ApiScopeHelper.Parse).ToList(),
            ApiVersion = organizationUpdateApiClientRequestDto.Body.ApiVersion != null
                ? new ApiVersion(organizationUpdateApiClientRequestDto.Body.ApiVersion)
                : null
        };
    }
}
