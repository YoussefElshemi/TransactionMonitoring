using System.Net;
using AutoFixture;
using BehaviouralTests.TestHelpers;
using Core.Constants;
using Core.Enums;
using FluentAssertions;
using Infrastructure.Configurations;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

namespace BehaviouralTests.Tests.Controllers;

public class PlatformAdministratorWithoutOrganizationTests(TestFixture testFixture) : IClassFixture<TestFixture>, IAsyncLifetime
{
    private readonly Fixture _fixture = AutoFixtureHelper.Create();

    private const string Password = "P@$$w0rd";
    private static readonly Guid OrganizationId = new("2a6f4c19-7d85-4e03-9b61-c8f5a3d72e40");

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
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Administrator_WithoutOrganization_CanReadThePermissionCatalogue()
    {
        // Arrange
        var httpClient = await SignInAsync("platform-catalogue@mail.com", UserRole.Admin, SystemConstants.DefaultOrganization);

        // Act
        var response = await httpClient.GetAsync("/permissions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Administrator_WithoutOrganization_CanReadTheirOwnGrants()
    {
        // Arrange
        var httpClient = await SignInAsync("platform-mine@mail.com", UserRole.Admin, SystemConstants.DefaultOrganization);

        // Act
        var response = await httpClient.GetAsync("/permissions/mine");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Administrator_WithoutOrganization_CanReachATenantNamedInTheRoute()
    {
        // Arrange
        var httpClient = await SignInAsync("platform-roles@mail.com", UserRole.Admin, SystemConstants.DefaultOrganization);

        // Act
        var response = await httpClient.GetAsync($"/organizations/{OrganizationId}/roles");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("/cases")]
    [InlineData("/invoices")]
    [InlineData("/api-clients")]
    public async Task Administrator_WithoutOrganization_IsRefusedTenantScopedEndpoints(string path)
    {
        // Arrange
        var httpClient = await SignInAsync($"platform-tenant{path.Replace("/", "-")}@mail.com", UserRole.Admin, SystemConstants.DefaultOrganization);

        // Act
        var response = await httpClient.GetAsync(path);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Supervisor_WithoutOrganization_IsStillRefusedTheCatalogue()
    {
        // Arrange
        var httpClient = await SignInAsync("uninvited-catalogue@mail.com", UserRole.Manager, SystemConstants.DefaultOrganization);

        // Act
        var response = await httpClient.GetAsync("/permissions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Administrator_WithAnOrganization_IsUnaffected()
    {
        // Arrange
        var httpClient = await SignInAsync("tenanted-admin@mail.com", UserRole.Admin, OrganizationId);

        // Act
        var response = await httpClient.GetAsync("/permissions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task<HttpClient> SignInAsync(string email, UserRole role, Guid organizationId)
    {
        var httpClient = testFixture.CreateClient();

        await AuthenticationHelper.CreateUserAsync(testFixture.Services, email, Password, [role], organizationId);
        await AuthenticationHelper.SetTokenAsync(httpClient, email, Password);

        return httpClient;
    }
}
