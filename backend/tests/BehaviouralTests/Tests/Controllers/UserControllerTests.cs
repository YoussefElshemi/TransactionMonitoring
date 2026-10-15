using System.Net;
using System.Net.Http.Json;
using AutoFixture;
using BehaviouralTests.TestHelpers;
using Core.Enums;
using Core.Helpers;
using FluentAssertions;
using Infrastructure;
using Infrastructure.Configurations;
using Infrastructure.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Constants;
using Presentation.Models.Common;
using Presentation.Models.Users;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

namespace BehaviouralTests.Tests.Controllers;

public class UserControllerTests(TestFixture testFixture) : IClassFixture<TestFixture>, IAsyncLifetime
{
    private readonly Fixture _fixture = AutoFixtureHelper.Create();
    private readonly HttpClient _httpClient = testFixture.CreateClient();

    private const string Email = "username@mail.com";
    private const string Password = "P@$$w0rd";
    private static readonly Guid OrganizationId = new("4d6389e9-f5d1-4325-a004-915865e3adec");
    private static readonly Guid DisabledOrganizationId = new("03a9b48e-4af2-40bd-a180-cac58bd930f3");

    [Fact]
    public async Task ListUser_NotAuthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _httpClient.GetAsync("/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task ListUser_NotAdmin_ShouldReturnForbidden()
    {
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.GetAsync("/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task ListUser_GivenInvalidRequest_ShouldReturnBadRequest()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Admin], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        await DatabaseHelper.CreateApplicationUsers(testFixture.Services, Enumerable.Range(0, 50).Select(
            _ =>
            {
                var applicationUserEntity = FakeApplicationUserEntity.CreateValid(_fixture);
                applicationUserEntity.IsDeleted = false;

                return applicationUserEntity;
            })
        );

        // Act
        var response = await _httpClient.GetAsync("/users?PageSize=200");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task ListUser_GivenValidRequest_ShouldReturnOk()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Admin], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        await DatabaseHelper.CreateApplicationUsers(testFixture.Services, Enumerable.Range(0, 49).Select(
            _ =>
            {
                var applicationUserEntity = FakeApplicationUserEntity.CreateValid(_fixture);
                applicationUserEntity.IsDeleted = false;

                return applicationUserEntity;
            })
        );

        // Act
        var response = await _httpClient.GetAsync("/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);

        var users = await response.Content.ReadFromJsonAsync<PagedResponseDto<UserResourceDto>>();
        users.Should().NotBeNull();
        users!.Metadata.Should().NotBeNull();
        users.Metadata.PageCount.Should().Be(5);
        users.Metadata.PageNumber.Should().Be(1);
        users.Metadata.PageSize.Should().Be(10);
        users.Metadata.TotalCount.Should().Be(50);
        users.Metadata.HasMore.Should().BeTrue();
    }

