using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
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
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Constants;
using Presentation.Models.Common;
using Presentation.Models.Invitations;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

namespace BehaviouralTests.Tests.Controllers;

public class InvitationControllerTests(TestFixture testFixture) : IClassFixture<TestFixture>, IAsyncLifetime
{
    private readonly Fixture _fixture = AutoFixtureHelper.Create();
    private readonly HttpClient _httpClient = testFixture.CreateClient();

    private const string Email = "supervisor@mail.com";
    private const string Password = "P@$$w0rd";
    private const string InviteeEmail = "newjoiner@mail.com";
    private const string InviteePassword = "P@$$w0rd1";

    private static readonly Guid OrganizationId = new("5b21c5b2-5f8e-4d1c-9a1d-9d0f7f2e77aa");

    [Fact]
    public async Task CreateInvitation_NotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var createRequest = CreateRequest();

        // Act
        var response = await _httpClient.PostAsJsonAsync("/invitations", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task CreateInvitation_AnalystOnly_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.PostAsJsonAsync("/invitations", CreateRequest());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateInvitation_GivenValidRequest_ShouldReturnCreatedAndStoreOnlyTheDigest()
    {
        // Arrange
        await AuthenticateAsManagerAsync();

        // Act
        var response = await _httpClient.PostAsJsonAsync("/invitations", CreateRequest());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await response.Content.ReadDataFromJsonAsync<IssuedInvitationResourceDto>();
        created.Should().NotBeNull();
        created!.Invitation.Email.Should().Be(InviteeEmail);
        created.Invitation.Status.Should().Be(nameof(InvitationStatus.Pending));
        created.Invitation.AcceptUrl.Should().Contain(created.Invitation.InvitationToken);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var invitationEntity = await context.Invitations.FirstAsync(x => x.InvitationId == created.Invitation.InvitationId);
        invitationEntity.TokenHash.Should().Be(InvitationTokenHelper.Hash(new InvitationToken(created.Invitation.InvitationToken)));
        invitationEntity.TokenHash.Should().NotBe(created.Invitation.InvitationToken);
        invitationEntity.RoleId.Should().Be(RoleHelper.MapFromUserRole(UserRole.User));
    }

    [Fact]
    public async Task CreateInvitation_OfferingTheInstallationOperatorsSeat_ShouldReturnBadRequest()
    {
        // Arrange
        await AuthenticateAsManagerAsync();

        // Act
        var response = await _httpClient.PostAsJsonAsync("/invitations", CreateRequest(nameof(UserRole.Admin)));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateInvitation_RepeatedForTheSameAddress_ShouldReissueRatherThanAddASecond()
    {
        // Arrange
        await AuthenticateAsManagerAsync();
        var first = await InviteAsync();

        // Act
        var second = await InviteAsync();

        // Assert
        second.InvitationId.Should().Be(first.InvitationId);
        second.InvitationToken.Should().NotBe(first.InvitationToken);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var count = await context.Invitations.CountAsync(x => x.OrganizationId == OrganizationId);
        count.Should().Be(1);
    }

    [Fact]
    public async Task CreateInvitation_PlatformOperatorInTheDefaultOrganization_ShouldBeRefused()
    {
        // Arrange
        await AuthenticateAsPlatformOperatorAsync();

        // Act
        var response = await _httpClient.PostAsJsonAsync("/invitations", CreateRequest());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateOrganizationInvitation_AsPlatformOperator_ShouldIssueIntoTheNamedOrganization()
    {
        // Arrange
        await AuthenticateAsPlatformOperatorAsync();

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/organizations/{OrganizationId}/invitations", CreateBody(nameof(UserRole.Manager)));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = (await response.Content.ReadDataFromJsonAsync<IssuedInvitationResourceDto>())!.Invitation;
        created.OrganizationId.Should().Be(OrganizationId);
        created.Role.Should().Be(nameof(UserRole.Manager));

        created.InvitedByPlatformOperator.Should().BeTrue();

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var invitationEntity = await context.Invitations.FirstAsync(x => x.InvitationId == created.InvitationId);
        invitationEntity.OrganizationId.Should().Be(OrganizationId);
        invitationEntity.RoleId.Should().Be(RoleHelper.MapFromUserRole(UserRole.Manager));
    }

    [Fact]
    public async Task CreateOrganizationInvitation_AsPlatformOperator_ShouldLandTheFirstSupervisorInThatOrganization()
    {
        // Arrange
        await AuthenticateAsPlatformOperatorAsync();

        var response = await _httpClient.PostAsJsonAsync($"/organizations/{OrganizationId}/invitations", CreateBody(nameof(UserRole.Manager)));
        var invitation = (await response.Content.ReadDataFromJsonAsync<IssuedInvitationResourceDto>())!.Invitation;

        // Act
        var acceptResponse = await AcceptAsync(invitation.InvitationToken);

        // Assert
        acceptResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var accepted = await acceptResponse.Content.ReadDataFromJsonAsync<AcceptedInvitationResponseDto>();
        accepted!.OrganizationId.Should().Be(OrganizationId);
        accepted.Role.Should().Be(nameof(UserRole.Manager));

        var supervisorClient = testFixture.CreateClient();
        await AuthenticationHelper.SetTokenAsync(supervisorClient, InviteeEmail, InviteePassword);

        var ownInvite = await supervisorClient.PostAsJsonAsync("/invitations", new CreateInvitationRequestDto
        {
            Email = "analyst@mail.com",
            Role = nameof(UserRole.User),
            SubOrganizationId = null
        });

        ownInvite.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateOrganizationInvitation_PlatformOperatorOffersTheOperatorsSeat_ShouldBeRefused()
    {
        // Arrange
        await AuthenticateAsPlatformOperatorAsync();

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/organizations/{OrganizationId}/invitations", CreateBody(nameof(UserRole.Admin)));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        (await context.Invitations.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task CreateOrganizationInvitation_ManagerNamingAnotherOrganization_ShouldReturnForbiddenNamingThePlatformPermission()
    {
        // Arrange
        await AuthenticateAsManagerAsync();

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/organizations/{OtherOrganizationId}/invitations", CreateBody());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problemDetails!.Extensions["requiredPermission"]!.ToString().Should().Be("platform:administer");
    }

    [Fact]
    public async Task CreateOrganizationInvitation_ManagerNamingTheirOwnOrganization_ShouldBeAllowed()
    {
        // Arrange
        await AuthenticateAsManagerAsync();

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/organizations/{OrganizationId}/invitations", CreateBody());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = (await response.Content.ReadDataFromJsonAsync<IssuedInvitationResourceDto>())!.Invitation;
        created.InvitedByPlatformOperator.Should().BeFalse();
    }

    [Fact]
    public async Task CreateOrganizationInvitation_AddressBelongsToAThirdOrganization_ShouldReturnConflictRatherThanMoveThem()
    {
        // Arrange
        await AuthenticateAsPlatformOperatorAsync();

        await AuthenticationHelper.CreateUserAsync(
            testFixture.Services, InviteeEmail, InviteePassword, [UserRole.User], OtherOrganizationId);

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/organizations/{OrganizationId}/invitations", CreateBody());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        ((JsonElement)problemDetails!.Extensions["sameOrganization"]!).GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task CreateOrganizationInvitation_OrganizationDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        await AuthenticateAsPlatformOperatorAsync();

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/organizations/{Guid.NewGuid()}/invitations", CreateBody());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ListInvitations_OperatorIssuedTheInvitation_ShouldTellTheCustomerWithoutDisclosingTheAccount()
    {
        // Arrange
        await AuthenticateAsPlatformOperatorAsync();
        await _httpClient.PostAsJsonAsync($"/organizations/{OrganizationId}/invitations", CreateBody(nameof(UserRole.Manager)));

        var managerClient = testFixture.CreateClient();
        await AuthenticationHelper.CreateUserAsync(
            testFixture.Services, "supervisor2@mail.com", Password, [UserRole.User, UserRole.Manager], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(managerClient, "supervisor2@mail.com", Password);

        // Act
        var response = await managerClient.GetAsync("/invitations");

        // Assert
        var invitations = await response.Content.ReadFromJsonAsync<PagedResponseDto<InvitationResourceDto>>();
        invitations!.Data.Should().ContainSingle();

        var resource = invitations.Data[0];

        resource.InvitedByUser.Should().BeNull();
        resource.Invitation.InvitedByPlatformOperator.Should().BeTrue();
    }

    [Fact]
    public async Task ListInvitations_AfterRevoking_ShouldOnlyShowItWhenHistoryIsAskedFor()
    {
        // Arrange
        await AuthenticateAsManagerAsync();
        var invitation = await InviteAsync();

        await _httpClient.PostAsync($"/invitations/{invitation.InvitationId}/revoke", null);

        // Act
        var pendingResponse = await _httpClient.GetAsync("/invitations");
        var historyResponse = await _httpClient.GetAsync("/invitations?includeResolved=true");

        // Assert
        var pending = await pendingResponse.Content.ReadFromJsonAsync<PagedResponseDto<InvitationResourceDto>>();
        pending!.Data.Should().BeEmpty();

        var history = await historyResponse.Content.ReadFromJsonAsync<PagedResponseDto<InvitationResourceDto>>();
        history!.Data.Should().ContainSingle();
        history.Data[0].Invitation.Status.Should().Be(nameof(InvitationStatus.Revoked));
    }

    [Fact]
    public async Task RevokeInvitation_RepeatedForTheSameInvitation_ShouldReturnConflictNamingTheStateItCollidedWith()
    {
        // Arrange
        await AuthenticateAsManagerAsync();
        var invitation = await InviteAsync();

        await _httpClient.PostAsync($"/invitations/{invitation.InvitationId}/revoke", null);

        // Act
        var response = await _httpClient.PostAsync($"/invitations/{invitation.InvitationId}/revoke", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problemDetails!.Extensions.Should().ContainKey("invitationStatus");
        problemDetails.Extensions["invitationStatus"]!.ToString().Should().Be(nameof(InvitationStatus.Revoked));
    }

    [Fact]
    public async Task ResendInvitation_GivenValidRequest_ShouldRetireTheTokenItReplaces()
    {
        // Arrange
        await AuthenticateAsManagerAsync();
        var invitation = await InviteAsync();

        // Act
        var response = await _httpClient.PostAsync($"/invitations/{invitation.InvitationId}/resend", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var reissued = (await response.Content.ReadDataFromJsonAsync<IssuedInvitationResourceDto>())!.Invitation;
        reissued.InvitationToken.Should().NotBe(invitation.InvitationToken);

        var accepted = await AcceptAsync(invitation.InvitationToken);
        accepted.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PreviewInvitation_GivenAValidToken_ShouldNameTheOrganizationAndSeatWithoutAuthentication()
    {
        // Arrange
        await AuthenticateAsManagerAsync();
        var invitation = await InviteAsync();

        var anonymousClient = testFixture.CreateClient();

        // Act
        var response = await anonymousClient.GetAsync($"/invitations/preview?token={invitation.InvitationToken}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var preview = await response.Content.ReadDataFromJsonAsync<InvitationPreviewResponseDto>();
        preview!.Email.Should().Be(InviteeEmail);
        preview.Role.Should().Be(nameof(UserRole.User));
        preview.OrganizationName.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task PreviewInvitation_GivenAnUnknownToken_ShouldReturnBadRequest()
    {
        // Arrange
        var anonymousClient = testFixture.CreateClient();

        // Act
        var response = await anonymousClient.GetAsync($"/invitations/preview?token={InvitationTokenHelper.Generate()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AcceptInvitation_AddressHasNoAccount_ShouldProvisionOneInTheInvitingOrganization()
    {
        // Arrange
        await AuthenticateAsManagerAsync();
        var invitation = await InviteAsync();

        // Act
        var response = await AcceptAsync(invitation.InvitationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var accepted = await response.Content.ReadDataFromJsonAsync<AcceptedInvitationResponseDto>();
        accepted!.IsCreated.Should().BeTrue();
        accepted.OrganizationId.Should().Be(OrganizationId);
        accepted.Role.Should().Be(nameof(UserRole.User));

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var userEntity = await context.Users.FirstAsync(x => x.Id == accepted.UserId);
        userEntity.OrganizationId.Should().Be(OrganizationId);
        userEntity.EmailConfirmed.Should().BeTrue();

        var invitationEntity = await context.Invitations.FirstAsync(x => x.InvitationId == invitation.InvitationId);
        invitationEntity.AcceptedAt.Should().NotBeNull();
        invitationEntity.AcceptedByUserId.Should().Be(accepted.UserId);
    }

    [Fact]
    public async Task AcceptInvitation_TheNewJoinerSignsIn_ShouldReachTheirNewOrganization()
    {
        // Arrange
        await AuthenticateAsManagerAsync();
        var invitation = await InviteAsync();

        await AcceptAsync(invitation.InvitationToken);

        var inviteeClient = testFixture.CreateClient();

        // Act
        await AuthenticationHelper.SetTokenAsync(inviteeClient, InviteeEmail, InviteePassword);

        var response = await inviteeClient.GetAsync("/cases/my");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AcceptInvitation_UsedTwice_ShouldReturnBadRequestTheSecondTime()
    {
        // Arrange
        await AuthenticateAsManagerAsync();
        var invitation = await InviteAsync();

        await AcceptAsync(invitation.InvitationToken);

        // Act
        var response = await AcceptAsync(invitation.InvitationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AcceptInvitation_AddressAlreadyBelongsToAnotherOrganization_ShouldReturnConflictSayingSoInAField()
    {
        // Arrange
        await AuthenticateAsManagerAsync();
        var invitation = await InviteAsync();

        await AuthenticationHelper.CreateUserAsync(
            testFixture.Services, InviteeEmail, InviteePassword, [UserRole.User], OtherOrganizationId);

        // Act
        var response = await AcceptAsync(invitation.InvitationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problemDetails!.Extensions.Should().ContainKey("sameOrganization");
        ((JsonElement)problemDetails.Extensions["sameOrganization"]!).GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task CreateInvitation_AddressAlreadyBelongsToThisOrganization_ShouldSaySoInTheSameField()
    {
        // Arrange
        await AuthenticateAsManagerAsync();

        await AuthenticationHelper.CreateUserAsync(
            testFixture.Services, InviteeEmail, InviteePassword, [UserRole.User], OrganizationId);

        // Act
        var response = await _httpClient.PostAsJsonAsync("/invitations", CreateRequest());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        ((JsonElement)problemDetails!.Extensions["sameOrganization"]!).GetBoolean().Should().BeTrue();
    }

    private static CreateInvitationRequestBodyDto CreateBody(string role = nameof(UserRole.User))
    {
        return new CreateInvitationRequestBodyDto
        {
            Email = InviteeEmail,
            Role = role,
            SubOrganizationId = null
        };
    }

    private async Task AuthenticateAsPlatformOperatorAsync()
    {
        await AuthenticationHelper.CreateUserAsync(
            testFixture.Services, Email, Password, [UserRole.User, UserRole.Admin], SystemConstants.DefaultOrganization);

        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
    }

    private static CreateInvitationRequestDto CreateRequest(string role = nameof(UserRole.User))
    {
        return new CreateInvitationRequestDto
        {
            Email = InviteeEmail,
            Role = role,
            SubOrganizationId = null
        };
    }

    private async Task<IssuedInvitationResponseDto> InviteAsync(string role = nameof(UserRole.User))
    {
        var response = await _httpClient.PostAsJsonAsync("/invitations", CreateRequest(role));

        return (await response.Content.ReadDataFromJsonAsync<IssuedInvitationResourceDto>())!.Invitation;
    }

    private Task<HttpResponseMessage> AcceptAsync(string token)
    {
        var anonymousClient = testFixture.CreateClient();

        return anonymousClient.PostAsJsonAsync("/invitations/accept", new AcceptInvitationRequestDto
        {
            Token = token,
            UserName = InviteeEmail,
            Password = InviteePassword
        });
    }

    private async Task AuthenticateAsManagerAsync()
    {
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
    }

    private static readonly Guid OtherOrganizationId = new("2f2f5ec4-6d38-4a4d-9d4f-d0b0d45f4d67");

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
