using System.Net;
using System.Net.Http.Json;
using AutoFixture;
using BehaviouralTests.TestHelpers;
using Core.Constants;
using Core.Enums;
using FluentAssertions;
using Infrastructure.Configurations;
using Infrastructure.Entities;
using Microsoft.AspNetCore.Mvc;
using Presentation.Models.ApiClients;
using Presentation.Models.AuditLogs;
using Presentation.Models.Common;
using Presentation.Models.Invitations;
using Presentation.Models.NotificationSubscriptions;
using Presentation.Models.Usages;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

namespace BehaviouralTests.Tests.Controllers;

public class OrganizationScopedAdminSurfaceTests(TestFixture testFixture) : IClassFixture<TestFixture>, IAsyncLifetime
{
    private readonly Fixture _fixture = AutoFixtureHelper.Create();
    private readonly HttpClient _httpClient = testFixture.CreateClient();

    private const string OperatorEmail = "installation@mail.com";
    private const string ManagerEmail = "supervisor@mail.com";
    private const string InviteeEmail = "newjoiner@mail.com";
    private const string Password = "P@$$w0rd";

    private static readonly Guid OrganizationId = new("8c41d0a7-2e6b-4f39-95c2-1a7d4e0b6f83");
    private static readonly Guid OtherOrganizationId = new("31b9f6e4-7c02-4a8d-b154-6e2f9d3a5c17");

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

