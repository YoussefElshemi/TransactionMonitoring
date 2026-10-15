using Core.Helpers;
using Core.Models.ApiClients;
using Core.ValueObjects;
using Presentation.Models.ApiClients;

namespace Presentation.Mappers.ApiClients;

public static class UpdateApiClientRequestDtoMapper
{
    public static UpdateApiClientRequest ToModel(this UpdateApiClientRequestDto updateApiClientRequestDto)
    {
        return new UpdateApiClientRequest
        {
            ApiClientId = new ApiClientId(updateApiClientRequestDto.ApiClientId),
            FriendlyName = updateApiClientRequestDto.Body.FriendlyName != null ? new FriendlyName(updateApiClientRequestDto.Body.FriendlyName) : null,
            IsEnabled = updateApiClientRequestDto.Body.IsEnabled.HasValue ? new IsEnabled(updateApiClientRequestDto.Body.IsEnabled.Value) : null,
            Scopes = updateApiClientRequestDto.Body.Scopes?.Select(ApiScopeHelper.Parse).ToList(),
            ApiVersion = updateApiClientRequestDto.Body.ApiVersion != null ? new ApiVersion(updateApiClientRequestDto.Body.ApiVersion) : null
        };
    }
}
