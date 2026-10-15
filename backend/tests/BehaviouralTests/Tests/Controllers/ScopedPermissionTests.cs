using System.Net;
using System.Net.Http.Json;
using AutoFixture;
using BehaviouralTests.TestHelpers;
using Core.Enums;
using FluentAssertions;
using Infrastructure.Configurations;
using Infrastructure.Entities;
using Presentation.Models.Cases;
using Presentation.Models.Common;
using Presentation.Models.Permissions;
using Presentation.Models.Queues;
using Presentation.Models.Roles;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

namespace BehaviouralTests.Tests.Controllers;

public class ScopedPermissionTests(TestFixture testFixture) : IClassFixture<TestFixture>, IAsyncLifetime
{
    private readonly Fixture _fixture = AutoFixtureHelper.Create();
    private readonly HttpClient _supervisorClient = testFixture.CreateClient();

    private const string SupervisorEmail = "scope-supervisor@mail.com";
    private const string ReviewerEmail = "scope-reviewer@mail.com";
    private const string Password = "P@$$w0rd";
    private static readonly Guid OrganizationId = new("1e6b8d34-7f52-4a91-b3c8-5d07e924a6f1");

    private Guid _reviewerId;
    private Guid _ownedQueueId;
    private Guid _otherQueueId;
    private Guid _ownedCaseId;
    private Guid _otherCaseId;

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
        await AuthenticationHelper.SetTokenAsync(_supervisorClient, SupervisorEmail, Password);

        var reviewer = await AuthenticationHelper.CreateUserAsync(testFixture.Services, ReviewerEmail, Password, [], OrganizationId);
        _reviewerId = reviewer.Id;

        _ownedQueueId = await CreateQueueAsync("Sanctions");
        _otherQueueId = await CreateQueueAsync("Fraud");

        _ownedCaseId = Guid.NewGuid();
        _otherCaseId = Guid.NewGuid();

