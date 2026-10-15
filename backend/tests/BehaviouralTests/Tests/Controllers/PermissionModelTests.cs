using System.Net;
using System.Net.Http.Json;
using AutoFixture;
using BehaviouralTests.TestHelpers;
using Core.Enums;
using FluentAssertions;
using Infrastructure.Configurations;
using Presentation.Models.Common;
using Presentation.Models.Permissions;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

namespace BehaviouralTests.Tests.Controllers;

public class PermissionModelTests(TestFixture testFixture) : IClassFixture<TestFixture>, IAsyncLifetime
{
    private readonly Fixture _fixture = AutoFixtureHelper.Create();

    private const string Password = "P@$$w0rd";
    private static readonly Guid OrganizationId = new("4f1d9c2e-8a63-4b57-9d31-6e0c7a2f4b18");
    private static readonly Guid OtherOrganizationId = new("9c3e5a71-2b48-4d69-8f02-1a7d4e6b3c95");

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
            },
            FakeOrganizationEntity.CreateValid(_fixture) with
            {
                OrganizationId = OtherOrganizationId,
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
    public async Task Analyst_CanReadCases()
    {
        // Arrange
        var httpClient = await SignInAsync("analyst-reads@mail.com", UserRole.User);

        // Act
        var response = await httpClient.GetAsync("/cases");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Analyst_CannotConfigureQueues()
    {
        // Arrange
        var httpClient = await SignInAsync("analyst-queues@mail.com", UserRole.User);

        // Act
        var response = await httpClient.GetAsync($"/organizations/{OrganizationId}/queues");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Analyst_CannotRevealAPayment()
    {
        // Arrange
        var httpClient = await SignInAsync("analyst-reveal@mail.com", UserRole.User);

        // Act
        var response = await httpClient.PostAsJsonAsync(
            $"/transactions/{Guid.NewGuid()}/reveal", new { Reason = "Checking the counterparty" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Supervisor_CanConfigureQueues()
    {
        // Arrange
        var httpClient = await SignInAsync("supervisor-queues@mail.com", UserRole.Manager);

        // Act
        var response = await httpClient.GetAsync($"/organizations/{OrganizationId}/queues");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Supervisor_CannotReachAnotherOrganization()
    {
        // Arrange
        var httpClient = await SignInAsync("supervisor-tenant@mail.com", UserRole.Manager);

        // Act
        var response = await httpClient.GetAsync($"/organizations/{OtherOrganizationId}/queues");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Supervisor_CannotListEveryOrganization()
    {
        // Arrange
        var httpClient = await SignInAsync("supervisor-orgs@mail.com", UserRole.Manager);

        // Act
        var response = await httpClient.GetAsync("/organizations");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Administrator_CanReachAnotherOrganization()
    {
        // Arrange
        var httpClient = await SignInAsync("admin-tenant@mail.com", UserRole.Admin);

        // Act
        var response = await httpClient.GetAsync($"/organizations/{OtherOrganizationId}/queues");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Auditor_CanReadCases()
    {
        // Arrange
        var httpClient = await SignInAsync("auditor-reads@mail.com", UserRole.Auditor);

        // Act
        var response = await httpClient.GetAsync("/cases");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Auditor_CanReadTheConfiguration()
    {
        // Arrange
        var httpClient = await SignInAsync("auditor-config@mail.com", UserRole.Auditor);

        // Act
        var response = await httpClient.GetAsync($"/organizations/{OrganizationId}/queues");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Auditor_CanVerifyTheAuditChain()
    {
        // Arrange
        var httpClient = await SignInAsync("auditor-chain@mail.com", UserRole.Auditor);

        // Act
        var response = await httpClient.GetAsync("/audit-logs/verification");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Auditor_CannotConfigureQueues()
    {
        // Arrange
        var httpClient = await SignInAsync("auditor-writes@mail.com", UserRole.Auditor);

        // Act
        var response = await httpClient.PostAsJsonAsync($"/organizations/{OrganizationId}/queues", new
        {
            Name = "Auditor's queue",
            RoutingPolicy = nameof(QueueRoutingPolicy.Manual),
            IsEnabled = true
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Auditor_CannotRevealAPayment()
    {
        // Arrange
        var httpClient = await SignInAsync("auditor-reveal@mail.com", UserRole.Auditor);

        // Act
        var response = await httpClient.PostAsJsonAsync(
            $"/transactions/{Guid.NewGuid()}/reveal", new { Reason = "Checking the counterparty" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Auditor_CannotDeleteACase()
    {
        // Arrange
        var httpClient = await SignInAsync("auditor-delete@mail.com", UserRole.Auditor);

        // Act
        var response = await httpClient.DeleteAsync($"/cases/{Guid.NewGuid()}");

        // Assert
        // No body on purpose: model binding runs before the permission filter, and a 400 would pass this for the wrong reason.
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Permissions_AreListedWithWhetherTheCallerHoldsThem()
    {
        // Arrange
        var httpClient = await SignInAsync("supervisor-permissions@mail.com", UserRole.Manager);

        // Act
        var response = await httpClient.GetAsync("/permissions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var permissions = await response.Content.ReadFromJsonAsync<ResponseDataDto<PermissionResponseDto[]>>();

        permissions.Should().NotBeNull();
        permissions!.Data.Should().HaveCount(Enum.GetValues<Permission>().Length);

        var platform = permissions.Data.Single(x => x.Name == "platform:administer");

        platform.IsGrantable.Should().BeFalse();
        platform.IsHeld.Should().BeFalse();

        var casesRead = permissions.Data.Single(x => x.Name == "cases:read");

        casesRead.IsGrantable.Should().BeTrue();
        casesRead.IsHeld.Should().BeTrue();
        casesRead.Resource.Should().Be("cases");
        casesRead.Action.Should().Be("read");
    }

    [Fact]
    public async Task MyPermissions_NameTheReachOfEachGrant()
    {
        // Arrange
        var httpClient = await SignInAsync("analyst-mine@mail.com", UserRole.User);

        // Act
        var response = await httpClient.GetAsync("/permissions/mine");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var grants = await response.Content.ReadFromJsonAsync<ResponseDataDto<EffectivePermissionResponseDto[]>>();

        grants.Should().NotBeNull();
        grants!.Data.Should().Contain(x => x.Name == "cases:read");
        grants.Data.Should().NotContain(x => x.Name == "cases:assign");

        grants.Data.Should().AllSatisfy(x => x.Scope.ScopeType.Should().Be(nameof(PermissionScopeType.Organization)));
    }

    private async Task<HttpClient> SignInAsync(string email, UserRole role)
    {
        var httpClient = testFixture.CreateClient();

        await AuthenticationHelper.CreateUserAsync(testFixture.Services, email, Password, [role], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(httpClient, email, Password);

        return httpClient;
    }
}
