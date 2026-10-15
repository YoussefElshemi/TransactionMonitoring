using System.Net;
using System.Net.Http.Json;
using AutoFixture;
using BehaviouralTests.TestHelpers;
using Core.Enums;
using FluentAssertions;
using Infrastructure;
using Infrastructure.Configurations;
using Infrastructure.Entities;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Constants;
using Presentation.Models.Common;
using Presentation.Models.WebhookConfigurations;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.OrganizationWebhookConfigurations;

namespace BehaviouralTests.Tests.Controllers;

public class OrganizationWebhookControllerTests(TestFixture testFixture) : IClassFixture<TestFixture>, IAsyncLifetime
{
    private readonly Fixture _fixture = AutoFixtureHelper.Create();
    private readonly HttpClient _httpClient = testFixture.CreateClient();

    private const string Email = "username@mail.com";
    private const string Password = "P@$$w0rd";
    private static readonly Guid OrganizationId = new("4d6389e9-f5d1-4325-a004-915865e3adec");
    private static readonly Guid DisabledOrganizationId = new("03a9b48e-4af2-40bd-a180-cac58bd930f3");

    [Fact]
    public async Task CreateOrganizationWebhook_NotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var createRequest = FakeCreateWebhookConfigurationRequestDto.CreateValid(_fixture);

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/organizations/{OrganizationId}/webhook-configurations", createRequest.Body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task CreateOrganizationWebhook_NotAdmin_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var createRequest = FakeCreateWebhookConfigurationRequestDto.CreateValid(_fixture);

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/organizations/{OrganizationId}/webhook-configurations", createRequest.Body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task CreateOrganizationWebhook_GivenInvalidRequest_ShouldReturnBadRequest()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Admin], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var createRequest = FakeCreateWebhookConfigurationRequestDto.CreateInvalid(_fixture);

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/organizations/{OrganizationId}/webhook-configurations", createRequest.Body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task CreateOrganizationWebhook_GivenValidRequest_ShouldReturnCreated()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Admin], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var createRequest = FakeCreateWebhookConfigurationRequestDto.CreateValid(_fixture);

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/organizations/{OrganizationId}/webhook-configurations", createRequest.Body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);

