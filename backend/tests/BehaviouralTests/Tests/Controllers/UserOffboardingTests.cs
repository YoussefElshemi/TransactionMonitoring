using System.Net;
using System.Net.Http.Json;
using AutoFixture;
using BehaviouralTests.TestHelpers;
using Core.Constants;
using Core.Enums;
using FluentAssertions;
using Infrastructure;
using Infrastructure.Configurations;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Constants;
using Presentation.Models.Invitations;
using Presentation.Models.OrganizationUsers;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

namespace BehaviouralTests.Tests.Controllers;

public class UserOffboardingTests(TestFixture testFixture) : IClassFixture<TestFixture>, IAsyncLifetime
{
    private readonly Fixture _fixture = AutoFixtureHelper.Create();
    private readonly HttpClient _httpClient = testFixture.CreateClient();

    private const string SupervisorEmail = "supervisor@mail.com";
    private const string LeaverEmail = "leaver@mail.com";
    private const string SuccessorEmail = "successor@mail.com";
    private const string Password = "P@$$w0rd";

    private static readonly Guid OrganizationId = new("a2c1f5f9-0f2e-4a3b-9d17-2ab3c1d4e5f6");
    private static readonly Guid OtherOrganizationId = new("b7c1f5f9-0f2e-4a3b-9d17-2ab3c1d4e5f7");

    [Fact]
    public async Task OffboardUser_AnalystOnly_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, SupervisorEmail, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, SupervisorEmail, Password);

        var leaver = await AuthenticationHelper.CreateUserAsync(testFixture.Services, LeaverEmail, Password, [UserRole.User], OrganizationId);

        // Act
        var response = await OffboardAsync(leaver.Id, null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task OffboardUser_ASuccessorIsNamed_ShouldMoveTheOpenCasesToThem()
    {
        // Arrange
        await AuthenticateAsManagerAsync();
        var leaver = await CreateUserAsync(LeaverEmail, OrganizationId);
        var successor = await CreateUserAsync(SuccessorEmail, OrganizationId);

        await GivenAssignedCasesAsync(leaver.Id, CaseStatus.InReview, 3);
        await GivenAssignedCasesAsync(leaver.Id, CaseStatus.Approved, 2);

        // Act
        var response = await OffboardAsync(leaver.Id, successor.Id);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var offboarded = await response.Content.ReadDataFromJsonAsync<OffboardedUserResponseDto>();
        offboarded!.ReassignedCaseCount.Should().Be(3);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var cases = await context.Cases.Where(x => x.OrganizationId == OrganizationId).ToListAsync();

        cases.Count(x => x.AssignedToUserId == successor.Id).Should().Be(3);
        cases.Count(x => x.AssignedToUserId == leaver.Id).Should().Be(2);
        cases.Where(x => x.AssignedToUserId == successor.Id).Should().OnlyContain(x => x.StatusId == (int)CaseStatus.InReview);
    }

    [Fact]
    public async Task OffboardUser_NobodyIsNamed_ShouldHandTheOpenCasesBackToThePool()
    {
        // Arrange
        await AuthenticateAsManagerAsync();
        var leaver = await CreateUserAsync(LeaverEmail, OrganizationId);

        await GivenAssignedCasesAsync(leaver.Id, CaseStatus.InReview, 2);

        // Act
        var response = await OffboardAsync(leaver.Id, null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var cases = await context.Cases.Where(x => x.OrganizationId == OrganizationId).ToListAsync();
        cases.Should().OnlyContain(x => x.AssignedToUserId == null);
        cases.Should().OnlyContain(x => x.StatusId == (int)CaseStatus.Pending);
    }

    [Fact]
    public async Task OffboardUser_Always_ShouldReleaseTheAccountToTheDefaultOrganization()
    {
        // Arrange
        await AuthenticateAsManagerAsync();
        var leaver = await CreateUserAsync(LeaverEmail, OrganizationId);

        // Act
        var response = await OffboardAsync(leaver.Id, null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var userEntity = await context.Users.FirstAsync(x => x.Id == leaver.Id);
        userEntity.OrganizationId.Should().Be(SystemConstants.DefaultOrganization);
    }

    [Fact]
    public async Task OffboardUser_TheLeaverHasOpenInvitations_ShouldWithdrawThem()
    {
        // Arrange
        var leaver = await AuthenticationHelper.CreateUserAsync(
            testFixture.Services, LeaverEmail, Password, [UserRole.User, UserRole.Manager], OrganizationId);

        var leaverClient = testFixture.CreateClient();
        await AuthenticationHelper.SetTokenAsync(leaverClient, LeaverEmail, Password);

        await leaverClient.PostAsJsonAsync("/invitations", new CreateInvitationRequestDto
        {
            Email = "pending@mail.com",
            Role = nameof(UserRole.User),
            SubOrganizationId = null
        });

        await AuthenticateAsManagerAsync();

        // Act
        var response = await OffboardAsync(leaver.Id, null);

        // Assert
        var offboarded = await response.Content.ReadDataFromJsonAsync<OffboardedUserResponseDto>();
        offboarded!.RevokedInvitationCount.Should().Be(1);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var invitationEntity = await context.Invitations.FirstAsync(x => x.InvitedByUserId == leaver.Id);
        invitationEntity.RevokedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task OffboardUser_TheCallerIsThePersonLeaving_ShouldReturnBadRequest()
    {
        // Arrange
        var supervisor = await AuthenticateAsManagerAsync();

        // Act
        var response = await OffboardAsync(supervisor.Id, null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task OffboardUser_TheSuccessorBelongsToAnotherOrganization_ShouldReturnNotFound()
    {
        // Arrange
        await AuthenticateAsManagerAsync();
        var leaver = await CreateUserAsync(LeaverEmail, OrganizationId);
        var outsider = await CreateUserAsync(SuccessorEmail, OtherOrganizationId);

        // Act
        var response = await OffboardAsync(leaver.Id, outsider.Id);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private Task<HttpResponseMessage> OffboardAsync(Guid userId, Guid? successorUserId)
    {
        return _httpClient.PostAsJsonAsync(
            $"/organizations/{OrganizationId}/users/{userId}/offboard",
            new OffboardUserRequestBodyDto { SuccessorUserId = successorUserId });
    }

    private Task<ApplicationUserEntity> CreateUserAsync(string email, Guid organizationId)
    {
        return AuthenticationHelper.CreateUserAsync(testFixture.Services, email, Password, [UserRole.User], organizationId);
    }

    private async Task GivenAssignedCasesAsync(Guid userId, CaseStatus caseStatus, int count)
    {
        await DatabaseHelper.CreateCases(testFixture.Services, Enumerable.Range(0, count).Select(
            _ => FakeCaseEntity.CreateValid(_fixture) with
            {
                OrganizationId = OrganizationId,
                Organization = null!,
                AssignedToUserId = userId,
                AssignedToUser = null,
                StatusId = (int)caseStatus,
                QueueId = null,
                IsDeleted = false
            }));
    }

    private async Task<ApplicationUserEntity> AuthenticateAsManagerAsync()
    {
        var supervisor = await AuthenticationHelper.CreateUserAsync(
            testFixture.Services, SupervisorEmail, Password, [UserRole.User, UserRole.Manager], OrganizationId);

        await AuthenticationHelper.SetTokenAsync(_httpClient, SupervisorEmail, Password);

        return supervisor;
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