        await DatabaseHelper.CreateCases(testFixture.Services,
        [
            Case(_ownedCaseId, _ownedQueueId),
            Case(_otherCaseId, _otherQueueId)
        ]);
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task QueueScopedGrant_ReachesACaseInThatQueue()
    {
        // Arrange
        var reviewerClient = await GrantOverQueueAsync("Queue reviewer", _ownedQueueId, "cases:read", "cases:write");

        // Act
        var response = await reviewerClient.GetAsync($"/cases/{_ownedCaseId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task QueueScopedGrant_DoesNotReachACaseInAnotherQueue()
    {
        // Arrange
        var reviewerClient = await GrantOverQueueAsync("Queue reviewer", _ownedQueueId, "cases:read", "cases:write");

        // Act
        var response = await reviewerClient.GetAsync($"/cases/{_otherCaseId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task QueueScopedGrant_NarrowsTheCaseListing()
    {
        // Arrange
        var reviewerClient = await GrantOverQueueAsync("Queue reviewer", _ownedQueueId, "cases:read");

        // Act
        var response = await reviewerClient.GetAsync("/cases?PageSize=100");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var cases = await response.Content.ReadFromJsonAsync<PagedResponseDto<CaseResourceDto>>();

        cases!.Data.Select(x => x.Case.CaseId).Should().Equal(_ownedCaseId);
        cases.Metadata.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task UnscopedGrant_SeesEveryCase()
    {
        // Arrange
        var role = await CreateRoleAsync("Whole desk", "cases:read");
        await AssignAsync(_reviewerId, role, scopeType: null, queueId: null);

        var reviewerClient = await SignInAsync(ReviewerEmail);

        // Act
        var response = await reviewerClient.GetAsync("/cases?PageSize=100");

        // Assert
        var cases = await response.Content.ReadFromJsonAsync<PagedResponseDto<CaseResourceDto>>();

        cases!.Data.Select(x => x.Case.CaseId).Should().BeEquivalentTo([_ownedCaseId, _otherCaseId]);
    }

    [Fact]
    public async Task QueueScopedGrant_DoesNotReachAnotherQueuesConfiguration()
    {
        // Arrange
        var reviewerClient = await GrantOverQueueAsync("Queue supervisor", _ownedQueueId, "queues:read", "queues:write");

        // Act
        var owned = await reviewerClient.GetAsync($"/organizations/{OrganizationId}/queues/{_ownedQueueId}");
        var other = await reviewerClient.GetAsync($"/organizations/{OrganizationId}/queues/{_otherQueueId}");

        // Assert
        owned.StatusCode.Should().Be(HttpStatusCode.OK);
        other.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task QueueScopedGrant_NarrowsTheQueueListing()
    {
        // Arrange
        var reviewerClient = await GrantOverQueueAsync("Queue supervisor", _ownedQueueId, "queues:read");

        // Act
        var response = await reviewerClient.GetAsync($"/organizations/{OrganizationId}/queues?PageSize=100");

        // Assert
        var queues = await response.Content.ReadFromJsonAsync<PagedResponseDto<QueueResourceDto>>();

        queues!.Data.Select(x => x.Queue.QueueId).Should().Equal(_ownedQueueId);
    }

    [Fact]
    public async Task ScopedGrant_DoesNotWidenBeyondThePermissionsGranted()
    {
        // Arrange
        var reviewerClient = await GrantOverQueueAsync("Queue reader", _ownedQueueId, "queues:read");

        // Act
        var response = await reviewerClient.PatchAsJsonAsync(
            $"/organizations/{OrganizationId}/queues/{_ownedQueueId}", new { Priority = 5 });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AssignRole_GivenAQueueScopeWithoutAQueue_IsRefused()
    {
        // Arrange
        var role = await CreateRoleAsync("Queue reviewer", "cases:read");

        // Act
        var response = await _supervisorClient.PostAsJsonAsync(
            $"/organizations/{OrganizationId}/users/{_reviewerId}/role-assignments",
            new { RoleId = role, ScopeType = nameof(PermissionScopeType.Queue) });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AssignRole_GivenAQueueScope_RecordsTheReach()
    {
        // Arrange
        var role = await CreateRoleAsync("Queue reviewer", "cases:read");

        // Act
        var assignment = await AssignAsync(_reviewerId, role, nameof(PermissionScopeType.Queue), _ownedQueueId);

        // Assert
        assignment.Scope.ScopeType.Should().Be(nameof(PermissionScopeType.Queue));
        assignment.Scope.QueueId.Should().Be(_ownedQueueId);
        assignment.Scope.RuleSetId.Should().BeNull();
        assignment.Scope.SubOrganizationId.Should().BeNull();
    }

    [Fact]
    public async Task MyPermissions_NamesTheQueueAGrantReaches()
    {
        // Arrange
        var reviewerClient = await GrantOverQueueAsync("Queue reviewer", _ownedQueueId, "cases:read");

        // Act
        var response = await reviewerClient.GetAsync("/permissions/mine");

        // Assert
        var grants = await response.Content.ReadFromJsonAsync<ResponseDataDto<EffectivePermissionResponseDto[]>>();

        grants!.Data.Should().Contain(x =>
            x.Name == "cases:read" &&
            x.Scope.ScopeType == nameof(PermissionScopeType.Queue) &&
            x.Scope.QueueId == _ownedQueueId);
    }

    [Fact]
    public async Task QueueScopedGrant_DoesNotNarrowSomebodyWhoAlsoHoldsItOutright()
    {
        // Arrange
        var analyst = await AuthenticationHelper.CreateUserAsync(
            testFixture.Services, "scope-analyst@mail.com", Password, [UserRole.User], OrganizationId);

        var role = await CreateRoleAsync("Sanctions reviewer", "cases:read");
        await AssignAsync(analyst.Id, role, nameof(PermissionScopeType.Queue), _ownedQueueId);

        var analystClient = await SignInAsync("scope-analyst@mail.com");

        // Act
        var response = await analystClient.GetAsync("/cases?PageSize=100");

        // Assert
        var cases = await response.Content.ReadFromJsonAsync<PagedResponseDto<CaseResourceDto>>();

        cases!.Data.Select(x => x.Case.CaseId).Should().BeEquivalentTo([_ownedCaseId, _otherCaseId]);
    }

    private async Task<HttpClient> GrantOverQueueAsync(string roleName, Guid queueId, params string[] permissions)
    {
        var role = await CreateRoleAsync(roleName, permissions);

        await AssignAsync(_reviewerId, role, nameof(PermissionScopeType.Queue), queueId);

        return await SignInAsync(ReviewerEmail);
    }

    private async Task<Guid> CreateRoleAsync(string name, params string[] permissions)
    {
        var response = await _supervisorClient.PostAsJsonAsync($"/organizations/{OrganizationId}/roles", new
        {
            Name = name,
            Permissions = permissions
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await response.Content.ReadFromJsonAsync<ResponseDataDto<RoleResponseDto>>();

        return created!.Data.RoleId;
    }

    private async Task<RoleAssignmentResponseDto> AssignAsync(Guid userId, Guid roleId, string? scopeType, Guid? queueId)
    {
        var response = await _supervisorClient.PostAsJsonAsync(
            $"/organizations/{OrganizationId}/users/{userId}/role-assignments",
            new { RoleId = roleId, ScopeType = scopeType, QueueId = queueId });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var assignment = await response.Content.ReadFromJsonAsync<ResponseDataDto<RoleAssignmentResponseDto>>();

        return assignment!.Data;
    }

    private async Task<Guid> CreateQueueAsync(string name)
    {
        var response = await _supervisorClient.PostAsJsonAsync($"/organizations/{OrganizationId}/queues", new
        {
            Name = name,
            RoutingPolicy = nameof(QueueRoutingPolicy.Manual),
            IsEnabled = true
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await response.Content.ReadFromJsonAsync<ResponseDataDto<QueueResponseDto>>();

        return created!.Data.QueueId;
    }

    private async Task<HttpClient> SignInAsync(string email)
    {
        var httpClient = testFixture.CreateClient();

        await AuthenticationHelper.SetTokenAsync(httpClient, email, Password);

        return httpClient;
    }

    private CaseEntity Case(Guid caseId, Guid queueId)
    {
        return FakeCaseEntity.CreateValid(_fixture) with
        {
            CaseId = caseId,
            OrganizationId = OrganizationId,
            Organization = null!,
            QueueId = queueId,
            IsDeleted = false
        };
    }
}
