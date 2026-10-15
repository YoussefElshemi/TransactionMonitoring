using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using AutoFixture;
using BehaviouralTests.TestHelpers;
using Core.Enums;
using Core.ValueObjects;
using FluentAssertions;
using Infrastructure;
using Infrastructure.Configurations;
using Infrastructure.Entities;
using Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Constants;
using Presentation.Models.Decisions;
using Presentation.Models.PartyMappings;
using Presentation.Models.Rules;
using UnitTests.TestHelpers.FakeObjects.Core.Models.ApiClients;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Users;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

namespace BehaviouralTests.Tests.Controllers;

public class DecisionControllerTests(TestFixture testFixture) : IClassFixture<TestFixture>, IAsyncLifetime
{
    private readonly Fixture _fixture = AutoFixtureHelper.Create();
    private readonly HttpClient _httpClient = testFixture.CreateClient();
    private readonly HttpClient _ingestClient = testFixture.CreateClient();

    private const string Email = "decisions@mail.com";
    private const string Password = "P@$$w0rd";
    private static readonly Guid OrganizationId = new("b7c8d9e0-2222-4333-8444-555566667777");

    [Fact]
    public async Task ResolveDecision_GivenTheLastHeldPayment_ShouldCloseTheCaseAndItsAlerts()
    {
        // Arrange
        await ArrangeAsync();
        await CreateRuleAsync();

        await EvaluateAsync(20000);
        await EvaluateAsync(20000);

        var decisionIds = await DecisionIdsAsync();
        var caseId = await OnlyCaseIdAsync();

        // Act
        await ResolveAsync(decisionIds[0], CaseStatus.Approved);
        await ResolveAsync(decisionIds[1], CaseStatus.Approved, CaseDisposition.FalsePositive);

        // Assert
        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var caseEntity = await context.Cases.FirstAsync(x => x.CaseId == caseId);
        caseEntity.StatusId.Should().Be((int)CaseStatus.Approved);
        caseEntity.ResolvedAt.Should().NotBeNull();

        var alertEntities = await context.Alerts.Where(x => x.CaseId == caseId).ToListAsync();
        alertEntities.Should().NotBeEmpty();

        alertEntities.Should().NotContain(x => x.StatusId == (int)AlertStatus.Open);
        alertEntities.Should().Contain(x => x.StatusId == (int)AlertStatus.Closed);
        alertEntities.Should().OnlyContain(x => x.ClosedAt != null);
    }

    [Fact]
    public async Task ResolveDecision_GivenAnotherPaymentStillHeld_ShouldLeaveTheCaseOpen()
    {
        // Arrange
        await ArrangeAsync();
        await CreateRuleAsync();

        await EvaluateAsync(20000);
        await EvaluateAsync(20000);

        var decisionIds = await DecisionIdsAsync();
        var caseId = await OnlyCaseIdAsync();

        // Act
        await ResolveAsync(decisionIds[0], CaseStatus.Approved);

        // Assert
        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var caseEntity = await context.Cases.FirstAsync(x => x.CaseId == caseId);
        caseEntity.StatusId.Should().Be((int)CaseStatus.Pending);
        caseEntity.ResolvedAt.Should().BeNull();

        var decisionEntities = await context.Decisions.Where(x => x.CaseId == caseId).ToListAsync();
        decisionEntities.Should().Contain(x => x.StatusId == (int)DecisionStatus.Released);
        decisionEntities.Should().Contain(x => x.StatusId == (int)DecisionStatus.Held);

        var alertEntities = await context.Alerts.Where(x => x.CaseId == caseId).ToListAsync();
        alertEntities.Should().Contain(x => x.StatusId == (int)AlertStatus.Open);
    }

    [Fact]
    public async Task ResolveDecision_GivenABlockedPaymentUnderTheCase_ShouldRejectTheCase()
    {
        // Arrange
        await ArrangeAsync();
        await CreateRuleAsync();

        await EvaluateAsync(20000);
        await EvaluateAsync(20000);

        var decisionIds = await DecisionIdsAsync();
        var caseId = await OnlyCaseIdAsync();

        // Act
        await ResolveAsync(decisionIds[0], CaseStatus.Rejected);
        await ResolveAsync(decisionIds[1], CaseStatus.Approved, CaseDisposition.TruePositive);

        // Assert
        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var caseEntity = await context.Cases.FirstAsync(x => x.CaseId == caseId);
        caseEntity.StatusId.Should().Be((int)CaseStatus.Rejected);
    }

