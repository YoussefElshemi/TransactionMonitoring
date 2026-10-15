using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Nodes;
using AutoFixture;
using BehaviouralTests.TestHelpers;
using Core.Enums;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentAssertions;
using Infrastructure;
using Infrastructure.Configurations;
using Infrastructure.Entities;
using Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Constants;
using Presentation.Models.Alerts;
using Presentation.Models.Cases;
using Presentation.Models.Decisions;
using Presentation.Models.PartyMappings;
using Presentation.Models.Rules;
using UnitTests.TestHelpers.FakeObjects.Core.Models.ApiClients;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Users;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

namespace BehaviouralTests.Tests.Controllers;

public class AlertGroupingTests(TestFixture testFixture) : IClassFixture<TestFixture>, IAsyncLifetime
{
    private readonly Fixture _fixture = AutoFixtureHelper.Create();
    private readonly HttpClient _httpClient = testFixture.CreateClient();
    private readonly HttpClient _ingestClient = testFixture.CreateClient();

    private const string Email = "username@mail.com";
    private const string Password = "P@$$w0rd";
    private static readonly Guid OrganizationId = new("c4f1d5b2-9e77-4a31-8f0c-7d2a6b3e5410");

    [Fact]
    public async Task Evaluate_GivenRepeatedMatchesForOneSubject_ShouldSuppressTheDuplicates()
    {
        // Arrange
        await ArrangeAsync();
        await CreateRuleAsync("Large payment");

        // Act
        await EvaluateAsync(20000);
        await EvaluateAsync(20000);
        await EvaluateAsync(20000);

        // Assert
        var alerts = await ListAlertsAsync();

        alerts.Should().HaveCount(3);
        alerts.Count(x => x.Alert.Status == nameof(AlertStatus.Open)).Should().Be(1);
        alerts.Count(x => x.Alert.Status == nameof(AlertStatus.Suppressed)).Should().Be(2);
        alerts.Where(x => x.Alert.Status == nameof(AlertStatus.Suppressed))
            .Should().OnlyContain(x => x.Alert.SuppressionReason == nameof(AlertSuppressionReason.DuplicateInWindow));
    }

    [Fact]
    public async Task Evaluate_GivenASuppressedDuplicate_ShouldStillRecordTheDecision()
    {
        // Arrange
        await ArrangeAsync();
        await CreateRuleAsync("Large payment");

        // Act
        await EvaluateAsync(20000);
        await EvaluateAsync(20000);

        // Assert
        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var decisions = await context.Decisions
            .Where(x => x.OrganizationId == OrganizationId)
            .ToListAsync();

        decisions.Should().HaveCount(2);
        decisions.Should().OnlyContain(x => x.StatusId == (int)DecisionStatus.Held);
    }

    [Fact]
    public async Task Evaluate_GivenRepeatedMatchesForOneSubject_ShouldOpenOnlyOneCase()
    {
        // Arrange
        await ArrangeAsync();
        await CreateRuleAsync("Large payment");

        // Act
        await EvaluateAsync(20000);
        await EvaluateAsync(20000);
        await EvaluateAsync(20000);

        // Assert
        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        context.Cases.Count(x => x.OrganizationId == OrganizationId).Should().Be(1);
    }

    [Fact]
    public async Task Evaluate_GivenASuppressedDuplicate_ShouldAttachItToTheOpenCase()
    {
        // Arrange
        await ArrangeAsync();
        await CreateRuleAsync("Large payment");

        // Act
        await EvaluateAsync(20000);
        await EvaluateAsync(20000);

        // Assert
        var alerts = await ListAlertsAsync();
        var caseIds = alerts.Select(x => x.Alert.CaseId).Distinct().ToList();

        caseIds.Should().ContainSingle();
        caseIds[0].Should().NotBeNull();
    }

    [Fact]
    public async Task Evaluate_GivenASecondSubject_ShouldNotSuppressAgainstTheFirst()
    {
        // Arrange
        await ArrangeAsync();
        await CreateRuleAsync("Large payment");

        // Act
        await EvaluateAsync(20000);
        await EvaluateAsync(20000, sender: "ACC-2");

        // Assert
        var alerts = await ListAlertsAsync();

        alerts.Should().HaveCount(2);
        alerts.Should().OnlyContain(x => x.Alert.Status == nameof(AlertStatus.Open));

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        context.Cases.Count(x => x.OrganizationId == OrganizationId).Should().Be(2);
    }

