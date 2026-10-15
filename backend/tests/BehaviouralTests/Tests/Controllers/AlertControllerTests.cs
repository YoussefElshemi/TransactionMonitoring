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
using Presentation.Models.Alerts;
using Presentation.Models.Common;
using Presentation.Models.PartyMappings;
using Presentation.Models.Rules;
using UnitTests.TestHelpers.FakeObjects.Core.Models.ApiClients;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Users;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

namespace BehaviouralTests.Tests.Controllers;

public class AlertControllerTests(TestFixture testFixture) : IClassFixture<TestFixture>, IAsyncLifetime
{
    private readonly Fixture _fixture = AutoFixtureHelper.Create();
    private readonly HttpClient _httpClient = testFixture.CreateClient();
    private readonly HttpClient _ingestClient = testFixture.CreateClient();

    private const string Email = "alerts@mail.com";
    private const string Password = "P@$$w0rd";
    private static readonly Guid OrganizationId = new("a1b2c3d4-1111-4222-8333-444455556666");

    [Fact]
    public async Task ListAlerts_ShouldReturnTheAlertsWithPagingMetadata()
    {
        // Arrange
        await ArrangeAsync();
        await CreateRuleAsync("Large payment", RuleSeverity.High);

        await EvaluateAsync(20000);
        await EvaluateAsync(20000);

        // Act
        var response = await _httpClient.GetAsync("/alerts");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var alerts = await response.Content.ReadFromJsonAsync<PagedResponseDto<AlertResourceDto>>();

        alerts!.Data.Should().HaveCount(2);
        alerts.Metadata.PageNumber.Should().Be(1);
        alerts.Metadata.PageSize.Should().Be(10);
        alerts.Metadata.TotalCount.Should().Be(2);
        alerts.Metadata.HasMore.Should().BeFalse();

        alerts.Data.Should().OnlyContain(x => x.Alert.OrganizationId == OrganizationId);
    }

