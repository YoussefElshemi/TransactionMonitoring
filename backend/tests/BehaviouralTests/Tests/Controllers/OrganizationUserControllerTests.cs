using System.Net;
using System.Net.Http.Json;
using AutoFixture;
using BehaviouralTests.TestHelpers;
using Core.Constants;
using Core.Enums;
using Core.Helpers;
using Core.ValueObjects;
using FluentAssertions;
using Infrastructure;
using Infrastructure.Configurations;
using Infrastructure.Entities;
using Infrastructure.Mappers;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Constants;
using Presentation.Models.Common;
using Presentation.Models.Users;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Users;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.OrganizationUsers;

namespace BehaviouralTests.Tests.Controllers;

public class OrganizationUserControllerTests(TestFixture testFixture) : IClassFixture<TestFixture>, IAsyncLifetime
{
    private readonly Fixture _fixture = AutoFixtureHelper.Create();
    private readonly HttpClient _httpClient = testFixture.CreateClient();

    private const string Email = "username@mail.com";
    private const string Password = "P@$$w0rd";
    private static readonly Guid OrganizationId = new("4d6389e9-f5d1-4325-a004-915865e3adec");
    private static readonly Guid DisabledOrganizationId = new("03a9b48e-4af2-40bd-a180-cac58bd930f3");

