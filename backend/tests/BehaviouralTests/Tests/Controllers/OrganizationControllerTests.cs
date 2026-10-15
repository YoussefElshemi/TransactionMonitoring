using System.Net;
using System.Net.Http.Json;
using System.Text;
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
using Presentation.Models.Organizations;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.Organizations;

namespace BehaviouralTests.Tests.Controllers;

public class OrganizationControllerTests(TestFixture testFixture) : IClassFixture<TestFixture>, IAsyncLifetime
{
    private readonly Fixture _fixture = AutoFixtureHelper.Create();
    private readonly HttpClient _httpClient = testFixture.CreateClient();

    private const string Email = "username@mail.com";
    private const string Password = "P@$$w0rd";
    private static readonly Guid OrganizationId = new("4d6389e9-f5d1-4325-a004-915865e3adec");
    private static readonly Guid DisabledOrganizationId = new("03a9b48e-4af2-40bd-a180-cac58bd930f3");

    [Fact]
    public async Task CreateOrganization_NotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var createRequest = FakeCreateOrganizationRequestDto.CreateValid(_fixture);

        // Act
        var response = await _httpClient.PostAsJsonAsync("/organizations", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task CreateOrganization_NotAdmin_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var createRequest = FakeCreateOrganizationRequestDto.CreateValid(_fixture);

        // Act
        var response = await _httpClient.PostAsJsonAsync("/organizations", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task CreateOrganization_GivenInvalidRequest_ShouldReturnBadRequest()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Admin], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var createRequest = FakeCreateOrganizationRequestDto.CreateInvalid(_fixture);

        // Act
        var response = await _httpClient.PostAsJsonAsync("/organizations", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task CreateOrganization_GivenValidRequest_ShouldReturnCreated()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Admin], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var createRequest = FakeCreateOrganizationRequestDto.CreateValid(_fixture);

        // Act
        var response = await _httpClient.PostAsJsonAsync("/organizations", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);

        var createdOrganization = await response.Content.ReadDataFromJsonAsync<OrganizationResponseDto>();
        createdOrganization.Should().NotBeNull();
        createdOrganization!.FriendlyName.Should().Be(createRequest.FriendlyName);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
        var organizationEntity = context.Organizations.FirstOrDefault(x => x.OrganizationId == createdOrganization.OrganizationId);
        organizationEntity.Should().NotBeNull();
    }

    [Fact]
    public async Task ListOrganization_NotAuthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _httpClient.GetAsync("/organizations");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task ListOrganization_NotAdmin_ShouldReturnForbidden()
    {
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.GetAsync("/organizations");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task ListOrganization_GivenInvalidRequest_ShouldReturnBadRequest()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Admin], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        await DatabaseHelper.CreateOrganizations(testFixture.Services, Enumerable.Range(0, 50).Select(
            _ => FakeOrganizationEntity.CreateValid(_fixture) with
            {
                IsDeleted = false
            })
        );