    [Fact]
    public async Task GetUser_NotAuthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _httpClient.GetAsync($"/users/{_fixture.Create<Guid>()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task GetUser_NotAdmin_ShouldReturnForbidden()
    {
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.GetAsync($"/users/{_fixture.Create<Guid>()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task GetUser_GivenValidRequest_ShouldReturnOk()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Admin], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        var applicationUserEntity = FakeApplicationUserEntity.CreateValid(_fixture);
        applicationUserEntity.IsDeleted = false;

        await DatabaseHelper.CreateApplicationUsers(testFixture.Services, [applicationUserEntity]);

        // Act
        var response = await _httpClient.GetAsync($"/users/{applicationUserEntity.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);

        var user = await response.Content.ReadDataFromJsonAsync<UserResourceDto>();
        user.Should().NotBeNull();
        user!.User.Email.Should().Be(applicationUserEntity.Email);
        user.User.UserName.Should().Be(applicationUserEntity.UserName);
        user.User.UserId.Should().Be(applicationUserEntity.Id);
    }

    [Fact]
    public async Task DeleteUser_NotAuthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _httpClient.DeleteAsync($"/users/{_fixture.Create<Guid>()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task DeleteUser_NotAdmin_ShouldReturnForbidden()
    {
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.DeleteAsync($"/users/{_fixture.Create<Guid>()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task DeleteUser_GivenValidRequest_ShouldReturnNoContent()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Admin], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        var applicationUserEntity = FakeApplicationUserEntity.CreateValid(_fixture);
        applicationUserEntity.IsDeleted = false;

        await DatabaseHelper.CreateApplicationUsers(testFixture.Services, [applicationUserEntity]);

        // Act
        var response = await _httpClient.DeleteAsync($"/users/{applicationUserEntity.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
        var updatedApplicationUserEntity = context.Users.FirstOrDefault(x => x.Id == applicationUserEntity.Id);
        updatedApplicationUserEntity.Should().NotBeNull();
        updatedApplicationUserEntity!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task RecoverUser_NotAuthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _httpClient.PostAsync($"/users/{_fixture.Create<Guid>()}/recover", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task RecoverUser_NotAdmin_ShouldReturnForbidden()
    {
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.PostAsync($"/users/{_fixture.Create<Guid>()}/recover", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task RecoverUser_GivenValidRequest_ShouldReturnNoContent()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Admin], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        var applicationUserEntity = FakeApplicationUserEntity.CreateValid(_fixture);
        applicationUserEntity.IsDeleted = false;

        await DatabaseHelper.CreateApplicationUsers(testFixture.Services, [applicationUserEntity]);

        // Act
        var response = await _httpClient.PostAsync($"/users/{applicationUserEntity.Id}/recover", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
        var updatedApplicationUserEntity = context.Users.FirstOrDefault(x => x.Id == applicationUserEntity.Id);
        updatedApplicationUserEntity.Should().NotBeNull();
        updatedApplicationUserEntity!.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task AddRole_NotAuthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _httpClient.PutAsync($"/users/{_fixture.Create<Guid>()}/roles/{_fixture.Create<string>()}", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task AddRole_NotAdmin_ShouldReturnForbidden()
    {
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.PutAsync($"/users/{_fixture.Create<Guid>()}/roles/{_fixture.Create<string>()}", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task AddRole_GivenValidRequest_ShouldReturnNoContent()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Admin], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        var applicationUserEntity = FakeApplicationUserEntity.CreateValid(_fixture);
        applicationUserEntity.IsDeleted = false;

        await DatabaseHelper.CreateApplicationUsers(testFixture.Services, [applicationUserEntity]);

        // Act
        var response = await _httpClient.PutAsync($"/users/{applicationUserEntity.Id}/roles/{UserRole.Manager}", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
        var updatedApplicationUserEntity = context.Users.FirstOrDefault(x => x.Id == applicationUserEntity.Id);
        updatedApplicationUserEntity.Should().NotBeNull();
        updatedApplicationUserEntity!.Roles.Should().Contain(role => role.RoleId == RoleHelper.MapFromUserRole(UserRole.Manager));
    }

    [Fact]
    public async Task RemoveRole_NotAuthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _httpClient.DeleteAsync($"/users/{_fixture.Create<Guid>()}/roles/{_fixture.Create<string>()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task RemoveRole_NotAdmin_ShouldReturnForbidden()
    {
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.DeleteAsync($"/users/{_fixture.Create<Guid>()}/roles/{_fixture.Create<string>()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task RemoveRole_GivenValidRequest_ShouldReturnNoContent()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Admin], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var applicationUserEntity = FakeApplicationUserEntity.CreateValid(_fixture);
        applicationUserEntity.IsDeleted = false;
        applicationUserEntity.Roles.Add(new IdentityUserRole<Guid>
        {
            UserId = applicationUserEntity.Id,
            RoleId = RoleHelper.MapFromUserRole(UserRole.Manager)
        });

        await DatabaseHelper.CreateApplicationUsers(testFixture.Services, [applicationUserEntity]);

        // Act
        var response = await _httpClient.DeleteAsync($"/users/{applicationUserEntity.Id}/roles/{UserRole.Manager}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
        var updatedApplicationUserEntity = context.Users.FirstOrDefault(x => x.Id == applicationUserEntity.Id);
        updatedApplicationUserEntity.Should().NotBeNull();
        updatedApplicationUserEntity!.Roles.Should().NotContain(role => role.RoleId == RoleHelper.MapFromUserRole(UserRole.Manager));
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