    [Fact]
    public async Task ResolveDecision_ShouldSettleOnlyTheePaymentItNames()
    {
        // Arrange
        await ArrangeAsync();
        await CreateRuleAsync();

        await EvaluateAsync(20000);
        await EvaluateAsync(20000);

        var decisionIds = await DecisionIdsAsync();

        // Act
        await ResolveAsync(decisionIds[0], CaseStatus.Rejected);

        // Assert
        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var resolved = await context.Decisions.FirstAsync(x => x.DecisionId == decisionIds[0]);
        resolved.StatusId.Should().Be((int)DecisionStatus.Blocked);
        resolved.ResolvedByUserId.Should().NotBeNull();

        var untouched = await context.Decisions.FirstAsync(x => x.DecisionId == decisionIds[1]);
        untouched.StatusId.Should().Be((int)DecisionStatus.Held);
        untouched.ResolvedByUserId.Should().BeNull();
    }

    [Fact]
    public async Task ResolveDecision_GivenAnAlreadySettledPayment_ShouldNotChangeIt()
    {
        // Arrange
        await ArrangeAsync();
        await CreateRuleAsync();

        await EvaluateAsync(20000);
        await EvaluateAsync(20000);

        var decisionIds = await DecisionIdsAsync();

        await ResolveAsync(decisionIds[0], CaseStatus.Rejected);

        // Act
        await ResolveAsync(decisionIds[0], CaseStatus.Approved);

        // Assert
        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var decisionEntity = await context.Decisions.FirstAsync(x => x.DecisionId == decisionIds[0]);
        decisionEntity.StatusId.Should().Be((int)DecisionStatus.Blocked);
    }

    [Fact]
    public async Task ResolveDecision_GivenAnUnknownDecision_ShouldReturnNotFound()
    {
        // Arrange
        await ArrangeAsync();

        // Act
        var response = await _httpClient.PostAsJsonAsync(
            $"/decisions/{Guid.NewGuid()}/resolve",
            new ResolveDecisionRequestBodyDto { Status = nameof(CaseStatus.Approved) });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData(nameof(CaseStatus.Pending))]
    [InlineData(nameof(CaseStatus.Assigned))]
    [InlineData(nameof(CaseStatus.Expired))]
    [InlineData("Unknown")]
    public async Task ResolveDecision_GivenAnUnusableStatus_ShouldReturnBadRequest(string status)
    {
        // Arrange
        await ArrangeAsync();
        await CreateRuleAsync();

        await EvaluateAsync(20000);

        var decisionIds = await DecisionIdsAsync();

        // Act
        var response = await _httpClient.PostAsJsonAsync(
            $"/decisions/{decisionIds[0]}/resolve",
            new ResolveDecisionRequestBodyDto { Status = status });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("approved", DecisionStatus.Released)]
    [InlineData("REJECTED", DecisionStatus.Blocked)]
    public async Task ResolveDecision_GivenAnyCasingOfAUsableStatus_ShouldSettleThePayment(string status, DecisionStatus expected)
    {
        // Arrange
        await ArrangeAsync();
        await CreateRuleAsync();

        await EvaluateAsync(20000);

        var decisionIds = await DecisionIdsAsync();

        // Act
        var response = await _httpClient.PostAsJsonAsync(
            $"/decisions/{decisionIds[0]}/resolve",
            new ResolveDecisionRequestBodyDto
            {
                Status = status,
                Disposition = nameof(CaseDisposition.FalsePositive),
                Note = "Reviewed by the desk"
            });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var decisionEntity = await context.Decisions.FirstAsync(x => x.DecisionId == decisionIds[0]);
        decisionEntity.StatusId.Should().Be((int)expected);
    }

