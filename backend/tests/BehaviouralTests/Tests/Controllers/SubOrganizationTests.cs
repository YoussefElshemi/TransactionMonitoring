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
using Presentation.Models.Queues;
using Presentation.Models.Roles;
using Presentation.Models.SubOrganizations;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

namespace BehaviouralTests.Tests.Controllers;

public class SubOrganizationTests(TestFixture testFixture) : IClassFixture<TestFixture>, IAsyncLifetime
{
    private readonly Fixture _fixture = AutoFixtureHelper.Create();
    private readonly HttpClient _supervisorClient = testFixture.CreateClient();

    private const string SupervisorEmail = "group-supervisor@mail.com";
    private const string Password = "P@$$w0rd";
    private static readonly Guid OrganizationId = new("5d9a3f18-4e72-4c86-9b15-7a2c0e6d813f");

    private Guid _northId;
    private Guid _southId;
    private Guid _northQueueId;
    private Guid _southQueueId;
    private Guid _northCaseId;
    private Guid _southCaseId;

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

        _northId = await CreateSubOrganizationAsync("Northern Bank");
        _southId = await CreateSubOrganizationAsync("Southern Bank");

        _northQueueId = await CreateQueueAsync("Northern review", _northId);
        _southQueueId = await CreateQueueAsync("Southern review", _southId);

        _northCaseId = Guid.NewGuid();
        _southCaseId = Guid.NewGuid();