    [Fact]
    public async Task ResolveCase_GivenSeveralHeldPayments_ShouldSettleAllOfThem()
    {
        // Arrange
        await ArrangeAsync();
        await CreateRuleAsync("Large payment");

        await EvaluateAsync(20000);
        await EvaluateAsync(20000);
        await EvaluateAsync(20000);

        var caseId = await OnlyCaseIdAsync();
        await AssignAsync(caseId);

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/cases/{caseId}/resolve", new ResolveCaseRequestBodyDto
        {
            Status = nameof(CaseStatus.Rejected),
            Disposition = nameof(CaseDisposition.TruePositive),
            Note = "Confirmed mule account"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var decisions = await context.Decisions
            .Where(x => x.OrganizationId == OrganizationId)
            .ToListAsync();

        decisions.Should().HaveCount(3);
        decisions.Should().OnlyContain(x => x.StatusId == (int)DecisionStatus.Blocked);
        decisions.Should().OnlyContain(x => x.ResolvedAt != null);
    }

    [Fact]
    public async Task ResolveCase_ShouldCloseTheAlertsUnderIt()
    {
        // Arrange
        await ArrangeAsync();
        await CreateRuleAsync("Large payment");

        await EvaluateAsync(20000);
        await EvaluateAsync(20000);

        var caseId = await OnlyCaseIdAsync();
        await AssignAsync(caseId);

        // Act
        await _httpClient.PostAsJsonAsync($"/cases/{caseId}/resolve", new ResolveCaseRequestBodyDto
        {
            Status = nameof(CaseStatus.Approved),
            Disposition = nameof(CaseDisposition.FalsePositive)
        });

        // Assert
        var alerts = await ListAlertsAsync();

        alerts.Should().NotContain(x => x.Alert.Status == nameof(AlertStatus.Open));
        alerts.Where(x => x.Alert.Status == nameof(AlertStatus.Closed))
            .Should().OnlyContain(x => x.Alert.ClosedAt != null);
    }

    [Fact]
    public async Task Evaluate_AfterTheCaseWasResolved_ShouldOpenANewCase()
    {
        // Arrange
        await ArrangeAsync();
        await CreateRuleAsync("Large payment");

        await EvaluateAsync(20000);

        var caseId = await OnlyCaseIdAsync();
        await AssignAsync(caseId);
        await _httpClient.PostAsJsonAsync($"/cases/{caseId}/resolve", new ResolveCaseRequestBodyDto
        {
            Status = nameof(CaseStatus.Approved),
            Disposition = nameof(CaseDisposition.FalsePositive)
        });

        // Act
        await EvaluateAsync(20000);

        // Assert
        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        context.Cases.Count(x => x.OrganizationId == OrganizationId).Should().Be(2);
    }

    [Fact]
    public async Task ResolveDecision_ShouldSettleOnePaymentAndLeaveTheOthersHeld()
    {
        // Arrange
        await ArrangeAsync();
        await CreateRuleAsync("Large payment");

        await EvaluateAsync(20000);
        await EvaluateAsync(20000);

        var decisionId = await FirstDecisionIdAsync();

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/decisions/{decisionId}/resolve", new ResolveDecisionRequestBodyDto
        {
            Status = nameof(CaseStatus.Approved)
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var decisions = await context.Decisions
            .Where(x => x.OrganizationId == OrganizationId)
            .ToListAsync();

        decisions.Count(x => x.StatusId == (int)DecisionStatus.Released).Should().Be(1);
        decisions.Count(x => x.StatusId == (int)DecisionStatus.Held).Should().Be(1);
    }

    [Fact]
    public async Task Evaluate_GivenNoMatch_ShouldRaiseNoAlert()
    {
        // Arrange
        await ArrangeAsync();
        await CreateRuleAsync("Large payment");

        // Act
        await EvaluateAsync(100);

        // Assert
        var alerts = await ListAlertsAsync();

        alerts.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_GivenTheSameExternalReferenceTwice_ShouldNotRaiseTheAlertTwice()
    {
        // Arrange
        await ArrangeAsync();
        await CreateRuleAsync("Large payment");

        var payload = Payload(20000, "ACC-1", "IBAN-1");

        // Act
        await ReEvaluateAsync(payload);
        await ReEvaluateAsync(payload);

        // Assert
        var alerts = await ListAlertsAsync();

        alerts.Should().ContainSingle();
    }

    [Fact]
    public async Task ListCaseAlerts_ShouldReturnOnlyTheAlertsUnderThatCase()
    {
        // Arrange
        await ArrangeAsync();
        await CreateRuleAsync("Large payment");

        await EvaluateAsync(20000);
        await EvaluateAsync(20000, sender: "ACC-2");

        var caseId = await FirstCaseIdAsync();

        // Act
        var response = await _httpClient.GetAsync($"/cases/{caseId}/alerts");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var alerts = await response.Content.ReadDataFromJsonAsync<AlertResourceDto[]>();
        alerts.Should().ContainSingle();
        alerts![0].Alert.CaseId.Should().Be(caseId);
    }

    [Fact]
    public async Task ExpireHold_ShouldCloseTheAlertsUnderTheExpiredCase()
    {
        // Arrange
        await ArrangeAsync();
        await CreateRuleAsync("Large payment");

        await EvaluateAsync(20000);
        await EvaluateAsync(20000);

        var caseId = await OnlyCaseIdAsync();

        await ForceHoldsDueAsync();

        using var scope = testFixture.Services.CreateScope();
        var decisionService = scope.ServiceProvider.GetRequiredService<IDecisionService>();

        // Act
        await decisionService.ExpireDueAsync(CancellationToken.None);

        // Assert
        using var assertScope = testFixture.Services.CreateScope();
        var context = assertScope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var caseEntity = await context.Cases.FirstAsync(x => x.CaseId == caseId);
        caseEntity.StatusId.Should().Be((int)CaseStatus.Expired);

        var alertEntities = await context.Alerts.Where(x => x.CaseId == caseId).ToListAsync();

        alertEntities.Should().NotBeEmpty();

        alertEntities.Should().NotContain(x => x.StatusId == (int)AlertStatus.Open);
        alertEntities.Should().Contain(x => x.StatusId == (int)AlertStatus.Closed);
        alertEntities.Should().OnlyContain(x => x.ClosedAt != null);
    }

    private async Task ForceHoldsDueAsync()
    {
        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(-1);
        var verdictId = (int)HoldExpiryVerdict.Expire;
        var heldStatusId = (int)DecisionStatus.Held;

        await context.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE \"Service\".\"Decisions\" SET \"ExpiresAt\" = {expiresAt}, \"HoldExpiryVerdictId\" = {verdictId} WHERE \"OrganizationId\" = {OrganizationId} AND \"StatusId\" = {heldStatusId}");
    }

    [Fact]
    public async Task Evaluate_GivenGroupingDisabled_ShouldOpenACasePerPayment()
    {
        // Arrange
        await ArrangeAsync();
        await SetAlertPolicyAsync("""{"caseGroupingEnabled":false,"deduplicationEnabled":false}""");

        await CreateRuleAsync("Large payment");

        // Act
        await EvaluateAsync(20000);
        await EvaluateAsync(20000);

        // Assert
        (await CaseCountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task Evaluate_GivenSuppressionOnAndGroupingOff_ShouldStillSuppressWithoutGrouping()
    {
        // Arrange
        await ArrangeAsync();
        await SetAlertPolicyAsync("""{"caseGroupingEnabled":false,"deduplicationEnabled":true}""");

        await CreateRuleAsync("Large payment");

        // Act
        await EvaluateAsync(20000);
        await EvaluateAsync(20000);

        // Assert
        var alerts = await ListAlertsAsync();

        alerts.Count(x => x.Alert.Status == nameof(AlertStatus.Suppressed)).Should().Be(1);

        (await CaseCountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Evaluate_GivenDeduplicationDisabled_ShouldRaiseEveryRepeat()
    {
        // Arrange
        await ArrangeAsync();
        await SetAlertPolicyAsync("""{"caseGroupingEnabled":true,"deduplicationEnabled":false}""");

        await CreateRuleAsync("Large payment");

        // Act
        await EvaluateAsync(20000);
        await EvaluateAsync(20000);

        // Assert
        var alerts = await ListAlertsAsync();

        alerts.Should().HaveCount(2);
        alerts.Should().OnlyContain(x => x.Alert.Status == nameof(AlertStatus.Open));

        (await CaseCountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Evaluate_GivenTheAlertCapIsReached_ShouldOpenAFreshCase()
    {
        // Arrange
        await ArrangeAsync();
        await SetAlertPolicyAsync("""{"caseGroupingEnabled":true,"deduplicationEnabled":false,"maxAlertsPerCase":2}""");

        await CreateRuleAsync("Large payment");

        // Act
        await EvaluateAsync(20000);
        await EvaluateAsync(20000);
        await EvaluateAsync(20000);

        // Assert
        (await CaseCountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task Evaluate_GivenPartyScope_ShouldGroupDifferentRulesOntoOneCase()
    {
        // Arrange
        await ArrangeAsync();
        await SetAlertPolicyAsync("""{"caseGroupingEnabled":true}""");

        await CreateRuleAsync("Large payment");
        await CreateRuleAsync("Tiny payment", RuleCondition.LessThan, 5000);

        // Act
        await EvaluateAsync(20000);
        await EvaluateAsync(3000);

        // Assert
        (await CaseCountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Evaluate_GivenPartyAndRuleScope_ShouldOpenACasePerRule()
    {
        // Arrange
        await ArrangeAsync();
        await SetAlertPolicyAsync("""{"caseGroupingEnabled":true,"caseGroupingScope":"PartyAndRule"}""");

        await CreateRuleAsync("Large payment");
        await CreateRuleAsync("Tiny payment", RuleCondition.LessThan, 5000);

        // Act
        await EvaluateAsync(20000);
        await EvaluateAsync(3000);

        // Assert
        (await CaseCountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task UpdateAlertPolicy_GivenAZeroAlertCap_ShouldReturnBadRequest()
    {
        // Arrange
        await ArrangeAsync();

        // Act
        var response = await PatchAlertPolicyAsync("""{"maxAlertsPerCase":0}""");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateAlertPolicy_GivenAnUnknownScope_ShouldReturnBadRequest()
    {
        // Arrange
        await ArrangeAsync();

        // Act
        var response = await PatchAlertPolicyAsync("""{"caseGroupingScope":"Everything"}""");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // Raw JSON: Patch<T> needs the API's converter, which the test client doesn't register.
    private async Task SetAlertPolicyAsync(string json)
    {
        var response = await PatchAlertPolicyAsync(json);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    private Task<HttpResponseMessage> PatchAlertPolicyAsync(string json)
    {
        return _httpClient.PatchAsync(
            "/organizations/my/alert-policy",
            new StringContent(json, Encoding.UTF8, "application/json"));
    }

    private async Task<int> CaseCountAsync()
    {
        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        return await context.Cases.CountAsync(x => x.OrganizationId == OrganizationId);
    }

    private async Task<AlertResourceDto[]> ListAlertsAsync()
    {
        var response = await _httpClient.GetAsync("/alerts");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        return (await response.Content.ReadDataFromJsonAsync<AlertResourceDto[]>())!;
    }

    private async Task AssignAsync(Guid caseId)
    {
        var response = await _httpClient.PostAsJsonAsync($"/cases/{caseId}/assign", new AssignCaseRequestBodyDto());

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    private async Task<Guid> OnlyCaseIdAsync()
    {
        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var cases = await context.Cases.Where(x => x.OrganizationId == OrganizationId).ToListAsync();
        cases.Should().ContainSingle();

        return cases[0].CaseId;
    }

    private async Task<Guid> FirstCaseIdAsync()
    {
        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var caseEntity = await context.Cases
            .Where(x => x.OrganizationId == OrganizationId)
            .OrderBy(x => x.CreatedAt)
            .FirstAsync();

        return caseEntity.CaseId;
    }

    private async Task<Guid> FirstDecisionIdAsync()
    {
        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var decisionEntity = await context.Decisions
            .Where(x => x.OrganizationId == OrganizationId)
            .OrderBy(x => x.CreatedAt)
            .FirstAsync();

        return decisionEntity.DecisionId;
    }

    private Task<HttpResponseMessage> EvaluateAsync(int amount, string sender = "ACC-1", string beneficiary = "IBAN-1")
    {
        return SendEvaluateAsync(Payload(amount, sender, beneficiary), null);
    }

    private Task<HttpResponseMessage> ReEvaluateAsync(JsonObject payload)
    {
        return SendEvaluateAsync(payload, "PMT-1");
    }

    private async Task<HttpResponseMessage> SendEvaluateAsync(JsonObject payload, string? transactionReference)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/rule-sets/evaluate")
        {
            Content = JsonContent.Create(payload)
        };

        request.Headers.Add(HeaderNames.CreateCase, "true");

        if (transactionReference is not null)
        {
            request.Headers.Add(HeaderNames.TransactionReference, transactionReference);
        }

        var response = await _ingestClient.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        return response;
    }

    private static JsonObject Payload(int amount, string sender, string beneficiary)
    {
        return JsonNode.Parse(
            $$"""
              {
                "amount": {{amount}},
                "beneficiaryAccount": "{{beneficiary}}",
                "sender": { "accountId": "{{sender}}", "name": "Ada" },
                "parties": [ { "iban": "{{beneficiary}}", "name": "Grace" } ]
              }
              """)!.AsObject();
    }

    private async Task CreateRuleAsync(
        string friendlyName,
        RuleCondition condition = RuleCondition.GreaterThan,
        int value = 10000)
    {
        var ruleSetId = await ExistingRuleSetIdAsync();

        var response = await _httpClient.PostAsJsonAsync($"/rule-sets/{ruleSetId}/rules", new CreateRuleRequestBodyDto
        {
            FriendlyName = friendlyName,
            IsEnabled = true,
            Action = nameof(RuleAction.Review),
            Severity = nameof(RuleSeverity.High),
            RuleBody = new RuleBodyDto
            {
                Field = "amount",
                Condition = condition.ToString(),
                Value = value
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