    [Fact]
    public async Task ResolveDecision_GivenAlreadySettled_DoesNotDemandAConclusionItWouldDiscard()
    {
        // Arrange
        await ArrangeAsync();
        await CreateRuleAsync();

        await EvaluateAsync(20000);

        var decisionIds = await DecisionIdsAsync();

        await ResolveAsync(decisionIds[0], CaseStatus.Approved, CaseDisposition.FalsePositive);

        // Act
        var response = await _httpClient.PostAsJsonAsync(
            $"/decisions/{decisionIds[0]}/resolve",
            new ResolveDecisionRequestBodyDto { Status = nameof(CaseStatus.Approved) });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ResolveDecision_GivenAReasonCodeOnASettleThatClosesNothing_IsRefused()
    {
        // Arrange
        await ArrangeAsync();
        await CreateRuleAsync();

        await EvaluateAsync(20000);
        await EvaluateAsync(20000);

        var decisionIds = await DecisionIdsAsync();

        decisionIds.Should().HaveCountGreaterThan(1);

        // Act
        var response = await _httpClient.PostAsJsonAsync(
            $"/decisions/{decisionIds[0]}/resolve",
            new ResolveDecisionRequestBodyDto
            {
                Status = nameof(CaseStatus.Approved),
                ReasonCodeId = Guid.NewGuid()
            });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task ResolveAsync(Guid decisionId, CaseStatus status, CaseDisposition? disposition = null)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"/decisions/{decisionId}/resolve",
            new ResolveDecisionRequestBodyDto
            {
                Status = status.ToString(),
                Disposition = disposition?.ToString(),
                Note = "Reviewed by the desk"
            });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    private async Task<List<Guid>> DecisionIdsAsync()
    {
        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        return await context.Decisions
            .Where(x => x.OrganizationId == OrganizationId)
            .OrderBy(x => x.CreatedAt)
            .Select(x => x.DecisionId)
            .ToListAsync();
    }

    private async Task<Guid> OnlyCaseIdAsync()
    {
        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var cases = await context.Cases.Where(x => x.OrganizationId == OrganizationId).ToListAsync();
        cases.Should().ContainSingle();

        return cases[0].CaseId;
    }

    private async Task EvaluateAsync(int amount)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/rule-sets/evaluate")
        {
            Content = JsonContent.Create(Payload(amount))
        };

        request.Headers.Add(HeaderNames.CreateCase, "true");

        var response = await _ingestClient.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static JsonObject Payload(int amount)
    {
        return JsonNode.Parse(
            $$"""
              {
                "amount": {{amount}},
                "beneficiaryAccount": "IBAN-1",
                "sender": { "accountId": "ACC-1", "name": "Ada" },
                "parties": [ { "iban": "IBAN-1", "name": "Grace" } ]
              }
              """)!.AsObject();
    }

    private async Task CreateRuleAsync()
    {
        var ruleSetId = await ExistingRuleSetIdAsync();

        var response = await _httpClient.PostAsJsonAsync($"/rule-sets/{ruleSetId}/rules", new CreateRuleRequestBodyDto
        {
            FriendlyName = "Large payment",
            IsEnabled = true,
            Action = nameof(RuleAction.Review),
            Severity = nameof(RuleSeverity.High),
            RuleBody = new RuleBodyDto
            {
                Field = "amount",
                Condition = nameof(RuleCondition.GreaterThan),
                Value = 10000
            }
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    private async Task<Guid> ExistingRuleSetIdAsync()
    {
        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var ruleSet = await context.RuleSets.FirstAsync(x => x.OrganizationId == OrganizationId);

        return ruleSet.RuleSetId;
    }

    private async Task ArrangeAsync()
    {
        await AuthenticateAsync(UserRole.Manager, UserRole.Admin);
        await ArrangeIngestAsync();
        await SetMappingAsync();
    }

    private async Task SetMappingAsync()
    {
        var body = new SetPartyMappingRequestBodyDto
        {
            Mappings =
            [
                new PartyRoleMappingDto
                {
                    Role = nameof(PartyRole.Remitter),
                    Path = "sender",
                    IdentifierPath = "accountId",
                    DisplayNamePath = "name"
                },
                new PartyRoleMappingDto
                {
                    Role = nameof(PartyRole.Beneficiary),
                    Path = "parties[*]",
                    IdentifierPath = "iban",
                    DisplayNamePath = "name"
                }
            ]
        };

        var response = await _httpClient.PutAsJsonAsync("/organizations/my/party-mapping", body);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task ArrangeIngestAsync()
    {
        var applicationUserEntity = (FakeApplicationUser.CreateValid(_fixture) with
        {
            OrganizationId = new OrganizationId(OrganizationId),
            IsDeleted = new IsDeleted(false)
        }).ToEntity();

        await DatabaseHelper.CreateApplicationUsers(testFixture.Services, [applicationUserEntity]);

        await DatabaseHelper.CreateRuleSets(testFixture.Services, [
            FakeRuleSetEntity.CreateValid(_fixture) with
            {
                OrganizationId = OrganizationId,
                Organization = null!,
                IsEnabled = true,
                IsDeleted = false,
                Rules = []
            }
        ]);

        var createApiClientRequest = FakeCreateApiClientRequest.CreateValid(_fixture) with
        {
            OrganizationId = new OrganizationId(OrganizationId),
            UserId = new ApplicationUserId(applicationUserEntity.Id)
        };

        var createdApiClient = await AuthenticationHelper.CreateApiClientAsync(testFixture.Services, createApiClientRequest);
        await AuthenticationHelper.SetClientCredentialsTokenAsync(_ingestClient, createdApiClient.ApiClient.ClientId, createdApiClient.ClientSecret);
    }

    private async Task AuthenticateAsync(params UserRole[] roles)
    {
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, .. roles], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
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
            }
        };

        await DatabaseHelper.CreateOrganizations(testFixture.Services, organizationEntities);
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}