        await DatabaseHelper.CreateCases(testFixture.Services,
        [
            Case(_northCaseId, _northQueueId),
            Case(_southCaseId, _southQueueId)
        ]);
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task CreateSubOrganization_GivenAValidRequest_ReturnsCreated()
    {
        // Act
        var response = await _supervisorClient.PostAsJsonAsync($"/organizations/{OrganizationId}/sub-organizations", new
        {
            Name = "Eastern Bank",
            ExternalReference = "LEI-549300EXAMPLE",
            IsEnabled = true
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await response.Content.ReadFromJsonAsync<ResponseDataDto<SubOrganizationResponseDto>>();

        created!.Data.Name.Should().Be("Eastern Bank");
        created.Data.ExternalReference.Should().Be("LEI-549300EXAMPLE");
        created.Data.OrganizationId.Should().Be(OrganizationId);
    }

    [Fact]
    public async Task CreateSubOrganization_GivenADuplicateName_IsRefused()
    {
        // Act
        var response = await _supervisorClient.PostAsJsonAsync($"/organizations/{OrganizationId}/sub-organizations", new
        {
            Name = "NORTHERN BANK",
            IsEnabled = true
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SubOrganizationScopedGrant_ReachesTheQueuesInThatUnit()
    {
        // Arrange
        var reviewerClient = await GrantOverUnitAsync("north-reviewer@mail.com", _northId, "queues:read");

        // Act
        var north = await reviewerClient.GetAsync($"/organizations/{OrganizationId}/queues/{_northQueueId}");
        var south = await reviewerClient.GetAsync($"/organizations/{OrganizationId}/queues/{_southQueueId}");

        // Assert
        north.StatusCode.Should().Be(HttpStatusCode.OK);
        south.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SubOrganizationScopedGrant_ReachesTheCasesInThatUnitsQueues()
    {
        // Arrange
        var reviewerClient = await GrantOverUnitAsync("north-cases@mail.com", _northId, "cases:read");

        // Act
        var north = await reviewerClient.GetAsync($"/cases/{_northCaseId}");
        var south = await reviewerClient.GetAsync($"/cases/{_southCaseId}");

        // Assert
        north.StatusCode.Should().Be(HttpStatusCode.OK);
        south.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SubOrganizationScopedGrant_NarrowsTheCaseListing()
    {
        // Arrange
        var reviewerClient = await GrantOverUnitAsync("north-listing@mail.com", _northId, "cases:read");

        // Act
        var response = await reviewerClient.GetAsync("/cases?PageSize=100");

        // Assert
        var cases = await response.Content.ReadFromJsonAsync<PagedResponseDto<CaseResourceDto>>();

        cases!.Data.Select(x => x.Case.CaseId).Should().Equal(_northCaseId);
    }

    [Fact]
    public async Task HomeSubOrganization_NarrowsAnOrganizationWideRole()
    {
        // Arrange
        var analyst = await AuthenticationHelper.CreateUserAsync(
            testFixture.Services, "homed-analyst@mail.com", Password, [UserRole.User], OrganizationId);

        var analystClient = await SignInAsync("homed-analyst@mail.com");

        var before = await analystClient.GetAsync("/cases?PageSize=100");

        // Act
        await SetHomeUnitAsync(analyst.Id, _southId);

        var after = await analystClient.GetAsync("/cases?PageSize=100");

        // Assert
        var casesBefore = await before.Content.ReadFromJsonAsync<PagedResponseDto<CaseResourceDto>>();
        var casesAfter = await after.Content.ReadFromJsonAsync<PagedResponseDto<CaseResourceDto>>();

        casesBefore!.Data.Select(x => x.Case.CaseId).Should().BeEquivalentTo([_northCaseId, _southCaseId]);
        casesAfter!.Data.Select(x => x.Case.CaseId).Should().Equal(_southCaseId);
    }

    [Fact]
    public async Task HomeSubOrganization_RefusesAResourceInAnotherUnit()
    {
        // Arrange
        var analyst = await AuthenticationHelper.CreateUserAsync(
            testFixture.Services, "homed-refused@mail.com", Password, [UserRole.User], OrganizationId);

        await SetHomeUnitAsync(analyst.Id, _southId);

        var analystClient = await SignInAsync("homed-refused@mail.com");

        // Act
        var response = await analystClient.GetAsync($"/cases/{_northCaseId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ClearingTheHomeSubOrganization_WidensThemBackOut()
    {
        // Arrange
        var analyst = await AuthenticationHelper.CreateUserAsync(
            testFixture.Services, "homed-cleared@mail.com", Password, [UserRole.User], OrganizationId);

        await SetHomeUnitAsync(analyst.Id, _southId);

        var analystClient = await SignInAsync("homed-cleared@mail.com");

        // Act
        await SetHomeUnitAsync(analyst.Id, null);

        var response = await analystClient.GetAsync("/cases?PageSize=100");

        // Assert
        var cases = await response.Content.ReadFromJsonAsync<PagedResponseDto<CaseResourceDto>>();

        cases!.Data.Select(x => x.Case.CaseId).Should().BeEquivalentTo([_northCaseId, _southCaseId]);
    }

    [Fact]
    public async Task HomeSubOrganization_TakesThePlatformReachWithIt()
    {
        // Arrange
        var administrator = await AuthenticationHelper.CreateUserAsync(
            testFixture.Services, "homed-admin@mail.com", Password, [UserRole.Admin], OrganizationId);

        await SetHomeUnitAsync(administrator.Id, _southId);

        var administratorClient = await SignInAsync("homed-admin@mail.com");

        // Act
        var response = await administratorClient.GetAsync("/organizations");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SubOrganizationUsages_CountWhatIsInside()
    {
        // Arrange
        var analyst = await AuthenticationHelper.CreateUserAsync(
            testFixture.Services, "counted-analyst@mail.com", Password, [UserRole.User], OrganizationId);

        await SetHomeUnitAsync(analyst.Id, _northId);

        // Act
        var response = await _supervisorClient.GetAsync($"/organizations/{OrganizationId}/sub-organizations/{_northId}/usages");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var usage = await response.Content.ReadFromJsonAsync<ResponseDataDto<SubOrganizationUsageResponseDto>>();

        usage!.Data.QueueCount.Should().Be(1);
        usage.Data.UserCount.Should().Be(1);
    }

    [Fact]
    public async Task AssignRole_GivenAnotherOrganizationsUnit_IsRefused()
    {
        // Arrange
        var analyst = await AuthenticationHelper.CreateUserAsync(
            testFixture.Services, "cross-tenant@mail.com", Password, [UserRole.User], OrganizationId);

        var role = await CreateRoleAsync("Cross tenant", "cases:read");

        // Act
        var response = await _supervisorClient.PostAsJsonAsync(
            $"/organizations/{OrganizationId}/users/{analyst.Id}/role-assignments",
            new
            {
                RoleId = role,
                ScopeType = nameof(PermissionScopeType.SubOrganization),
                SubOrganizationId = Guid.NewGuid()
            });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateQueue_GivenAnUnknownSubOrganization_IsNotFound()
    {
        // Act
        var response = await _supervisorClient.PostAsJsonAsync($"/organizations/{OrganizationId}/queues", new
        {
            Name = "Unfiled",
            RoutingPolicy = nameof(QueueRoutingPolicy.Manual),
            IsEnabled = true,
            SubOrganizationId = Guid.NewGuid()
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateRuleSet_GivenAnUnknownSubOrganization_IsNotFound()
    {
        // Act
        var response = await _supervisorClient.PostAsJsonAsync("/rule-sets", new
        {
            FriendlyName = "Unfiled",
            IsEnabled = true,
            SubOrganizationId = Guid.NewGuid()
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateQueue_GivenAnUnknownSubOrganization_IsNotFound()
    {
        // Act
        var response = await _supervisorClient.PatchAsJsonAsync(
            $"/organizations/{OrganizationId}/queues/{_northQueueId}",
            new { SubOrganizationId = Guid.NewGuid() });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Analyst_CannotCreateASubOrganization()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(
            testFixture.Services, "analyst-units@mail.com", Password, [UserRole.User], OrganizationId);

        var analystClient = await SignInAsync("analyst-units@mail.com");

        // Act
        var response = await analystClient.PostAsJsonAsync($"/organizations/{OrganizationId}/sub-organizations", new
        {
            Name = "Rogue Bank",
            IsEnabled = true
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private async Task<HttpClient> GrantOverUnitAsync(string email, Guid subOrganizationId, params string[] permissions)
    {
        var reviewer = await AuthenticationHelper.CreateUserAsync(testFixture.Services, email, Password, [], OrganizationId);

        var role = await CreateRoleAsync($"Unit reviewer {email}", permissions);

        var response = await _supervisorClient.PostAsJsonAsync(
            $"/organizations/{OrganizationId}/users/{reviewer.Id}/role-assignments",
            new
            {
                RoleId = role,
                ScopeType = nameof(PermissionScopeType.SubOrganization),
                SubOrganizationId = subOrganizationId
            });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        return await SignInAsync(email);
    }

    private async Task SetHomeUnitAsync(Guid userId, Guid? subOrganizationId)
    {
        var response = await _supervisorClient.PutAsJsonAsync(
            $"/organizations/{OrganizationId}/users/{userId}/sub-organization",
            new { SubOrganizationId = subOrganizationId });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    private async Task<Guid> CreateSubOrganizationAsync(string name)
    {
        var response = await _supervisorClient.PostAsJsonAsync($"/organizations/{OrganizationId}/sub-organizations", new
        {
            Name = name,
            IsEnabled = true
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await response.Content.ReadFromJsonAsync<ResponseDataDto<SubOrganizationResponseDto>>();

        return created!.Data.SubOrganizationId;
    }

    private async Task<Guid> CreateQueueAsync(string name, Guid subOrganizationId)
    {
        var response = await _supervisorClient.PostAsJsonAsync($"/organizations/{OrganizationId}/queues", new
        {
            Name = name,
            RoutingPolicy = nameof(QueueRoutingPolicy.Manual),
            IsEnabled = true,
            SubOrganizationId = subOrganizationId
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await response.Content.ReadFromJsonAsync<ResponseDataDto<QueueResponseDto>>();

        return created!.Data.QueueId;
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