        var createdOrganizationWebhook = await response.Content.ReadDataFromJsonAsync<WebhookConfigurationSecretResponseDto>();
        createdOrganizationWebhook.Should().NotBeNull();
        createdOrganizationWebhook!.EndpointUrl.Should().Be(createRequest.Body.EndpointUrl);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
        var webhookConfigurationEntity = context.WebhookConfigurations.FirstOrDefault(x => x.WebhookConfigurationId == createdOrganizationWebhook.WebhookConfigurationId);
        webhookConfigurationEntity.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateOrganizationWebhook_GivenValidRequest_ShouldReturnGeneratedSecret()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Admin], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var createRequest = FakeCreateWebhookConfigurationRequestDto.CreateValid(_fixture);

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/organizations/{OrganizationId}/webhook-configurations", createRequest.Body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdOrganizationWebhook = await response.Content.ReadDataFromJsonAsync<WebhookConfigurationSecretResponseDto>();
        createdOrganizationWebhook.Should().NotBeNull();
        createdOrganizationWebhook!.SecretKey.Should().StartWith("whsec_");
        createdOrganizationWebhook.SecretKey.Should().NotContain(createdOrganizationWebhook.WebhookConfigurationId.ToString());

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
        var webhookConfigurationEntity = context.WebhookConfigurations.FirstOrDefault(x => x.WebhookConfigurationId == createdOrganizationWebhook.WebhookConfigurationId);
        webhookConfigurationEntity.Should().NotBeNull();
        webhookConfigurationEntity!.SecretKey.Should().Be(createdOrganizationWebhook.SecretKey);
    }

    [Fact]
    public async Task GetOrganizationWebhook_GivenValidRequest_ShouldNotReturnSecret()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Admin], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var webhookConfigurationEntity = FakeWebhookConfigurationEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            SecretKey = "whsec_notdisclosed",
            IsDeleted = false
        };

        await DatabaseHelper.CreateWebhookConfigurations(testFixture.Services, [webhookConfigurationEntity]);

        // Act
        var response = await _httpClient.GetAsync(
            $"/organizations/{OrganizationId}/webhook-configurations/{webhookConfigurationEntity.WebhookConfigurationId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotContain("whsec_notdisclosed");
    }

    [Fact]
    public async Task RotateOrganizationWebhookSecret_GivenValidRequest_ShouldReplaceSecret()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Admin], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var webhookConfigurationEntity = FakeWebhookConfigurationEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            SecretKey = "whsec_original",
            IsDeleted = false
        };

        await DatabaseHelper.CreateWebhookConfigurations(testFixture.Services, [webhookConfigurationEntity]);

        // Act
        var response = await _httpClient.PostAsync(
            $"/organizations/{OrganizationId}/webhook-configurations/{webhookConfigurationEntity.WebhookConfigurationId}/rotate-secret", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var rotatedOrganizationWebhook = await response.Content.ReadDataFromJsonAsync<WebhookConfigurationSecretResponseDto>();
        rotatedOrganizationWebhook.Should().NotBeNull();
        rotatedOrganizationWebhook!.SecretKey.Should().StartWith("whsec_");
        rotatedOrganizationWebhook.SecretKey.Should().NotBe("whsec_original");

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
        var updatedEntity = context.WebhookConfigurations.FirstOrDefault(x => x.WebhookConfigurationId == webhookConfigurationEntity.WebhookConfigurationId);
        updatedEntity.Should().NotBeNull();
        updatedEntity!.SecretKey.Should().Be(rotatedOrganizationWebhook.SecretKey);
    }

    [Fact]
    public async Task ListOrganizationWebhook_NotAuthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _httpClient.GetAsync($"/organizations/{OrganizationId}/webhook-configurations");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task ListOrganizationWebhook_NotAdmin_ShouldReturnForbidden()
    {
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.GetAsync($"/organizations/{OrganizationId}/webhook-configurations");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task ListOrganizationWebhook_GivenInvalidRequest_ShouldReturnBadRequest()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Admin], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        await DatabaseHelper.CreateWebhookConfigurations(testFixture.Services, Enumerable.Range(0, 50).Select(
            _ => FakeWebhookConfigurationEntity.CreateValid(_fixture) with
            {
                OrganizationId = OrganizationId,
                Organization = null!,
                IsDeleted = false
            })
        );

        // Act
        var response = await _httpClient.GetAsync($"/organizations/{OrganizationId}/webhook-configurations?PageSize=200");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task ListOrganizationWebhook_GivenValidRequest_ShouldReturnOk()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Admin], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        await DatabaseHelper.CreateWebhookConfigurations(testFixture.Services, Enumerable.Range(0, 50).Select(
            _ => FakeWebhookConfigurationEntity.CreateValid(_fixture) with
            {
                OrganizationId = OrganizationId,
                Organization = null!,
                IsDeleted = false
            })
        );

        // Act
        var response = await _httpClient.GetAsync($"/organizations/{OrganizationId}/webhook-configurations");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);

        var apiKeys = await response.Content.ReadFromJsonAsync<PagedResponseDto<WebhookConfigurationResourceDto>>();
        apiKeys.Should().NotBeNull();
        apiKeys!.Metadata.Should().NotBeNull();
        apiKeys.Metadata.PageCount.Should().Be(5);
        apiKeys.Metadata.PageNumber.Should().Be(1);
        apiKeys.Metadata.PageSize.Should().Be(10);
        apiKeys.Metadata.TotalCount.Should().Be(50);
        apiKeys.Metadata.HasMore.Should().BeTrue();
    }

    [Fact]
    public async Task GetOrganizationWebhook_NotAuthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _httpClient.GetAsync($"/organizations/{OrganizationId}/webhook-configurations/{_fixture.Create<Guid>()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task GetOrganizationWebhook_NotAdmin_ShouldReturnForbidden()
    {
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.GetAsync($"/organizations/{OrganizationId}/webhook-configurations/{_fixture.Create<Guid>()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task GetOrganizationWebhook_GivenValidRequest_ShouldReturnOk()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Admin], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        var webhookConfigurationEntity = FakeWebhookConfigurationEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            IsDeleted = false
        };

        await DatabaseHelper.CreateWebhookConfigurations(testFixture.Services, [webhookConfigurationEntity]);

        // Act
        var response = await _httpClient.GetAsync($"/organizations/{OrganizationId}/webhook-configurations/{webhookConfigurationEntity.WebhookConfigurationId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);

        var apiKey = await response.Content.ReadDataFromJsonAsync<WebhookConfigurationResourceDto>();
        apiKey.Should().NotBeNull();
        apiKey!.WebhookConfiguration.WebhookConfigurationId.Should().Be(webhookConfigurationEntity.WebhookConfigurationId);
        apiKey.WebhookConfiguration.EndpointUrl.Should().Be(webhookConfigurationEntity.EndpointUrl);
    }

    [Fact]
    public async Task UpdateOrganizationWebhook_NotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var updateRequest = FakeUpdateWebhookConfigurationRequestDto.CreateValid(_fixture);

        // Act
        var response = await _httpClient.PatchAsJsonAsync($"/organizations/{OrganizationId}/webhook-configurations/{updateRequest.WebhookConfigurationId}", updateRequest.Body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task UpdateOrganizationWebhook_NotAdmin_ShouldReturnForbidden()
    {
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var updateRequest = FakeUpdateWebhookConfigurationRequestDto.CreateValid(_fixture);

        // Act
        var response = await _httpClient.PatchAsJsonAsync($"/organizations/{OrganizationId}/webhook-configurations/{updateRequest.WebhookConfigurationId}", updateRequest.Body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task UpdateOrganizationWebhook_GivenInvalidRequest_ShouldReturnBadRequest()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Admin], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var webhookConfigurationEntity = FakeWebhookConfigurationEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            IsDeleted = false
        };

        await DatabaseHelper.CreateWebhookConfigurations(testFixture.Services, [webhookConfigurationEntity]);

        var updateRequest = FakeUpdateWebhookConfigurationRequestDto.CreateInvalid(_fixture) with
        {
            WebhookConfigurationId = webhookConfigurationEntity.WebhookConfigurationId,
            OrganizationId = OrganizationId
        };

        // Act
        var response = await _httpClient.PatchAsJsonAsync($"/organizations/{OrganizationId}/webhook-configurations/{updateRequest.WebhookConfigurationId}", updateRequest.Body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task UpdateOrganizationWebhook_GivenValidRequest_ShouldReturnNoContent()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Admin], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var webhookConfigurationEntity = FakeWebhookConfigurationEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            IsDeleted = false
        };

        await DatabaseHelper.CreateWebhookConfigurations(testFixture.Services, [webhookConfigurationEntity]);

        var updateRequest = FakeUpdateWebhookConfigurationRequestDto.CreateValid(_fixture) with
        {
            WebhookConfigurationId = webhookConfigurationEntity.WebhookConfigurationId,
            OrganizationId = OrganizationId
        };

        // Act
        var response = await _httpClient.PatchAsJsonAsync($"/organizations/{OrganizationId}/webhook-configurations/{updateRequest.WebhookConfigurationId}", updateRequest.Body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
        var updatedWebhookConfigurationEntity = context.WebhookConfigurations.FirstOrDefault(x => x.WebhookConfigurationId == updateRequest.WebhookConfigurationId);
        updatedWebhookConfigurationEntity.Should().NotBeNull();
        updatedWebhookConfigurationEntity!.EndpointUrl.Should().Be(updateRequest.Body.EndpointUrl);
    }

    [Fact]
    public async Task DeleteOrganizationWebhook_NotAuthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _httpClient.DeleteAsync($"/organizations/{OrganizationId}/webhook-configurations/{_fixture.Create<Guid>()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task DeleteOrganizationWebhook_NotAdmin_ShouldReturnForbidden()
    {
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.DeleteAsync($"/organizations/{OrganizationId}/webhook-configurations/{_fixture.Create<Guid>()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task DeleteOrganizationWebhook_GivenValidRequest_ShouldReturnNoContent()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Admin], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var webhookConfigurationEntity = FakeWebhookConfigurationEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            IsDeleted = false
        };

        await DatabaseHelper.CreateWebhookConfigurations(testFixture.Services, [webhookConfigurationEntity]);

        // Act
        var response = await _httpClient.DeleteAsync($"/organizations/{OrganizationId}/webhook-configurations/{webhookConfigurationEntity.WebhookConfigurationId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
        var updatedWebhookConfigurationEntity = context.WebhookConfigurations.FirstOrDefault(x => x.WebhookConfigurationId == webhookConfigurationEntity.WebhookConfigurationId);
        updatedWebhookConfigurationEntity.Should().NotBeNull();
        updatedWebhookConfigurationEntity!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task RecoverOrganizationWebhook_NotAuthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _httpClient.PostAsync($"/organizations/{OrganizationId}/webhook-configurations/{_fixture.Create<Guid>()}/recover", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task RecoverOrganizationWebhook_NotAdmin_ShouldReturnForbidden()
    {
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.PostAsync($"/organizations/{OrganizationId}/webhook-configurations/{_fixture.Create<Guid>()}/recover", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task RecoverOrganizationWebhook_GivenValidRequest_ShouldReturnNoContent()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Admin], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var webhookConfigurationEntity = FakeWebhookConfigurationEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            IsDeleted = true
        };

        await DatabaseHelper.CreateWebhookConfigurations(testFixture.Services, [webhookConfigurationEntity]);

        // Act
        var response = await _httpClient.PostAsync($"/organizations/{OrganizationId}/webhook-configurations/{webhookConfigurationEntity.WebhookConfigurationId}/recover", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
        var updatedWebhookConfigurationEntity = context.WebhookConfigurations.FirstOrDefault(x => x.WebhookConfigurationId == webhookConfigurationEntity.WebhookConfigurationId);
        updatedWebhookConfigurationEntity.Should().NotBeNull();
        updatedWebhookConfigurationEntity!.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task ListOrganizationWebhook_Manager_ShouldReadTheirOwnAndNoOther()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var own = await _httpClient.GetAsync($"/organizations/{OrganizationId}/webhook-configurations");
        var other = await _httpClient.GetAsync($"/organizations/{Guid.NewGuid()}/webhook-configurations");

        // Assert
        own.StatusCode.Should().Be(HttpStatusCode.OK);
        other.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        other.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task RotateOrganizationWebhookSecret_Manager_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var webhookConfigurationEntity = FakeWebhookConfigurationEntity.CreateValid(_fixture) with
        {
            OrganizationId = DisabledOrganizationId,
            Organization = null!,
            IsDeleted = false
        };

        await DatabaseHelper.CreateWebhookConfigurations(testFixture.Services, [webhookConfigurationEntity]);

        // Act
        var response = await _httpClient.PostAsync(
            $"/organizations/{DisabledOrganizationId}/webhook-configurations/{webhookConfigurationEntity.WebhookConfigurationId}/rotate-secret", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task GetOrganizationWebhookHistory_Manager_ShouldReadTheirOwnAndNoOther()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var own = await _httpClient.GetAsync(
            $"/organizations/{OrganizationId}/webhook-configurations/{_fixture.Create<Guid>()}/history");

        var other = await _httpClient.GetAsync(
            $"/organizations/{Guid.NewGuid()}/webhook-configurations/{_fixture.Create<Guid>()}/history");

        // Assert
        own.StatusCode.Should().Be(HttpStatusCode.OK);
        other.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        other.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task ListMyOrganizationWebhook_NotAuthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _httpClient.GetAsync("/organizations/my/webhook-configurations");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task ListMyOrganizationWebhook_NotManagerOrAdmin_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.GetAsync("/organizations/my/webhook-configurations");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task ListMyOrganizationWebhook_GivenManager_ShouldOnlyReturnOwnOrganizationWebhooks()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        await DatabaseHelper.CreateWebhookConfigurations(testFixture.Services, Enumerable.Range(0, 3).Select(
            _ => FakeWebhookConfigurationEntity.CreateValid(_fixture) with
            {
                OrganizationId = OrganizationId,
                Organization = null!,
                IsDeleted = false
            })
        );

        await DatabaseHelper.CreateWebhookConfigurations(testFixture.Services, [
            FakeWebhookConfigurationEntity.CreateValid(_fixture) with
            {
                OrganizationId = DisabledOrganizationId,
                Organization = null!,
                IsDeleted = false
            }
        ]);

        // Act
        var response = await _httpClient.GetAsync("/organizations/my/webhook-configurations");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);

        var webhookConfigurations = await response.Content.ReadFromJsonAsync<PagedResponseDto<WebhookConfigurationResourceDto>>();
        webhookConfigurations.Should().NotBeNull();
        webhookConfigurations!.Metadata.Should().NotBeNull();
        webhookConfigurations.Metadata.TotalCount.Should().Be(3);
        webhookConfigurations.Data.Should().OnlyContain(x => x.WebhookConfiguration.OrganizationId == OrganizationId);
    }

    [Fact]
    public async Task CreateMyOrganizationWebhook_GivenManager_ShouldReturnCreatedInOwnOrganization()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var createRequest = FakeCreateWebhookConfigurationRequestDto.CreateValid(_fixture);

        // Act
        var response = await _httpClient.PostAsJsonAsync("/organizations/my/webhook-configurations", createRequest.Body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);

        var createdOrganizationWebhook = await response.Content.ReadDataFromJsonAsync<WebhookConfigurationSecretResponseDto>();
        createdOrganizationWebhook.Should().NotBeNull();
        createdOrganizationWebhook!.EndpointUrl.Should().Be(createRequest.Body.EndpointUrl);
        createdOrganizationWebhook.SecretKey.Should().StartWith("whsec_");
        createdOrganizationWebhook.OrganizationId.Should().Be(OrganizationId);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
        var webhookConfigurationEntity = context.WebhookConfigurations.FirstOrDefault(x => x.WebhookConfigurationId == createdOrganizationWebhook.WebhookConfigurationId);
        webhookConfigurationEntity.Should().NotBeNull();
        webhookConfigurationEntity!.OrganizationId.Should().Be(OrganizationId);
    }

    [Fact]
    public async Task GetMyOrganizationWebhook_GivenManager_ShouldReturnOk()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var webhookConfigurationEntity = FakeWebhookConfigurationEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            SecretKey = "whsec_notdisclosed",
            IsDeleted = false
        };

        await DatabaseHelper.CreateWebhookConfigurations(testFixture.Services, [webhookConfigurationEntity]);

        // Act
        var response = await _httpClient.GetAsync($"/organizations/my/webhook-configurations/{webhookConfigurationEntity.WebhookConfigurationId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var webhookConfiguration = await response.Content.ReadDataFromJsonAsync<WebhookConfigurationResourceDto>();
        webhookConfiguration.Should().NotBeNull();
        webhookConfiguration!.WebhookConfiguration.WebhookConfigurationId.Should().Be(webhookConfigurationEntity.WebhookConfigurationId);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotContain("whsec_notdisclosed");
    }

    [Fact]
    public async Task RotateMyOrganizationWebhookSecret_GivenManager_ShouldReplaceSecret()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var webhookConfigurationEntity = FakeWebhookConfigurationEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            SecretKey = "whsec_original",
            IsDeleted = false
        };

        await DatabaseHelper.CreateWebhookConfigurations(testFixture.Services, [webhookConfigurationEntity]);

        // Act
        var response = await _httpClient.PostAsync(
            $"/organizations/my/webhook-configurations/{webhookConfigurationEntity.WebhookConfigurationId}/rotate-secret", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var rotatedOrganizationWebhook = await response.Content.ReadDataFromJsonAsync<WebhookConfigurationSecretResponseDto>();
        rotatedOrganizationWebhook.Should().NotBeNull();
        rotatedOrganizationWebhook!.SecretKey.Should().StartWith("whsec_");
        rotatedOrganizationWebhook.SecretKey.Should().NotBe("whsec_original");

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
        var updatedEntity = context.WebhookConfigurations.FirstOrDefault(x => x.WebhookConfigurationId == webhookConfigurationEntity.WebhookConfigurationId);
        updatedEntity.Should().NotBeNull();
        updatedEntity!.SecretKey.Should().Be(rotatedOrganizationWebhook.SecretKey);
    }

    [Fact]
    public async Task UpdateMyOrganizationWebhook_GivenManager_ShouldReturnNoContent()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var webhookConfigurationEntity = FakeWebhookConfigurationEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            IsDeleted = false
        };

        await DatabaseHelper.CreateWebhookConfigurations(testFixture.Services, [webhookConfigurationEntity]);

        var updateRequest = FakeUpdateWebhookConfigurationRequestDto.CreateValid(_fixture) with
        {
            WebhookConfigurationId = webhookConfigurationEntity.WebhookConfigurationId,
            OrganizationId = OrganizationId
        };

        // Act
        var response = await _httpClient.PatchAsJsonAsync(
            $"/organizations/my/webhook-configurations/{webhookConfigurationEntity.WebhookConfigurationId}", updateRequest.Body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
        var updatedWebhookConfigurationEntity = context.WebhookConfigurations.FirstOrDefault(x => x.WebhookConfigurationId == webhookConfigurationEntity.WebhookConfigurationId);
        updatedWebhookConfigurationEntity.Should().NotBeNull();
        updatedWebhookConfigurationEntity!.EndpointUrl.Should().Be(updateRequest.Body.EndpointUrl);
    }

    [Fact]
    public async Task DeleteMyOrganizationWebhook_GivenManager_ShouldReturnNoContent()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var webhookConfigurationEntity = FakeWebhookConfigurationEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            IsDeleted = false
        };

        await DatabaseHelper.CreateWebhookConfigurations(testFixture.Services, [webhookConfigurationEntity]);

        // Act
        var response = await _httpClient.DeleteAsync($"/organizations/my/webhook-configurations/{webhookConfigurationEntity.WebhookConfigurationId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
        var deletedWebhookConfigurationEntity = context.WebhookConfigurations.FirstOrDefault(x => x.WebhookConfigurationId == webhookConfigurationEntity.WebhookConfigurationId);
        deletedWebhookConfigurationEntity.Should().NotBeNull();
        deletedWebhookConfigurationEntity!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task RecoverMyOrganizationWebhook_GivenManager_ShouldReturnNoContent()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var webhookConfigurationEntity = FakeWebhookConfigurationEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            IsDeleted = true
        };

        await DatabaseHelper.CreateWebhookConfigurations(testFixture.Services, [webhookConfigurationEntity]);

        // Act
        var response = await _httpClient.PostAsync(
            $"/organizations/my/webhook-configurations/{webhookConfigurationEntity.WebhookConfigurationId}/recover", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
        var recoveredWebhookConfigurationEntity = context.WebhookConfigurations.FirstOrDefault(x => x.WebhookConfigurationId == webhookConfigurationEntity.WebhookConfigurationId);
        recoveredWebhookConfigurationEntity.Should().NotBeNull();
        recoveredWebhookConfigurationEntity!.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task GetMyOrganizationWebhook_GivenAnotherOrganizationsWebhook_ShouldReturnNotFound()
    {
        // Arrange
        var webhookConfigurationEntity = await ArrangeManagerWithAnotherOrganizationsWebhookAsync();

        // Act
        var response = await _httpClient.GetAsync($"/organizations/my/webhook-configurations/{webhookConfigurationEntity.WebhookConfigurationId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task UpdateMyOrganizationWebhook_GivenAnotherOrganizationsWebhook_ShouldReturnNotFound()
    {
        // Arrange
        var webhookConfigurationEntity = await ArrangeManagerWithAnotherOrganizationsWebhookAsync();

        var updateRequest = FakeUpdateWebhookConfigurationRequestDto.CreateValid(_fixture) with
        {
            WebhookConfigurationId = webhookConfigurationEntity.WebhookConfigurationId,
            OrganizationId = DisabledOrganizationId
        };

        // Act
        var response = await _httpClient.PatchAsJsonAsync(
            $"/organizations/my/webhook-configurations/{webhookConfigurationEntity.WebhookConfigurationId}", updateRequest.Body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
        var untouchedEntity = context.WebhookConfigurations.FirstOrDefault(x => x.WebhookConfigurationId == webhookConfigurationEntity.WebhookConfigurationId);
        untouchedEntity.Should().NotBeNull();
        untouchedEntity!.EndpointUrl.Should().Be(webhookConfigurationEntity.EndpointUrl);
    }

    [Fact]
    public async Task DeleteMyOrganizationWebhook_GivenAnotherOrganizationsWebhook_ShouldReturnNotFound()
    {
        // Arrange
        var webhookConfigurationEntity = await ArrangeManagerWithAnotherOrganizationsWebhookAsync();

        // Act
        var response = await _httpClient.DeleteAsync($"/organizations/my/webhook-configurations/{webhookConfigurationEntity.WebhookConfigurationId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
        var untouchedEntity = context.WebhookConfigurations.FirstOrDefault(x => x.WebhookConfigurationId == webhookConfigurationEntity.WebhookConfigurationId);
        untouchedEntity.Should().NotBeNull();
        untouchedEntity!.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task RecoverMyOrganizationWebhook_GivenAnotherOrganizationsWebhook_ShouldReturnNotFound()
    {
        // Arrange
        var webhookConfigurationEntity = await ArrangeManagerWithAnotherOrganizationsWebhookAsync(isDeleted: true);

        // Act
        var response = await _httpClient.PostAsync(
            $"/organizations/my/webhook-configurations/{webhookConfigurationEntity.WebhookConfigurationId}/recover", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
        var untouchedEntity = context.WebhookConfigurations.FirstOrDefault(x => x.WebhookConfigurationId == webhookConfigurationEntity.WebhookConfigurationId);
        untouchedEntity.Should().NotBeNull();
        untouchedEntity!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task TestMyOrganizationWebhook_GivenAnotherOrganizationsWebhook_ShouldReturnNotFound()
    {
        // Arrange
        var webhookConfigurationEntity = await ArrangeManagerWithAnotherOrganizationsWebhookAsync();

        // Act
        var response = await _httpClient.PostAsync(
            $"/organizations/my/webhook-configurations/{webhookConfigurationEntity.WebhookConfigurationId}/test", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RotateMyOrganizationWebhookSecret_GivenAnotherOrganizationsWebhook_ShouldReturnNotFoundAndKeepSecret()
    {
        // Arrange
        var webhookConfigurationEntity = await ArrangeManagerWithAnotherOrganizationsWebhookAsync();

        // Act
        var response = await _httpClient.PostAsync(
            $"/organizations/my/webhook-configurations/{webhookConfigurationEntity.WebhookConfigurationId}/rotate-secret", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotContain(webhookConfigurationEntity.SecretKey);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
        var untouchedEntity = context.WebhookConfigurations.FirstOrDefault(x => x.WebhookConfigurationId == webhookConfigurationEntity.WebhookConfigurationId);
        untouchedEntity.Should().NotBeNull();
        untouchedEntity!.SecretKey.Should().Be(webhookConfigurationEntity.SecretKey);
    }

    private async Task<WebhookConfigurationEntity> ArrangeManagerWithAnotherOrganizationsWebhookAsync(bool isDeleted = false)
    {
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        var webhookConfigurationEntity = FakeWebhookConfigurationEntity.CreateValid(_fixture) with
        {
            OrganizationId = DisabledOrganizationId,
            Organization = null!,
            SecretKey = "whsec_someoneelses",
            IsDeleted = isDeleted
        };

        await DatabaseHelper.CreateWebhookConfigurations(testFixture.Services, [webhookConfigurationEntity]);

        return webhookConfigurationEntity;
    }

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
                OrganizationId = DisabledOrganizationId,
                StatusId = (int)OrganizationStatus.Disabled,
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