    [Theory]
    [InlineData("invitations")]
    [InlineData("api-clients")]
    [InlineData("notification-subscriptions")]
    [InlineData("usages")]
    [InlineData("audit-logs/verification")]
    public async Task Supervisor_NamingAnotherTenant_IsRefusedNamingThePlatformPermission(string path)
    {
        // Arrange
        await AuthenticateAsManagerAsync();

        // Act
        var response = await _httpClient.GetAsync($"/organizations/{OtherOrganizationId}/{path}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problemDetails!.Extensions["requiredPermission"]!.ToString().Should().Be("platform:administer");
    }

    [Theory]
    [InlineData("invitations")]
    [InlineData("api-clients")]
    [InlineData("notification-subscriptions")]
    [InlineData("usages")]
    [InlineData("audit-logs/verification")]
    public async Task Supervisor_NamingTheirOwnTenant_IsAnswered(string path)
    {
        // Arrange
        await AuthenticateAsManagerAsync();

        // Act
        var response = await _httpClient.GetAsync($"/organizations/{OrganizationId}/{path}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("invitations")]
    [InlineData("api-clients")]
    [InlineData("notification-subscriptions")]
    [InlineData("usages")]
    [InlineData("audit-logs/verification")]
    public async Task Operator_WithoutATenantOfTheirOwn_ReachesTheOneNamedInTheRoute(string path)
    {
        // Arrange
        await AuthenticateAsPlatformOperatorAsync();

        // Act
        var response = await _httpClient.GetAsync($"/organizations/{OrganizationId}/{path}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ListOrganizationInvitations_AsOperator_ShowsWhatTheTenantHasOutstanding()
    {
        // Arrange
        await AuthenticateAsPlatformOperatorAsync();
        await IssueInvitationAsync();

        // Act
        var response = await _httpClient.GetAsync($"/organizations/{OrganizationId}/invitations");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var invitations = await response.Content.ReadFromJsonAsync<PagedResponseDto<InvitationResourceDto>>();

        invitations!.Data.Should().ContainSingle();
        invitations.Data[0].Invitation.OrganizationId.Should().Be(OrganizationId);
    }

    [Fact]
    public async Task GetOrganizationInvitation_NamingATenantTheInvitationIsNotIn_ReturnsNotFound()
    {
        // Arrange
        await AuthenticateAsPlatformOperatorAsync();
        var invitation = await IssueInvitationAsync();

        // Act
        var response = await _httpClient.GetAsync($"/organizations/{OtherOrganizationId}/invitations/{invitation.InvitationId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ResendOrganizationInvitation_AsOperator_RetiresTheTokenItReplaces()
    {
        // Arrange
        await AuthenticateAsPlatformOperatorAsync();
        var invitation = await IssueInvitationAsync();

        // Act
        var response = await _httpClient.PostAsync($"/organizations/{OrganizationId}/invitations/{invitation.InvitationId}/resend", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var reissued = (await response.Content.ReadDataFromJsonAsync<IssuedInvitationResourceDto>())!.Invitation;
        reissued.InvitationToken.Should().NotBe(invitation.InvitationToken);
    }

    [Fact]
    public async Task RevokeOrganizationInvitation_AsOperator_TakesItOutOfThePendingList()
    {
        // Arrange
        await AuthenticateAsPlatformOperatorAsync();
        var invitation = await IssueInvitationAsync();

        // Act
        var response = await _httpClient.PostAsync($"/organizations/{OrganizationId}/invitations/{invitation.InvitationId}/revoke", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var pending = await _httpClient.GetAsync($"/organizations/{OrganizationId}/invitations");
        var listed = await pending.Content.ReadFromJsonAsync<PagedResponseDto<InvitationResourceDto>>();

        listed!.Data.Should().BeEmpty();

        var history = await _httpClient.GetAsync($"/organizations/{OrganizationId}/invitations?includeResolved=true");
        var resolved = await history.Content.ReadFromJsonAsync<PagedResponseDto<InvitationResourceDto>>();

        resolved!.Data.Should().ContainSingle();
        resolved.Data[0].Invitation.Status.Should().Be(nameof(InvitationStatus.Revoked));
    }

    [Fact]
    public async Task ListOrganizationApiClients_AsOperator_ShowsTheTenantsIntegrations()
    {
        // Arrange
        await AuthenticateAsPlatformOperatorAsync();
        var apiClientEntity = await CreateApiClientAsync();

        // Act
        var response = await _httpClient.GetAsync($"/organizations/{OrganizationId}/api-clients");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var apiClients = await response.Content.ReadFromJsonAsync<PagedResponseDto<ApiClientResourceDto>>();

        apiClients!.Data.Should().ContainSingle();
        apiClients.Data[0].ApiClient.ApiClientId.Should().Be(apiClientEntity.ApiClientId);
    }

    [Fact]
    public async Task ListOrganizationApiClientSecrets_AsOperator_IsAnswered()
    {
        // Arrange
        await AuthenticateAsPlatformOperatorAsync();
        var apiClientEntity = await CreateApiClientAsync();

        // Act
        var response = await _httpClient.GetAsync($"/organizations/{OrganizationId}/api-clients/{apiClientEntity.ApiClientId}/secrets");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateOrganizationApiClientSecret_IsNotExposedUnderTheOrganizationRoute()
    {
        // Arrange
        await AuthenticateAsPlatformOperatorAsync();
        var apiClientEntity = await CreateApiClientAsync();

        // Act
        var response = await _httpClient.PostAsJsonAsync(
            $"/organizations/{OrganizationId}/api-clients/{apiClientEntity.ApiClientId}/secrets",
            new CreateApiClientSecretRequestBodyDto { ExpiresAt = null });

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed);
    }

    [Fact]
    public async Task CreateOrganizationNotificationSubscription_AsOperator_LandsInTheNamedTenant()
    {
        // Arrange
        await AuthenticateAsPlatformOperatorAsync();

        // Act
        var response = await _httpClient.PostAsJsonAsync(
            $"/organizations/{OrganizationId}/notification-subscriptions",
            new CreateNotificationSubscriptionRequestBodyDto
            {
                Channel = NotificationChannel.Email,
                Destination = "alerts@customer.example",
                MinimumSeverity = RuleSeverity.High,
                NotifyOnBreach = false,
                IsEnabled = true,
                Locale = null
            });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = (await response.Content.ReadDataFromJsonAsync<NotificationSubscriptionResourceDto>())!.NotificationSubscription;

        created.OrganizationId.Should().Be(OrganizationId);
        created.Destination.Should().Be("alerts@customer.example");
    }

    [Fact]
    public async Task CreateOrganizationNotificationSubscription_ForAWebhook_IsStillRefused()
    {
        // Arrange
        await AuthenticateAsPlatformOperatorAsync();

        // Act
        var response = await _httpClient.PostAsJsonAsync(
            $"/organizations/{OrganizationId}/notification-subscriptions",
            new CreateNotificationSubscriptionRequestBodyDto
            {
                Channel = NotificationChannel.Webhook,
                Destination = "https://customer.example/hook",
                MinimumSeverity = RuleSeverity.High,
                NotifyOnBreach = false,
                IsEnabled = true,
                Locale = null
            });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetOrganizationUsage_AsOperator_ReadsTheMeterBehindTheBill()
    {
        // Arrange
        await AuthenticateAsPlatformOperatorAsync();

        var usageEntity = FakeUsageEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!
        };

        await DatabaseHelper.CreateUsages(testFixture.Services, [usageEntity]);

        // Act
        var response = await _httpClient.GetAsync($"/organizations/{OrganizationId}/usages/{usageEntity.UsageId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var usage = await response.Content.ReadDataFromJsonAsync<UsageResponseDto>();

        usage!.UsageId.Should().Be(usageEntity.UsageId);
        usage.Resource.Should().Be(usageEntity.Resource);
    }

    [Fact]
    public async Task GetOrganizationUsage_NamingATenantTheRowIsNotIn_ReturnsNotFound()
    {
        // Arrange
        await AuthenticateAsPlatformOperatorAsync();

        var usageEntity = FakeUsageEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!
        };

        await DatabaseHelper.CreateUsages(testFixture.Services, [usageEntity]);

        // Act
        var response = await _httpClient.GetAsync($"/organizations/{OtherOrganizationId}/usages/{usageEntity.UsageId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task VerifyOrganizationAuditChain_AsOperator_AnswersForTheNamedTenant()
    {
        // Arrange
        await AuthenticateAsPlatformOperatorAsync();

        // Act
        var response = await _httpClient.GetAsync($"/organizations/{OrganizationId}/audit-logs/verification");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var verification = await response.Content.ReadDataFromJsonAsync<AuditChainVerificationResponseDto>();

        verification!.OrganizationId.Should().Be(OrganizationId);
        verification.IsIntact.Should().BeTrue();
    }

    [Fact]
    public async Task ExportOrganizationAuditLogs_AsOperator_ReturnsTheTrailAsACsv()
    {
        // Arrange
        await AuthenticateAsPlatformOperatorAsync();

        // Act
        var response = await _httpClient.GetAsync($"/organizations/{OrganizationId}/audit-logs/export");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/csv");
    }

    [Fact]
    public async Task ExportOrganizationAuditLogs_NamingAnEntityTypeWithoutAnId_IsRefused()
    {
        // Arrange
        await AuthenticateAsPlatformOperatorAsync();

        // Act
        var response = await _httpClient.GetAsync(
            $"/organizations/{OrganizationId}/audit-logs/export?entityType={nameof(AuditableEntityType.ApiClient)}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task OrganizationScopedRoutes_GivenAnOrganizationThatDoesNotExist_ReturnNotFound()
    {
        // Arrange
        await AuthenticateAsPlatformOperatorAsync();

        // Act
        var response = await _httpClient.GetAsync($"/organizations/{Guid.NewGuid()}/usages");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<ApiClientEntity> CreateApiClientAsync()
    {
        var ownerUserEntity = FakeApplicationUserEntity.CreateValid(_fixture);
        ownerUserEntity.OrganizationId = OrganizationId;

        await DatabaseHelper.CreateApplicationUsers(testFixture.Services, [ownerUserEntity]);

        var apiClientEntity = FakeApiClientEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            CreatedByUserId = ownerUserEntity.Id,
            CreatedByUser = null!,
            Scopes = []
        };

        await DatabaseHelper.CreateApiClients(testFixture.Services, [apiClientEntity]);

        return apiClientEntity;
    }

    private async Task<IssuedInvitationResponseDto> IssueInvitationAsync()
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"/organizations/{OrganizationId}/invitations",
            new CreateInvitationRequestBodyDto
            {
                Email = InviteeEmail,
                Role = nameof(UserRole.User),
                SubOrganizationId = null
            });

        return (await response.Content.ReadDataFromJsonAsync<IssuedInvitationResourceDto>())!.Invitation;
    }

    private async Task AuthenticateAsPlatformOperatorAsync()
    {
        await AuthenticationHelper.CreateUserAsync(
            testFixture.Services, OperatorEmail, Password, [UserRole.User, UserRole.Admin], SystemConstants.DefaultOrganization);

        await AuthenticationHelper.SetTokenAsync(_httpClient, OperatorEmail, Password);
    }

    private async Task AuthenticateAsManagerAsync()
    {
        await AuthenticationHelper.CreateUserAsync(
            testFixture.Services, ManagerEmail, Password, [UserRole.User, UserRole.Manager], OrganizationId);

        await AuthenticationHelper.SetTokenAsync(_httpClient, ManagerEmail, Password);
    }
}