    [Fact]
    public async Task ListOrganizationUser_NotAuthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _httpClient.GetAsync($"/organizations/{_fixture.Create<Guid>()}/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task ListOrganizationUser_NotAdmin_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        await DatabaseHelper.CreateApplicationUsers(testFixture.Services, Enumerable.Range(0, 49).Select(
            _ => (FakeApplicationUser.CreateValid(_fixture) with
            {
                OrganizationId = new OrganizationId(OrganizationId),
                IsDeleted = new IsDeleted(false)
            }).ToEntity())
        );

        // Act
        var response = await _httpClient.GetAsync($"/organizations/{OrganizationId}/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task ListOrganizationUser_GivenInvalidRequest_ShouldReturnBadRequest()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Admin], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        await DatabaseHelper.CreateApplicationUsers(testFixture.Services, Enumerable.Range(0, 50).Select(
            _ => (FakeApplicationUser.CreateValid(_fixture) with
            {
                OrganizationId = new OrganizationId(OrganizationId),
                IsDeleted = new IsDeleted(false)
            }).ToEntity())
        );

        // Act
        var response = await _httpClient.GetAsync($"/organizations/{OrganizationId}/users?PageSize=200");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task ListOrganizationUser_GivenValidRequest_ShouldReturnOk()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Admin], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        await DatabaseHelper.CreateApplicationUsers(testFixture.Services, Enumerable.Range(0, 49).Select(
            _ => (FakeApplicationUser.CreateValid(_fixture) with
            {
                OrganizationId = new OrganizationId(OrganizationId),
                IsDeleted = new IsDeleted(false)
            }).ToEntity())
        );

        // Act
        var response = await _httpClient.GetAsync($"/organizations/{OrganizationId}/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);

        var applicationUsers = await response.Content.ReadFromJsonAsync<PagedResponseDto<UserResourceDto>>();
        applicationUsers.Should().NotBeNull();
        applicationUsers!.Metadata.Should().NotBeNull();
        applicationUsers.Metadata.PageCount.Should().Be(5);
        applicationUsers.Metadata.PageNumber.Should().Be(1);
        applicationUsers.Metadata.PageSize.Should().Be(10);
        applicationUsers.Metadata.TotalCount.Should().Be(50);
        applicationUsers.Metadata.HasMore.Should().BeTrue();
    }

    [Fact]
    public async Task ListOrganizationUser_Manager_ShouldReadTheirOwnAndNoOther()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var own = await _httpClient.GetAsync($"/organizations/{OrganizationId}/users");
        var other = await _httpClient.GetAsync($"/organizations/{Guid.NewGuid()}/users");

        // Assert
        own.StatusCode.Should().Be(HttpStatusCode.OK);
        other.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        other.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task GetOrganizationUserHistory_Manager_ShouldReadTheirOwnAndNoOther()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var own = await _httpClient.GetAsync($"/organizations/{OrganizationId}/users/{_fixture.Create<Guid>()}/history");
        var other = await _httpClient.GetAsync($"/organizations/{Guid.NewGuid()}/users/{_fixture.Create<Guid>()}/history");

        // Assert
        own.StatusCode.Should().Be(HttpStatusCode.OK);
        other.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        other.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task ListMyOrganizationUser_NotAuthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _httpClient.GetAsync("/organizations/my/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task ListMyOrganizationUser_NotManagerOrAdmin_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.GetAsync("/organizations/my/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task ListMyOrganizationUser_GivenInvalidRequest_ShouldReturnBadRequest()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.GetAsync("/organizations/my/users?PageSize=200");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task ListMyOrganizationUser_GivenManager_ShouldOnlyReturnOwnOrganizationUsers()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        await DatabaseHelper.CreateApplicationUsers(testFixture.Services, Enumerable.Range(0, 9).Select(
            _ => (FakeApplicationUser.CreateValid(_fixture) with
            {
                OrganizationId = new OrganizationId(OrganizationId),
                IsDeleted = new IsDeleted(false)
            }).ToEntity())
        );

        await DatabaseHelper.CreateApplicationUsers(testFixture.Services, [
            (FakeApplicationUser.CreateValid(_fixture) with
            {
                OrganizationId = new OrganizationId(OrganizationId),
                IsDeleted = new IsDeleted(true)
            }).ToEntity(),
            (FakeApplicationUser.CreateValid(_fixture) with
            {
                OrganizationId = new OrganizationId(SystemConstants.DefaultOrganization),
                IsDeleted = new IsDeleted(false)
            }).ToEntity()
        ]);

        // Act
        var response = await _httpClient.GetAsync("/organizations/my/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);

        var applicationUsers = await response.Content.ReadFromJsonAsync<PagedResponseDto<UserResourceDto>>();
        applicationUsers.Should().NotBeNull();
        applicationUsers!.Metadata.Should().NotBeNull();
        applicationUsers.Metadata.PageCount.Should().Be(1);
        applicationUsers.Metadata.PageNumber.Should().Be(1);
        applicationUsers.Metadata.PageSize.Should().Be(10);
        applicationUsers.Metadata.TotalCount.Should().Be(10);
        applicationUsers.Metadata.HasMore.Should().BeFalse();
        applicationUsers.Data.Should().OnlyContain(x => x.User.OrganizationId == OrganizationId);
    }

    [Fact]
    public async Task GetOrganizationUser_NotAuthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _httpClient.GetAsync($"/organizations/{_fixture.Create<Guid>()}/users/{_fixture.Create<Guid>()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task GetOrganizationUser_NotAdmin_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        var applicationUserEntity = (FakeApplicationUser.CreateValid(_fixture) with
        {
            OrganizationId = new OrganizationId(OrganizationId),
            IsDeleted = new IsDeleted(false)
        }).ToEntity();

        await DatabaseHelper.CreateApplicationUsers(testFixture.Services, [applicationUserEntity]);

        // Act
        var response = await _httpClient.GetAsync($"/organizations/{OrganizationId}/users/{applicationUserEntity.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task GetOrganizationUser_GivenValidRequest_ShouldReturnOk()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Admin], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        var applicationUserEntity = (FakeApplicationUser.CreateValid(_fixture) with
        {
            OrganizationId = new OrganizationId(OrganizationId),
            IsDeleted = new IsDeleted(false)
        }).ToEntity();

        await DatabaseHelper.CreateApplicationUsers(testFixture.Services, [applicationUserEntity]);

        // Act
        var response = await _httpClient.GetAsync($"/organizations/{OrganizationId}/users/{applicationUserEntity.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);

        var applicationUser = await response.Content.ReadDataFromJsonAsync<UserResourceDto>();
        applicationUser.Should().NotBeNull();
        applicationUser!.User.UserId.Should().Be(applicationUserEntity.Id);
    }

    [Fact]
    public async Task PutOrganizationUser_NotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var updateRequest = FakeUpdateOrganizationUserRequestDto.CreateValid(_fixture);

        // Act
        var response = await _httpClient.PutAsync($"/organizations/{_fixture.Create<Guid>()}/users/{updateRequest.UserId}", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task PutOrganizationUser_NotAuthenticated_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var applicationUserEntity = (FakeApplicationUser.CreateValid(_fixture) with
        {
            OrganizationId = new OrganizationId(SystemConstants.DefaultOrganization),
            IsDeleted = new IsDeleted(false)
        }).ToEntity();

        await DatabaseHelper.CreateApplicationUsers(testFixture.Services, [applicationUserEntity]);

        var updateRequest = FakeUpdateOrganizationUserRequestDto.CreateValid(_fixture) with
        {
            UserId = applicationUserEntity.Id,
            OrganizationId = OrganizationId
        };

        // Act
        var response = await _httpClient.PutAsync($"/organizations/{OrganizationId}/users/{updateRequest.UserId}", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task PutOrganizationUser_GivenInvalidRequest_ShouldReturnNotFound()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Admin], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var applicationUserEntity = (FakeApplicationUser.CreateValid(_fixture) with
        {
            OrganizationId = new OrganizationId(SystemConstants.DefaultOrganization),
            IsDeleted = new IsDeleted(false)
        }).ToEntity();

        await DatabaseHelper.CreateApplicationUsers(testFixture.Services, [applicationUserEntity]);

        var updateRequest = FakeUpdateOrganizationUserRequestDto.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId
        };

        // Act
        var response = await _httpClient.PutAsync($"/organizations/{OrganizationId}/users/{updateRequest.UserId}", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task PutOrganizationUser_GivenValidRequest_ShouldReturnNoContent()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Admin], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var applicationUserEntity = (FakeApplicationUser.CreateValid(_fixture) with
        {
            OrganizationId = new OrganizationId(SystemConstants.DefaultOrganization),
            IsDeleted = new IsDeleted(false)
        }).ToEntity();

        await DatabaseHelper.CreateApplicationUsers(testFixture.Services, [applicationUserEntity]);

        var updateRequest = FakeUpdateOrganizationUserRequestDto.CreateValid(_fixture) with
        {
            UserId = applicationUserEntity.Id,
            OrganizationId = OrganizationId
        };

        // Act
        var response = await _httpClient.PutAsync($"/organizations/{OrganizationId}/users/{updateRequest.UserId}", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
        var updatedUserEntity = context.Users.FirstOrDefault(x => x.Id == updateRequest.UserId);
        updatedUserEntity.Should().NotBeNull();
        updatedUserEntity!.OrganizationId.Should().Be(updateRequest.OrganizationId);
    }

    [Fact]
    public async Task PutOrganizationUser_GivenAManager_ShouldStripTheManagerRole()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Admin], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        var manager = await AuthenticationHelper.CreateUserAsync(
            testFixture.Services, "manager@mail.com", Password, [UserRole.User, UserRole.Manager], SystemConstants.DefaultOrganization);

        // Act
        var response = await _httpClient.PutAsync($"/organizations/{OrganizationId}/users/{manager.Id}", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var roleIds = context.UserRoles.Where(x => x.UserId == manager.Id).Select(x => x.RoleId).ToList();
        roleIds.Should().NotContain(RoleHelper.MapFromUserRole(UserRole.Manager));
        roleIds.Should().Contain(RoleHelper.MapFromUserRole(UserRole.User));
    }

    [Fact]
    public async Task RemoveOrganizationUser_NotAuthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _httpClient.DeleteAsync($"/organizations/{_fixture.Create<Guid>()}/users/{_fixture.Create<Guid>()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task RemoveOrganizationUser_NotAdmin_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var applicationUserEntity = (FakeApplicationUser.CreateValid(_fixture) with
        {
            OrganizationId = new OrganizationId(OrganizationId),
            IsDeleted = new IsDeleted(false)
        }).ToEntity();

        await DatabaseHelper.CreateApplicationUsers(testFixture.Services, [applicationUserEntity]);

        // Act
        var response = await _httpClient.DeleteAsync($"/organizations/{OrganizationId}/users/{applicationUserEntity.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task RemoveOrganizationUser_GivenValidRequest_ShouldReturnNoContent()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Admin], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var applicationUserEntity = (FakeApplicationUser.CreateValid(_fixture) with
        {
            OrganizationId = new OrganizationId(OrganizationId),
            IsDeleted = new IsDeleted(false)
        }).ToEntity();

        await DatabaseHelper.CreateApplicationUsers(testFixture.Services, [applicationUserEntity]);

        // Act
        var response = await _httpClient.DeleteAsync($"/organizations/{OrganizationId}/users/{applicationUserEntity.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
        var updatedUserEntity = context.Users.FirstOrDefault(x => x.Id == applicationUserEntity.Id);
        updatedUserEntity.Should().NotBeNull();
        updatedUserEntity!.OrganizationId.Should().Be(SystemConstants.DefaultOrganization);
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