    [Fact]
    public async Task ListAlerts_ShouldReturnTheRuleAndTransactionAlongsideTheAlert()
    {
        // Arrange
        await ArrangeAsync();
        await CreateRuleAsync("Large payment", RuleSeverity.High);

        await EvaluateAsync(20000);

        // Act
        var alerts = await ListAsync("/alerts");

        // Assert
        var resource = alerts.Should().ContainSingle().Subject;

        resource.Rule.Should().NotBeNull();
        resource.Transaction.Should().NotBeNull();
        resource.Alert.RuleVersionId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ListAlerts_GivenAStatusFilter_ShouldReturnOnlyThatStatus()
    {
        // Arrange
        await ArrangeAsync();
        await CreateRuleAsync("Large payment", RuleSeverity.High);

        await EvaluateAsync(20000);
        await EvaluateAsync(20000);
        await EvaluateAsync(20000);

        // Act
        var open = await ListAsync($"/alerts?Status={nameof(AlertStatus.Open)}");
        var suppressed = await ListAsync($"/alerts?Status={nameof(AlertStatus.Suppressed)}");

        // Assert
        open.Should().ContainSingle();
        open.Should().OnlyContain(x => x.Alert.Status == nameof(AlertStatus.Open));

        suppressed.Should().HaveCount(2);
        suppressed.Should().OnlyContain(x => x.Alert.Status == nameof(AlertStatus.Suppressed));
    }

    [Fact]
    public async Task ListAlerts_GivenASeverityFilter_ShouldReturnOnlyThatSeverity()
    {
        // Arrange
        await ArrangeAsync();
        await CreateRuleAsync("Large payment", RuleSeverity.High);
        await CreateRuleAsync("Very large payment", RuleSeverity.Critical, 50000);

        await EvaluateAsync(80000);

        // Act
        var critical = await ListAsync($"/alerts?Severity={nameof(RuleSeverity.Critical)}");
        var high = await ListAsync($"/alerts?Severity={nameof(RuleSeverity.High)}");

        // Assert
        critical.Should().ContainSingle();
        critical.Should().OnlyContain(x => x.Alert.Severity == nameof(RuleSeverity.Critical));

        high.Should().ContainSingle();
        high.Should().OnlyContain(x => x.Alert.Severity == nameof(RuleSeverity.High));
    }

    [Fact]
    public async Task ListAlerts_GivenARuleFilter_ShouldReturnOnlyThatRulesAlerts()
    {
        // Arrange
        await ArrangeAsync();
        await CreateRuleAsync("Large payment", RuleSeverity.High);
        await CreateRuleAsync("Very large payment", RuleSeverity.Critical, 50000);

        await EvaluateAsync(80000);

        var all = await ListAsync("/alerts");
        var ruleId = all[0].Alert.RuleId;

        // Act
        var filtered = await ListAsync($"/alerts?RuleId={ruleId}");

        // Assert
        filtered.Should().NotBeEmpty();
        filtered.Should().OnlyContain(x => x.Alert.RuleId == ruleId);
        filtered.Should().HaveCountLessThan(all.Length);
    }

    [Fact]
    public async Task ListAlerts_GivenAPartyFilter_ShouldReturnOnlyThatSubjectsAlerts()
    {
        // Arrange
        await ArrangeAsync();
        await CreateRuleAsync("Large payment", RuleSeverity.High);

        await EvaluateAsync(20000);
        await EvaluateAsync(20000, "ACC-2");

        var all = await ListAsync("/alerts");
        var partyId = all[0].Alert.PartyId!.Value;

        // Act
        var filtered = await ListAsync($"/alerts?PartyId={partyId}");

        // Assert
        all.Should().HaveCount(2);
        filtered.Should().ContainSingle();
        filtered.Should().OnlyContain(x => x.Alert.PartyId == partyId);
    }

    [Fact]
    public async Task ListAlerts_GivenAPageSize_ShouldPage()
    {
        // Arrange
        await ArrangeAsync();
        await CreateRuleAsync("Large payment", RuleSeverity.High);

        await EvaluateAsync(20000);
        await EvaluateAsync(20000);
        await EvaluateAsync(20000);

        // Act
        var response = await _httpClient.GetAsync("/alerts?PageNumber=1&PageSize=2");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var alerts = await response.Content.ReadFromJsonAsync<PagedResponseDto<AlertResourceDto>>();

        alerts!.Data.Should().HaveCount(2);
        alerts.Metadata.PageCount.Should().Be(2);
        alerts.Metadata.TotalCount.Should().Be(3);
        alerts.Metadata.HasMore.Should().BeTrue();
    }

    [Theory]
    [InlineData("Status=Unknown")]
    [InlineData("Severity=Unknown")]
    [InlineData("PageSize=0")]
    public async Task ListAlerts_GivenAnInvalidFilter_ShouldReturnBadRequest(string query)
    {
        // Arrange
        await ArrangeAsync();

        // Act
        var response = await _httpClient.GetAsync($"/alerts?{query}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetAlert_ShouldReturnTheAlert()
    {
        // Arrange
        await ArrangeAsync();
        await CreateRuleAsync("Large payment", RuleSeverity.High);

        await EvaluateAsync(20000);

        var alertId = (await ListAsync("/alerts"))[0].Alert.AlertId;

        // Act
        var response = await _httpClient.GetAsync($"/alerts/{alertId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var alert = await response.Content.ReadDataFromJsonAsync<AlertResourceDto>();

        alert!.Alert.AlertId.Should().Be(alertId);
        alert.Alert.Status.Should().Be(nameof(AlertStatus.Open));
    }

    [Fact]
    public async Task GetAlert_GivenAnUnknownAlert_ShouldReturnNotFound()
    {
        // Arrange
        await ArrangeAsync();

        // Act
        var response = await _httpClient.GetAsync($"/alerts/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ListCaseAlerts_GivenAConflictingQueryStringCaseId_ShouldAnswerForTheRoute()
    {
        // Arrange
        await ArrangeAsync();
        await CreateRuleAsync("Large payment", RuleSeverity.High);

        await EvaluateAsync(20000);
        await EvaluateAsync(20000, "ACC-2");

        var caseIds = await CaseIdsAsync();
        caseIds.Should().HaveCount(2);

        // Act
        var alerts = await ListAsync($"/cases/{caseIds[0]}/alerts?CaseId={caseIds[1]}");

        // Assert
        alerts.Should().NotBeEmpty();
        alerts.Should().OnlyContain(x => x.Alert.CaseId == caseIds[0]);
    }

    private async Task<AlertResourceDto[]> ListAsync(string requestUri)
    {
        var response = await _httpClient.GetAsync(requestUri);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var alerts = await response.Content.ReadFromJsonAsync<PagedResponseDto<AlertResourceDto>>();

        return alerts!.Data;
    }

    private async Task<List<Guid>> CaseIdsAsync()
    {
        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        return await context.Cases
            .Where(x => x.OrganizationId == OrganizationId)
            .OrderBy(x => x.CreatedAt)
            .Select(x => x.CaseId)
            .ToListAsync();
    }

    private async Task EvaluateAsync(int amount, string sender = "ACC-1")
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/rule-sets/evaluate")
        {
            Content = JsonContent.Create(Payload(amount, sender))
        };

        request.Headers.Add(HeaderNames.CreateCase, "true");

        var response = await _ingestClient.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static JsonObject Payload(int amount, string sender)
    {
        return JsonNode.Parse(
            $$"""
              {
                "amount": {{amount}},
                "beneficiaryAccount": "IBAN-1",
                "sender": { "accountId": "{{sender}}", "name": "Ada" },
                "parties": [ { "iban": "IBAN-1", "name": "Grace" } ]
              }
              """)!.AsObject();
    }

    private async Task CreateRuleAsync(string friendlyName, RuleSeverity severity, int threshold = 10000)
    {
        var ruleSetId = await ExistingRuleSetIdAsync();

        var response = await _httpClient.PostAsJsonAsync($"/rule-sets/{ruleSetId}/rules", new CreateRuleRequestBodyDto
        {
            FriendlyName = friendlyName,
            IsEnabled = true,
            Action = nameof(RuleAction.Review),
            Severity = severity.ToString(),
            RuleBody = new RuleBodyDto
            {
                Field = "amount",
                Condition = nameof(RuleCondition.GreaterThan),
                Value = threshold
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