        // Act
        var response = await _httpClient.GetAsync("/organizations?PageSize=200");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task ListOrganization_GivenValidRequest_ShouldReturnOk()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Admin], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        await DatabaseHelper.CreateOrganizations(testFixture.Services, Enumerable.Range(0, 47).Select(
            _ => FakeOrganizationEntity.CreateValid(_fixture) with
            {
                IsDeleted = false
            })
        );

        // Act
        var response = await _httpClient.GetAsync("/organizations");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);

        var organizations = await response.Content.ReadFromJsonAsync<PagedResponseDto<OrganizationResponseDto>>();
        organizations.Should().NotBeNull();
        organizations!.Metadata.Should().NotBeNull();
        organizations.Metadata.PageCount.Should().Be(5);
        organizations.Metadata.PageNumber.Should().Be(1);
        organizations.Metadata.PageSize.Should().Be(10);
        organizations.Metadata.TotalCount.Should().Be(50);
        organizations.Metadata.HasMore.Should().BeTrue();
    }

    [Fact]
    public async Task GetOrganization_NotAuthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _httpClient.GetAsync($"/organizations/{_fixture.Create<Guid>()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task GetOrganization_NotAdmin_ShouldReturnForbidden()
    {
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.GetAsync($"/organizations/{_fixture.Create<Guid>()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task GetOrganization_GivenValidRequest_ShouldReturnOk()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Admin], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        var organizationEntity = FakeOrganizationEntity.CreateValid(_fixture) with
        {
            IsDeleted = false
        };

        await DatabaseHelper.CreateOrganizations(testFixture.Services, [organizationEntity]);

        // Act
        var response = await _httpClient.GetAsync($"/organizations/{organizationEntity.OrganizationId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);

        var organization = await response.Content.ReadDataFromJsonAsync<OrganizationResponseDto>();
        organization.Should().NotBeNull();
        organization!.OrganizationId.Should().Be(organizationEntity.OrganizationId);
        organization.FriendlyName.Should().Be(organizationEntity.FriendlyName);
    }

    [Fact]
    public async Task UpdateOrganization_NotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var updateRequest = FakeUpdateOrganizationRequestDto.CreateValid(_fixture);

        // Act
        var response = await _httpClient.PatchAsJsonAsync($"/organizations/{updateRequest.OrganizationId}", updateRequest.Body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task UpdateOrganization_NotAdmin_ShouldReturnForbidden()
    {
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var updateRequest = FakeUpdateOrganizationRequestDto.CreateValid(_fixture);

        // Act
        var response = await _httpClient.PatchAsJsonAsync($"/organizations/{updateRequest.OrganizationId}", updateRequest.Body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task UpdateOrganization_GivenInvalidRequest_ShouldReturnBadRequest()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Admin], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var organizationEntity = FakeOrganizationEntity.CreateValid(_fixture) with
        {
            IsDeleted = false
        };

        await DatabaseHelper.CreateOrganizations(testFixture.Services, [organizationEntity]);

        var updateRequest = FakeUpdateOrganizationRequestDto.CreateInvalid(_fixture) with
        {
            OrganizationId = OrganizationId
        };

        // Act
        var response = await _httpClient.PatchAsJsonAsync($"/organizations/{updateRequest.OrganizationId}", updateRequest.Body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task UpdateOrganization_GivenValidRequest_ShouldReturnNoContent()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Admin], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var organizationEntity = FakeOrganizationEntity.CreateValid(_fixture) with
        {
            IsDeleted = false
        };

        await DatabaseHelper.CreateOrganizations(testFixture.Services, [organizationEntity]);

        var updateRequest = FakeUpdateOrganizationRequestDto.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId
        };

        // Act
        var response = await _httpClient.PatchAsJsonAsync($"/organizations/{updateRequest.OrganizationId}", updateRequest.Body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
        var updatedOrganizationEntity = context.Organizations.FirstOrDefault(x => x.OrganizationId == updateRequest.OrganizationId);
        updatedOrganizationEntity.Should().NotBeNull();
        updatedOrganizationEntity!.FriendlyName.Should().Be(updateRequest.Body.FriendlyName);
    }

    [Fact]
    public async Task DeleteOrganization_NotAuthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _httpClient.DeleteAsync($"/organizations/{_fixture.Create<Guid>()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task DeleteOrganization_NotAdmin_ShouldReturnForbidden()
    {
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.DeleteAsync($"/organizations/{_fixture.Create<Guid>()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task DeleteOrganization_GivenValidRequest_ShouldReturnNoContent()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Admin], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var organizationEntity = FakeOrganizationEntity.CreateValid(_fixture) with
        {
            IsDeleted = false
        };

        await DatabaseHelper.CreateOrganizations(testFixture.Services, [organizationEntity]);

        // Act
        var response = await _httpClient.DeleteAsync($"/organizations/{organizationEntity.OrganizationId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
        var updatedOrganizationEntity = context.Organizations.FirstOrDefault(x => x.OrganizationId == organizationEntity.OrganizationId);
        updatedOrganizationEntity.Should().NotBeNull();
        updatedOrganizationEntity!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task RecoverOrganization_NotAuthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _httpClient.PostAsync($"/organizations/{_fixture.Create<Guid>()}/recover", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task RecoverOrganization_NotAdmin_ShouldReturnForbidden()
    {
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.PostAsync($"/organizations/{_fixture.Create<Guid>()}/recover", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task RecoverOrganization_GivenValidRequest_ShouldReturnNoContent()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Admin], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var organizationEntity = FakeOrganizationEntity.CreateValid(_fixture) with
        {
            IsDeleted = true
        };

        await DatabaseHelper.CreateOrganizations(testFixture.Services, [organizationEntity]);

        // Act
        var response = await _httpClient.PostAsync($"/organizations/{organizationEntity.OrganizationId}/recover", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
        var updatedOrganizationEntity = context.Organizations.FirstOrDefault(x => x.OrganizationId == organizationEntity.OrganizationId);
        updatedOrganizationEntity.Should().NotBeNull();
        updatedOrganizationEntity!.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateMyOrganizationHoldPolicy_NotAuthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _httpClient.PatchAsync(
            "/organizations/my/hold-policy",
            new StringContent("""{"holdWindow":45}""", Encoding.UTF8, "application/json"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task UpdateMyOrganizationHoldPolicy_NotManagerOrAdmin_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.PatchAsync(
            "/organizations/my/hold-policy",
            new StringContent("""{"holdWindow":45}""", Encoding.UTF8, "application/json"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task UpdateMyOrganizationHoldPolicy_AsManager_ShouldUpdateTheirOwnOrganization()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.PatchAsync(
            "/organizations/my/hold-policy",
            new StringContent("""{"holdWindow":45,"holdExpiryVerdict":"Release"}""", Encoding.UTF8, "application/json"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
        var updatedOrganizationEntity = context.Organizations.First(x => x.OrganizationId == OrganizationId);
        updatedOrganizationEntity.HoldWindow.Should().Be(45);
        updatedOrganizationEntity.HoldExpiryVerdictId.Should().Be((int)HoldExpiryVerdict.Release);
    }

    [Fact]
    public async Task UpdateMyOrganizationHoldPolicy_GivenExplicitNulls_ShouldClearItBackToTheConfiguredDefault()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        await _httpClient.PatchAsync(
            "/organizations/my/hold-policy",
            new StringContent("""{"holdWindow":45,"holdExpiryVerdict":"Release"}""", Encoding.UTF8, "application/json"));

        // Act
        var response = await _httpClient.PatchAsync(
            "/organizations/my/hold-policy",
            new StringContent("""{"holdWindow":null,"holdExpiryVerdict":null}""", Encoding.UTF8, "application/json"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
        var updatedOrganizationEntity = context.Organizations.First(x => x.OrganizationId == OrganizationId);
        updatedOrganizationEntity.HoldWindow.Should().BeNull();
        updatedOrganizationEntity.HoldExpiryVerdictId.Should().BeNull();
    }

    [Fact]
    public async Task UpdateMyOrganizationHoldPolicy_OmittingTheHoldPolicy_ShouldLeaveItAlone()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        await _httpClient.PatchAsync(
            "/organizations/my/hold-policy",
            new StringContent("""{"holdWindow":45,"holdExpiryVerdict":"Release"}""", Encoding.UTF8, "application/json"));

        // Act
        var response = await _httpClient.PatchAsync(
            "/organizations/my/hold-policy",
            new StringContent("{}", Encoding.UTF8, "application/json"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
        var updatedOrganizationEntity = context.Organizations.First(x => x.OrganizationId == OrganizationId);
        updatedOrganizationEntity.HoldWindow.Should().Be(45);
        updatedOrganizationEntity.HoldExpiryVerdictId.Should().Be((int)HoldExpiryVerdict.Release);
    }

    [Fact]
    public async Task UpdateMyOrganizationHoldPolicy_GivenInvalidRequest_ShouldReturnBadRequest()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.PatchAsync(
            "/organizations/my/hold-policy",
            new StringContent("""{"holdWindow":0}""", Encoding.UTF8, "application/json"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task UpdateOrganization_AsManager_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var updateRequest = FakeUpdateOrganizationRequestDto.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId
        };

        // Act
        var response = await _httpClient.PatchAsJsonAsync($"/organizations/{updateRequest.OrganizationId}", updateRequest.Body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
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