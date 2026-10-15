using System.Net;
using System.Net.Http.Json;
using AutoFixture;
using BehaviouralTests.TestHelpers;
using Core.Enums;
using Core.Helpers;
using Core.ValueObjects;
using FluentAssertions;
using Infrastructure;
using Infrastructure.Configurations;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Constants;
using Presentation.Models.ApiClients;
using Presentation.Models.Common;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.ApiClients;

namespace BehaviouralTests.Tests.Controllers;

public class ApiClientControllerTests(TestFixture testFixture) : IClassFixture<TestFixture>, IAsyncLifetime
{
    private readonly Fixture _fixture = AutoFixtureHelper.Create();
    private readonly HttpClient _httpClient = testFixture.CreateClient();

    private const string Email = "username@mail.com";
    private const string Password = "P@$$w0rd";
    private static readonly Guid OrganizationId = new("4d6389e9-f5d1-4325-a004-915865e3adec");

    [Fact]
    public async Task CreateApiClient_NotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var createRequest = FakeCreateApiClientRequestDto.CreateValid(_fixture);

        // Act
        var response = await _httpClient.PostAsJsonAsync("/api-clients", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task CreateApiClient_NotManager_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var createRequest = FakeCreateApiClientRequestDto.CreateValid(_fixture);

        // Act
        var response = await _httpClient.PostAsJsonAsync("/api-clients", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task CreateApiClient_GivenAnUnknownScope_ShouldReturnBadRequest()
    {
        // Arrange
        await AuthenticateAsManagerAsync();
        var createRequest = FakeCreateApiClientRequestDto.CreateInvalid(_fixture);

        // Act
        var response = await _httpClient.PostAsJsonAsync("/api-clients", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task CreateApiClient_GivenValidRequest_ShouldReturnCreatedWithCredentials()
    {
        // Arrange
        await AuthenticateAsManagerAsync();
        var createRequest = FakeCreateApiClientRequestDto.CreateValid(_fixture);

        // Act
        var response = await _httpClient.PostAsJsonAsync("/api-clients", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);

        var created = await response.Content.ReadDataFromJsonAsync<CreatedApiClientResourceDto>();
        created.Should().NotBeNull();
        created!.ApiClient.ClientId.Should().StartWith("tmc_");
        created.ApiClient.ClientSecret.Should().StartWith("tmsec_");
        created.ApiClient.Scopes.Should().BeEquivalentTo(createRequest.Scopes);
        created.ApiClient.IsEnabled.Should().BeTrue();

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var apiClientSecretEntity = await context.ApiClientSecrets.FirstAsync(x => x.ApiClientId == created.ApiClient.ApiClientId);
        apiClientSecretEntity.SecretHash.Should().Be(ApiClientCredentialHelper.Hash(new ClientSecret(created.ApiClient.ClientSecret)));
        apiClientSecretEntity.SecretHash.Should().NotBe(created.ApiClient.ClientSecret);
    }

    [Fact]
    public async Task GetApiClient_GivenValidRequest_ShouldNotReturnAnySecretMaterial()
    {
        // Arrange
        await AuthenticateAsManagerAsync();
        var created = await CreateApiClientAsync();

        // Act
        var response = await _httpClient.GetAsync($"/api-clients/{created.ApiClient.ApiClientId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotContain(created.ApiClient.ClientSecret);
        body.Should().Contain(created.ApiClient.ClientId);
    }

    [Fact]
    public async Task ListApiClientSecrets_GivenValidRequest_ShouldReturnOnlyTheSecretPrefix()
    {
        // Arrange
        await AuthenticateAsManagerAsync();
        var created = await CreateApiClientAsync();

        // Act
        var response = await _httpClient.GetAsync($"/api-clients/{created.ApiClient.ApiClientId}/secrets");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var secrets = await response.Content.ReadDataFromJsonAsync<List<ApiClientSecretResponseDto>>();
        secrets.Should().ContainSingle();
        secrets![0].SecretPrefix.Should().Be(created.ApiClient.ClientSecret[..12]);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotContain(created.ApiClient.ClientSecret);
    }

    [Fact]
    public async Task CreateApiClientSecret_GivenValidRequest_ShouldReturnAnAdditionalSecret()
    {
        // Arrange
        await AuthenticateAsManagerAsync();
        var created = await CreateApiClientAsync();

        // Act
        var response = await _httpClient.PostAsJsonAsync(
            $"/api-clients/{created.ApiClient.ApiClientId}/secrets", new CreateApiClientSecretRequestBodyDto { ExpiresAt = null });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var rotated = await response.Content.ReadDataFromJsonAsync<CreatedApiClientSecretResponseDto>();
        rotated.Should().NotBeNull();
        rotated!.ClientSecret.Should().StartWith("tmsec_");
        rotated.ClientSecret.Should().NotBe(created.ApiClient.ClientSecret);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var secrets = await context.ApiClientSecrets.Where(x => x.ApiClientId == created.ApiClient.ApiClientId).ToListAsync();
        secrets.Should().HaveCount(2);
        secrets.Should().OnlyContain(x => x.RevokedAt == null);
    }

    [Fact]
    public async Task RevokeApiClientSecret_GivenValidRequest_ShouldStampRevokedAt()
    {
        // Arrange
        await AuthenticateAsManagerAsync();
        var created = await CreateApiClientAsync();

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
        var secret = await context.ApiClientSecrets.FirstAsync(x => x.ApiClientId == created.ApiClient.ApiClientId);

        // Act
        var response = await _httpClient.DeleteAsync($"/api-clients/{created.ApiClient.ApiClientId}/secrets/{secret.ApiClientSecretId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var assertScope = testFixture.Services.CreateScope();
        var assertContext = assertScope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
        var revoked = await assertContext.ApiClientSecrets.FirstAsync(x => x.ApiClientSecretId == secret.ApiClientSecretId);
        revoked.RevokedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task RevokeApiClientSecret_RepeatedForTheSameSecret_ShouldKeepTheOriginalRevocation()
    {
        // Arrange
        await AuthenticateAsManagerAsync();
        var created = await CreateApiClientAsync();

        Guid apiClientSecretId;
        DateTimeOffset? revokedAt;

        using (var scope = testFixture.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
            apiClientSecretId = (await context.ApiClientSecrets.FirstAsync(x => x.ApiClientId == created.ApiClient.ApiClientId)).ApiClientSecretId;
        }

        var url = $"/api-clients/{created.ApiClient.ApiClientId}/secrets/{apiClientSecretId}";
        (await _httpClient.DeleteAsync(url)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        using (var scope = testFixture.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
            revokedAt = (await context.ApiClientSecrets.FirstAsync(x => x.ApiClientSecretId == apiClientSecretId)).RevokedAt;
        }

        // Act
        var response = await _httpClient.DeleteAsync(url);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var assertScope = testFixture.Services.CreateScope();
        var assertContext = assertScope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var reRevoked = await assertContext.ApiClientSecrets.FirstAsync(x => x.ApiClientSecretId == apiClientSecretId);
        reRevoked.RevokedAt.Should().Be(revokedAt);

        var auditLogs = await assertContext.AuditLogs
            .Where(x => x.EntityTypeId == (int)AuditableEntityType.ApiClientSecret &&
                        x.EntityId == apiClientSecretId &&
                        x.OperationId == (int)AuditOperation.Updated)
            .CountAsync();

        auditLogs.Should().Be(1);
    }

    [Fact]
    public async Task RevokeApiClientSecret_SecretBelongsToAnotherClient_ShouldReturnNotFound()
    {
        // Arrange
        await AuthenticateAsManagerAsync();
        var created = await CreateApiClientAsync();
        var other = await CreateApiClientAsync();

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
        var otherSecret = await context.ApiClientSecrets.FirstAsync(x => x.ApiClientId == other.ApiClient.ApiClientId);

        // Act
        var response = await _httpClient.DeleteAsync($"/api-clients/{created.ApiClient.ApiClientId}/secrets/{otherSecret.ApiClientSecretId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ListApiClients_GivenValidRequest_ShouldReturnOk()
    {
        // Arrange
        await AuthenticateAsManagerAsync();
        await CreateApiClientAsync();

        // Act
        var response = await _httpClient.GetAsync("/api-clients");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var apiClients = await response.Content.ReadFromJsonAsync<PagedResponseDto<ApiClientResourceDto>>();
        apiClients.Should().NotBeNull();
        apiClients!.Data.Should().ContainSingle();
    }

    [Fact]
    public async Task UpdateApiClient_GivenValidRequest_ShouldReplaceTheScopes()
    {
        // Arrange
        await AuthenticateAsManagerAsync();
        var created = await CreateApiClientAsync();

        var updateBody = new UpdateApiClientRequestBodyDto
        {
            IsEnabled = false,
            Scopes = [ApiScopeHelper.ToScopeString(ApiScope.RulesEvaluate)]
        };

        // Act
        var response = await _httpClient.PatchAsJsonAsync($"/api-clients/{created.ApiClient.ApiClientId}", updateBody);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await _httpClient.GetAsync($"/api-clients/{created.ApiClient.ApiClientId}");
        var apiClient = await getResponse.Content.ReadDataFromJsonAsync<ApiClientResourceDto>();
        apiClient!.ApiClient.IsEnabled.Should().BeFalse();
        apiClient.ApiClient.Scopes.Should().BeEquivalentTo(ApiScopeHelper.ToScopeString(ApiScope.RulesEvaluate));
    }

    [Fact]
    public async Task DeleteApiClient_GivenValidRequest_ShouldSoftDelete()
    {
        // Arrange
        await AuthenticateAsManagerAsync();
        var created = await CreateApiClientAsync();

        // Act
        var response = await _httpClient.DeleteAsync($"/api-clients/{created.ApiClient.ApiClientId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
        var apiClientEntity = await context.ApiClients.FirstAsync(x => x.ApiClientId == created.ApiClient.ApiClientId);
        apiClientEntity.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task RecoverApiClient_GivenValidRequest_ShouldUndoTheSoftDelete()
    {
        // Arrange
        await AuthenticateAsManagerAsync();
        var created = await CreateApiClientAsync();
        await _httpClient.DeleteAsync($"/api-clients/{created.ApiClient.ApiClientId}");

        // Act
        var response = await _httpClient.PostAsync($"/api-clients/{created.ApiClient.ApiClientId}/recover", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
        var apiClientEntity = await context.ApiClients.FirstAsync(x => x.ApiClientId == created.ApiClient.ApiClientId);
        apiClientEntity.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task GetApiClient_BelongsToAnotherOrganization_ShouldReturnNotFound()
    {
        // Arrange
        await AuthenticateAsManagerAsync();

        var foreignUserEntity = FakeApplicationUserEntity.CreateValid(_fixture);
        foreignUserEntity.OrganizationId = OtherOrganizationId;

        await DatabaseHelper.CreateApplicationUsers(testFixture.Services, [foreignUserEntity]);

        var apiClientEntity = FakeApiClientEntity.CreateValid(_fixture) with
        {
            OrganizationId = OtherOrganizationId,
            Organization = null!,
            CreatedByUserId = foreignUserEntity.Id,
            CreatedByUser = null!,
            Scopes = []
        };

        await DatabaseHelper.CreateApiClients(testFixture.Services, [apiClientEntity]);

        // Act
        var response = await _httpClient.GetAsync($"/api-clients/{apiClientEntity.ApiClientId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task AuthenticateAsManagerAsync()
    {
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
    }

    private async Task<CreatedApiClientResourceDto> CreateApiClientAsync()
    {
        var createRequest = FakeCreateApiClientRequestDto.CreateValid(_fixture);
        var response = await _httpClient.PostAsJsonAsync("/api-clients", createRequest);

        return (await response.Content.ReadDataFromJsonAsync<CreatedApiClientResourceDto>())!;
    }

    private static readonly Guid OtherOrganizationId = new("03a9b48e-4af2-40bd-a180-cac58bd930f3");

    public async Task InitializeAsync()
    {
        await testFixture.ResetDatabaseAsync();

        var organizationEntities = new List<OrganizationEntity>
        {
            OrganizationConfigurations.DefaultOrganization,
            FakeOrganizationEntity.CreateValid(_fixture) with
            {
                OrganizationId = OrganizationId,
                StatusId = (int)OrganizationStatus.Enabled,
                IsDeleted = false
            },
            FakeOrganizationEntity.CreateValid(_fixture) with
            {
                OrganizationId = OtherOrganizationId,
                StatusId = (int)OrganizationStatus.Enabled,
                IsDeleted = false
            }
        };

        await DatabaseHelper.CreateOrganizations(testFixture.Services, organizationEntities);
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}
