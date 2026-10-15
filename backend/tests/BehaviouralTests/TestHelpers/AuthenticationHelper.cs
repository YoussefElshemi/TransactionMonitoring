using System.Net.Http.Headers;
using System.Net.Http.Json;
using Core.Enums;
using Core.Interfaces;
using Core.Interfaces.Services;
using Core.Models.ApiClients;
using Infrastructure.Entities;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Constants;
using Presentation.Models.Auth;
using Presentation.Models.OAuth;

namespace BehaviouralTests.TestHelpers;

public static class AuthenticationHelper
{
    public static async Task<ApplicationUserEntity> CreateUserAsync(IServiceProvider serviceProvider, string email, string password, List<UserRole> roles, Guid organizationId)
    {
        using var scope = serviceProvider.CreateScope();
        var userStore = scope.ServiceProvider.GetRequiredService<IUserStore<ApplicationUserEntity>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUserEntity>>();
        var timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();

        var user = new ApplicationUserEntity
        {
            OrganizationId = organizationId,
            CreatedAt = timeProvider.GetUtcNow(),
            UpdatedAt = timeProvider.GetUtcNow(),
            IsDeleted = false,
            DeletedAt = null
        };

        var emailStore = (IUserEmailStore<ApplicationUserEntity>)userStore;
        await userStore.SetUserNameAsync(user, email, CancellationToken.None);
        await emailStore.SetEmailAsync(user, email, CancellationToken.None);
        await userManager.CreateAsync(user, password);

        await userManager.AddToRolesAsync(user, roles.Select(x => x.ToString()));

        return (await userManager.FindByEmailAsync(email))!;
    }

    public static async Task<CreatedApiClient> CreateApiClientAsync(
        IServiceProvider serviceProvider,
        CreateApiClientRequest createApiClientRequest,
        Mode mode = Mode.Live)
    {
        using var scope = serviceProvider.CreateScope();
        var apiClientService = scope.ServiceProvider.GetRequiredService<IApiClientService>();

        // The key's mode comes from the creating request, so establish one as the middleware would.
        scope.ServiceProvider.GetRequiredService<IModeContext>().Current = mode;

        return await apiClientService.CreateAsync(createApiClientRequest, CancellationToken.None);
    }

    public static async Task SetTokenAsync(HttpClient httpClient, string userName, string password)
    {
        var token = await GetTokenAsync(httpClient, userName, password);
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public static async Task SetCookieAsync(HttpClient httpClient, string userName, string password)
    {
        var loginRequest = new LoginRequestDto
        {
            UserName = userName,
            Password = password
        };

        var loginResponse = await httpClient.PostAsJsonAsync("/auth/login?useCookies=true", loginRequest);

        loginResponse.EnsureSuccessStatusCode();
    }

    public static async Task SetClientCredentialsTokenAsync(HttpClient httpClient, string clientId, string clientSecret, string? scope = null)
    {
        var tokenResponse = await RequestClientCredentialsTokenAsync(httpClient, clientId, clientSecret, scope);

        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenResponse!.AccessToken);
    }

    public static async Task<TokenResponseDto?> RequestClientCredentialsTokenAsync(HttpClient httpClient, string clientId, string clientSecret, string? scope = null)
    {
        var response = await PostTokenFormAsync(httpClient, new Dictionary<string, string>
        {
            ["grant_type"] = OAuthConstants.ClientCredentialsGrantType,
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
            ["scope"] = scope ?? string.Empty
        });

        return await response.Content.ReadFromJsonAsync<TokenResponseDto>();
    }

    public static Task<HttpResponseMessage> PostTokenFormAsync(HttpClient httpClient, Dictionary<string, string> form)
    {
        return httpClient.PostAsync("/oauth/token", new FormUrlEncodedContent(form));
    }

    private static async Task<string> GetTokenAsync(HttpClient httpClient, string userName, string password)
    {
        var loginRequest = new LoginRequestDto
        {
            UserName = userName,
            Password = password
        };

        var loginResponse = await httpClient.PostAsJsonAsync("/auth/login", loginRequest);
        var accessTokenResponse = await loginResponse.Content.ReadFromJsonAsync<AccessTokenResponse>();

        return accessTokenResponse!.AccessToken;
    }
}
