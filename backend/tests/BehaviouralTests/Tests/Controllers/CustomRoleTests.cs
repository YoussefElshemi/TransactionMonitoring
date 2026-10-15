using System.Net;
using System.Net.Http.Json;
using AutoFixture;
using BehaviouralTests.TestHelpers;
using Core.Enums;
using Core.Helpers;
using FluentAssertions;
using Infrastructure.Configurations;
using Presentation.Models.Common;
using Presentation.Models.Roles;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

namespace BehaviouralTests.Tests.Controllers;

public class CustomRoleTests(TestFixture testFixture) : IClassFixture<TestFixture>, IAsyncLifetime
{
    private readonly Fixture _fixture = AutoFixtureHelper.Create();
    private readonly HttpClient _httpClient = testFixture.CreateClient();

    private const string SupervisorEmail = "role-supervisor@mail.com";
    private const string Password = "P@$$w0rd";
    private static readonly Guid OrganizationId = new("7a2e4c91-5b83-4d16-8f72-3c9e1a6d4b58");

    public async Task InitializeAsync()
    {
        await testFixture.ResetDatabaseAsync();

        await DatabaseHelper.CreateOrganizations(testFixture.Services,
        [
            OrganizationConfigurations.DefaultOrganization,
            FakeOrganizationEntity.CreateValid(_fixture) with
            {
                OrganizationId = OrganizationId,
                StatusId = (int)OrganizationStatus.Enabled,
                IsDeleted = false
            }
        ]);

        await AuthenticationHelper.CreateUserAsync(testFixture.Services, SupervisorEmail, Password, [UserRole.Manager], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, SupervisorEmail, Password);
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task CreateRole_GivenPermissionsTheComposerHolds_ReturnsCreated()
    {
        // Act
        var response = await CreateRoleAsync("Rule owner", "rules:read", "rules:write", "rules:deploy");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await response.Content.ReadFromJsonAsync<ResponseDataDto<RoleResponseDto>>();

        created.Should().NotBeNull();
        created!.Data.Name.Should().Be("Rule owner");
        created.Data.IsBuiltIn.Should().BeFalse();
        created.Data.OrganizationId.Should().Be(OrganizationId);
        created.Data.Permissions.Should().BeEquivalentTo("rules:read", "rules:write", "rules:deploy");
    }

    [Fact]
    public async Task CreateRole_GivenAPermissionTheComposerDoesNotHold_IsRefused()
    {
        // Arrange
        var response = await CreateRoleAsync("Everything", "cases:read", "platform:administer");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var problem = await response.Content.ReadAsStringAsync();

        problem.Should().Contain("platform:administer");
    }

    [Fact]
    public async Task CreateRole_GivenAnUnknownPermission_IsRefused()
    {
        // Act
        var response = await CreateRoleAsync("Nonsense", "cases:incinerate");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateRole_GivenNoPermissions_IsRefused()
    {
        // Act
        var response = await CreateRoleAsync("Empty");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateRole_GivenABuiltInName_IsRefused()
    {
        // Act
        var response = await CreateRoleAsync("Manager", "cases:read");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateRole_GivenADuplicateName_IsRefused()
    {
        // Arrange
        await CreateRoleAsync("Reviewer", "cases:read");

        // Act
        var response = await CreateRoleAsync("REVIEWER", "cases:read");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateRole_GivenABuiltInRole_IsRefused()
    {
        // Arrange
        var roles = await ListRolesAsync();
        var manager = roles.Single(x => x.Name == nameof(UserRole.Manager));

        // Act
        var response = await _httpClient.PatchAsJsonAsync(
            $"/organizations/{OrganizationId}/roles/{manager.RoleId}",
            new { Permissions = new[] { "cases:read" } });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task DeleteRole_GivenABuiltInRole_IsRefused()
    {
        // Arrange
        var roles = await ListRolesAsync();
        var auditor = roles.Single(x => x.Name == nameof(UserRole.Auditor));

        // Act
        var response = await _httpClient.DeleteAsync($"/organizations/{OrganizationId}/roles/{auditor.RoleId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task DeleteRole_WhileSomebodyIsOnIt_IsRefusedWithTheCount()
    {
        // Arrange
        var role = await CreateRoleAndReadAsync("Occupied", "cases:read");
        var analyst = await CreateAnalystAsync("occupied-analyst@mail.com");

        await AssignAsync(analyst, role.RoleId);

        // Act
        var response = await _httpClient.DeleteAsync($"/organizations/{OrganizationId}/roles/{role.RoleId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var problem = await response.Content.ReadAsStringAsync();

        problem.Should().Contain("1 assignment(s)");
    }

    [Fact]
    public async Task DeleteRole_AfterTheAssignmentIsRevoked_Succeeds()
    {
        // Arrange
        var role = await CreateRoleAndReadAsync("Vacated", "cases:read");
        var analyst = await CreateAnalystAsync("vacated-analyst@mail.com");

        var assignment = await AssignAsync(analyst, role.RoleId);

        await _httpClient.DeleteAsync($"/organizations/{OrganizationId}/role-assignments/{assignment.RoleAssignmentId}");

        // Act
        var response = await _httpClient.DeleteAsync($"/organizations/{OrganizationId}/roles/{role.RoleId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task AssignRole_GrantsTheRolesPermissionsImmediately()
    {
        // Arrange
        var role = await CreateRoleAndReadAsync("Reason code owner", "case-reason-codes:read", "case-reason-codes:write");
        var analyst = await CreateAnalystAsync("reason-code-owner@mail.com");
        var analystClient = await SignInAsync("reason-code-owner@mail.com");

        var before = await analystClient.GetAsync($"/organizations/{OrganizationId}/reason-codes");

        // Act
        await AssignAsync(analyst, role.RoleId);

        var after = await analystClient.GetAsync($"/organizations/{OrganizationId}/reason-codes");

        // Assert
        before.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        after.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RevokeRole_TakesThePermissionsBackImmediately()
    {
        // Arrange
        var role = await CreateRoleAndReadAsync("Temporary owner", "case-reason-codes:read");
        var analyst = await CreateAnalystAsync("temporary-owner@mail.com");
        var analystClient = await SignInAsync("temporary-owner@mail.com");

        var assignment = await AssignAsync(analyst, role.RoleId);

        var before = await analystClient.GetAsync($"/organizations/{OrganizationId}/reason-codes");

        // Act
        await _httpClient.DeleteAsync($"/organizations/{OrganizationId}/role-assignments/{assignment.RoleAssignmentId}");

        var after = await analystClient.GetAsync($"/organizations/{OrganizationId}/reason-codes");

        // Assert
        before.StatusCode.Should().Be(HttpStatusCode.OK);
        after.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AssignRole_Twice_ReturnsTheSameGrant()
    {
        // Arrange
        var role = await CreateRoleAndReadAsync("Idempotent", "cases:read");
        var analyst = await CreateAnalystAsync("idempotent-analyst@mail.com");

        // Act
        var first = await AssignAsync(analyst, role.RoleId);
        var second = await AssignAsync(analyst, role.RoleId);

        // Assert
        second.RoleAssignmentId.Should().Be(first.RoleAssignmentId);
    }

    [Fact]
    public async Task ListRoles_IncludesTheBuiltInRolesAndTheOrganizationsOwn()
    {
        // Arrange
        await CreateRoleAsync("Composed", "cases:read");

        // Act
        var roles = await ListRolesAsync();

        // Assert
        roles.Where(x => x.IsBuiltIn).Select(x => x.Name)
            .Should().BeEquivalentTo(Enum.GetNames<UserRole>());

        roles.Should().ContainSingle(x => !x.IsBuiltIn).Which.Name.Should().Be("Composed");
    }

    [Fact]
    public async Task AssignRole_GivenTheBuiltInAdminRole_IsRefused()
    {
        // Arrange
        var analystId = await CreateAnalystAsync("promoted-analyst@mail.com");

        // Act
        var response = await _httpClient.PostAsJsonAsync(
            $"/organizations/{OrganizationId}/users/{analystId}/role-assignments",
            new { RoleId = RoleHelper.MapFromUserRole(UserRole.Admin) });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var problem = await response.Content.ReadAsStringAsync();

        problem.Should().Contain("platform:administer");
    }

    [Fact]
    public async Task AssignRole_GivenABuiltInRoleTheAssignerHolds_Succeeds()
    {
        // Arrange
        var analystId = await CreateAnalystAsync("promoted-manager@mail.com");

        // Act
        var response = await _httpClient.PostAsJsonAsync(
            $"/organizations/{OrganizationId}/users/{analystId}/role-assignments",
            new { RoleId = RoleHelper.MapFromUserRole(UserRole.Manager) });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Analyst_CannotComposeARole()
    {
        // Arrange
        await CreateAnalystAsync("nosy-analyst@mail.com");
        var analystClient = await SignInAsync("nosy-analyst@mail.com");

        // Act
        var response = await analystClient.PostAsJsonAsync($"/organizations/{OrganizationId}/roles", new
        {
            Name = "Self promotion",
            Permissions = new[] { "cases:read" }
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private Task<HttpResponseMessage> CreateRoleAsync(string name, params string[] permissions)
    {
        return _httpClient.PostAsJsonAsync($"/organizations/{OrganizationId}/roles", new
        {
            Name = name,
            Permissions = permissions
        });
    }

    private async Task<RoleResponseDto> CreateRoleAndReadAsync(string name, params string[] permissions)
    {
        var response = await CreateRoleAsync(name, permissions);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await response.Content.ReadFromJsonAsync<ResponseDataDto<RoleResponseDto>>();

        return created!.Data;
    }

    private async Task<RoleResponseDto[]> ListRolesAsync()
    {
        var response = await _httpClient.GetAsync($"/organizations/{OrganizationId}/roles?PageSize=100");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var roles = await response.Content.ReadFromJsonAsync<PagedResponseDto<RoleResponseDto>>();

        return roles!.Data;
    }

    private async Task<RoleAssignmentResponseDto> AssignAsync(Guid userId, Guid roleId)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"/organizations/{OrganizationId}/users/{userId}/role-assignments", new { RoleId = roleId });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var assignment = await response.Content.ReadFromJsonAsync<ResponseDataDto<RoleAssignmentResponseDto>>();

        return assignment!.Data;
    }

    private async Task<Guid> CreateAnalystAsync(string email)
    {
        var user = await AuthenticationHelper.CreateUserAsync(testFixture.Services, email, Password, [UserRole.User], OrganizationId);

        return user.Id;
    }

    private async Task<HttpClient> SignInAsync(string email)
    {
        var httpClient = testFixture.CreateClient();

        await AuthenticationHelper.SetTokenAsync(httpClient, email, Password);

        return httpClient;
    }
}
