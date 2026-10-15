using System.Net;
using System.Net.Http.Json;
using AutoFixture;
using BehaviouralTests.TestHelpers;
using Core.Constants;
using Core.Enums;
using Core.Models.ApiClients;
using Core.ValueObjects;
using FluentAssertions;
using Infrastructure;
using Infrastructure.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Constants;
using Presentation.Extensions;
using Presentation.Models.Common;
using Presentation.Models.Queues;
using Presentation.Models.RuleSets;
using Presentation.Models.Sandbox;
using Presentation.Models.Simulations;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

namespace BehaviouralTests.Tests.Controllers;

public class SandboxModeTests(TestFixture testFixture) : IClassFixture<TestFixture>, IAsyncLifetime
{
    private readonly Fixture _fixture = AutoFixtureHelper.Create();
    private readonly HttpClient _httpClient = testFixture.CreateClient();

    private const string Email = "sandbox@mail.com";
    private const string Password = "P@$$w0rd";
    private static readonly Guid OrganizationId = new("6f2c9a41-08d5-4bb3-9e17-2a5c3d84f0b9");

    private Guid _userId;

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

        var user = await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.Manager], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        _userId = user.Id;
    }

    public Task DisposeAsync()
    {
        _httpClient.Dispose();

        return Task.CompletedTask;
    }

    [Fact]
    public async Task CreateQueue_InTestMode_IsNotVisibleInLiveMode()
    {
        // Arrange
        var createRequest = QueueBody("High value");

        // Act
        var created = await PostAsync($"/organizations/{OrganizationId}/queues", createRequest, Mode.Test);
        var liveList = await GetAsync($"/organizations/{OrganizationId}/queues", Mode.Live);
        var testList = await GetAsync($"/organizations/{OrganizationId}/queues", Mode.Test);

        // Assert
        created.StatusCode.Should().Be(HttpStatusCode.Created);

        var live = await liveList.Content.ReadFromJsonAsync<PagedResponseDto<QueueResourceDto>>();
        var test = await testList.Content.ReadFromJsonAsync<PagedResponseDto<QueueResourceDto>>();

        live!.Data.Should().BeEmpty();
        test!.Data.Should().ContainSingle();
        test.Data[0].Queue.Mode.Should().Be(nameof(Mode.Test));
    }

    [Fact]
    public async Task CreateQueue_InLiveMode_IsNotVisibleInTestMode()
    {
        // Arrange
        var createRequest = QueueBody("High value");

        // Act
        await PostAsync($"/organizations/{OrganizationId}/queues", createRequest, Mode.Live);
        var liveList = await GetAsync($"/organizations/{OrganizationId}/queues", Mode.Live);
        var testList = await GetAsync($"/organizations/{OrganizationId}/queues", Mode.Test);

        // Assert
        var live = await liveList.Content.ReadFromJsonAsync<PagedResponseDto<QueueResourceDto>>();
        var test = await testList.Content.ReadFromJsonAsync<PagedResponseDto<QueueResourceDto>>();

        live!.Data.Should().ContainSingle();
        live.Data[0].Queue.Mode.Should().Be(nameof(Mode.Live));
        test!.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateQueue_WithTheSameNameInBothModes_IsAllowed()
    {
        // Arrange
        var createRequest = QueueBody("High value");

        // Act
        var live = await PostAsync($"/organizations/{OrganizationId}/queues", createRequest, Mode.Live);
        var test = await PostAsync($"/organizations/{OrganizationId}/queues", createRequest, Mode.Test);

        // Assert
        live.StatusCode.Should().Be(HttpStatusCode.Created);
        test.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Request_WithNoModeHeader_IsLive()
    {
        // Act
        var response = await GetAsync($"/organizations/{OrganizationId}/queues", mode: null);

        // Assert
        response.Headers.GetValues(ModeConstants.ModeHeader).Should().ContainSingle(ModeConstants.Live);
    }

    [Fact]
    public async Task CreateApiClient_InTestMode_IssuesVisiblyDistinctKeys()
    {
        // Act
        var created = await AuthenticationHelper.CreateApiClientAsync(testFixture.Services, new CreateApiClientRequest
        {
            FriendlyName = new FriendlyName("sandbox key"),
            UserId = new ApplicationUserId(_userId),
            OrganizationId = new OrganizationId(OrganizationId),
            Scopes = [ApiScope.RulesEvaluate]
        }, Mode.Test);

        // Assert
        created.ApiClient.ClientId.ToString().Should().StartWith("tmc_test_");
        created.ClientSecret.ToString().Should().StartWith("tmsec_test_");
        created.ApiClient.Mode.Should().Be(Mode.Test);
    }

    [Fact]
    public async Task CreateApiClient_InLiveMode_KeepsTheOriginalPrefixes()
    {
        // Act
        var created = await AuthenticationHelper.CreateApiClientAsync(testFixture.Services, new CreateApiClientRequest
        {
            FriendlyName = new FriendlyName("live key"),
            UserId = new ApplicationUserId(_userId),
            OrganizationId = new OrganizationId(OrganizationId),
            Scopes = [ApiScope.RulesEvaluate]
        });

        // Assert
        created.ApiClient.ClientId.ToString().Should().StartWith("tmc_");
        created.ApiClient.ClientId.ToString().Should().NotStartWith("tmc_test_");
        created.ClientSecret.ToString().Should().StartWith("tmsec_");
        created.ClientSecret.ToString().Should().NotStartWith("tmsec_test_");
    }

    [Fact]
    public async Task ApiClientRequest_AskingForLiveWithATestKey_IsStillServedTheSandbox()
    {
        // Arrange
        var created = await AuthenticationHelper.CreateApiClientAsync(testFixture.Services, new CreateApiClientRequest
        {
            FriendlyName = new FriendlyName("sandbox key"),
            UserId = new ApplicationUserId(_userId),
            OrganizationId = new OrganizationId(OrganizationId),
            Scopes = [ApiScope.RuleSetsEvaluate]
        }, Mode.Test);

        using var apiClient = testFixture.CreateClient();

        await AuthenticationHelper.SetClientCredentialsTokenAsync(
            apiClient, created.ApiClient.ClientId, created.ClientSecret);

        apiClient.DefaultRequestHeaders.Add(ModeConstants.ModeHeader, ModeConstants.Live);

        var request = new HttpRequestMessage(HttpMethod.Post, "/rule-sets/evaluate")
        {
            Content = JsonContent.Create(new { amount = 10 })
        };

        // Act
        var response = await apiClient.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.GetValues(ModeConstants.ModeHeader).Should().ContainSingle(ModeConstants.Test);
    }

    [Fact]
    public async Task ResetSandbox_Always_RemovesOnlyTheTestModeRows()
    {
        // Arrange
        await PostAsync($"/organizations/{OrganizationId}/queues", QueueBody("Live desk"), Mode.Live);
        await PostAsync($"/organizations/{OrganizationId}/queues", QueueBody("Sandbox desk"), Mode.Test);

        // Act
        var reset = await PostAsync($"/organizations/{OrganizationId}/sandbox/reset", new { }, Mode.Test);

        // Assert
        reset.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await reset.Content.ReadFromJsonAsync<ResponseDataDto<SandboxResetResponseDto>>();
        body!.Data.TotalDeleted.Should().BeGreaterThan(0);

        await using var scope = testFixture.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var remaining = await context.Queues.IgnoreQueryFilters()
            .Where(x => x.OrganizationId == OrganizationId)
            .Select(x => x.ModeId)
            .ToListAsync();

        remaining.Should().ContainSingle().And.OnlyContain(x => x == (int)Mode.Live);
    }

    [Fact]
    public async Task ResetSandbox_InLiveMode_IsRefused()
    {
        // Act
        var response = await PostAsync($"/organizations/{OrganizationId}/sandbox/reset", new { }, Mode.Live);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task StartSimulation_InLiveMode_IsRefused()
    {
        // Arrange
        var body = new { Scenario = nameof(SimulationScenario.Baseline), PaymentCount = 5 };

        // Act
        var response = await PostAsync($"/organizations/{OrganizationId}/sandbox/simulations", body, Mode.Live);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task StartSimulation_InTestMode_IsAcceptedAndRecordsItsSeed()
    {
        // Arrange
        var body = new { Scenario = nameof(SimulationScenario.Structuring), PaymentCount = 5, Seed = 7 };

        // Act
        var response = await PostAsync($"/organizations/{OrganizationId}/sandbox/simulations", body, Mode.Test);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var created = await response.Content.ReadFromJsonAsync<ResponseDataDto<SimulationResourceDto>>();
        created!.Data.Simulation.Seed.Should().Be(7);
        created.Data.Simulation.TotalPayments.Should().Be(5);
        created.Data.Simulation.Mode.Should().Be(nameof(Mode.Test));
    }

    [Fact]
    public async Task StartSimulation_AskingForMoreThanTheCap_IsRejected()
    {
        // Arrange
        var body = new { Scenario = nameof(SimulationScenario.Baseline), PaymentCount = 500_000 };

        // Act
        var response = await PostAsync($"/organizations/{OrganizationId}/sandbox/simulations", body, Mode.Test);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task StartSimulation_WithAnUnknownScenario_IsRejected()
    {
        // Arrange
        var body = new { Scenario = "chaos", PaymentCount = 5 };

        // Act
        var response = await PostAsync($"/organizations/{OrganizationId}/sandbox/simulations", body, Mode.Test);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetSandboxCredits_Always_ReportsTheHouseAllowanceAndWhatIsLeft()
    {
        // Act
        var response = await GetAsync($"/organizations/{OrganizationId}/sandbox/credits", Mode.Test);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ResponseDataDto<SandboxCreditResponseDto>>();

        body!.Data.Allowance.Should().BeGreaterThan(0);
        body.Data.Used.Should().Be(0);
        body.Data.Remaining.Should().Be(body.Data.Allowance);
        body.Data.Unit.Should().Be("rule-pass");
    }

    [Fact]
    public async Task Evaluate_WithNoSandboxCreditsLeft_IsRefused()
    {
        // Arrange
        var ruleSetId = await CreateRuleSetAsync();

        await SetSandboxAllowanceAsync(0);

        // Act
        var response = await EvaluateAsync(ruleSetId, Mode.Test);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.PaymentRequired);
    }

    [Fact]
    public async Task Evaluate_InLiveMode_IsNeverRefusedForCredits()
    {
        // Arrange
        await SetSandboxAllowanceAsync(0);

        var ruleSetId = await CreateRuleSetAsync(Mode.Live);

        // Act
        var response = await EvaluateAsync(ruleSetId, Mode.Live);

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.PaymentRequired);
    }

    [Fact]
    public async Task StartSimulation_CostingMoreThanIsLeft_IsRefusedBeforeItRuns()
    {
        // Arrange
        await SetSandboxAllowanceAsync(1);

        var body = new { Scenario = nameof(SimulationScenario.Baseline), PaymentCount = 500 };

        // Act
        var response = await PostAsync($"/organizations/{OrganizationId}/sandbox/simulations", body, Mode.Test);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.PaymentRequired);
    }

    [Fact]
    public async Task ResetSandbox_Always_LeavesTheMonthsSpendWhereItIs()
    {
        // Arrange
        var ruleSetId = await CreateRuleSetAsync();

        await EvaluateAsync(ruleSetId, Mode.Test);

        var before = await ReadCreditsAsync();
        before.Used.Should().BeGreaterThan(0);

        // Act
        await PostAsync($"/organizations/{OrganizationId}/sandbox/reset", new { }, Mode.Test);

        // Assert
        var after = await ReadCreditsAsync();

        after.Used.Should().Be(before.Used);
        after.Remaining.Should().Be(before.Remaining);
    }

    [Fact]
    public async Task ResetSandbox_GivenAPopulatedSandbox_EmptiesItInOneGo()
    {
        // Arrange
        var ruleSetId = await CreateRuleSetAsync();

        await PostAsync($"/organizations/{OrganizationId}/queues", QueueBody("Sandbox desk"), Mode.Test);

        var held = await PostAsync(
            $"/rule-sets/{ruleSetId}/evaluate",
            new { amount = 10, reference = SandboxTriggerConstants.Prefix + "hold" },
            Mode.Test);

        held.StatusCode.Should().Be(HttpStatusCode.OK);

        await using (var seeded = testFixture.Services.CreateAsyncScope())
        {
            var context = seeded.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

            var cases = await context.Cases.IgnoreQueryFilters()
                .CountAsync(x => x.OrganizationId == OrganizationId && x.ModeId == (int)Mode.Test);

            cases.Should().BeGreaterThan(0, "the hold trigger should have opened a case to delete");
        }

        // Act
        var reset = await PostAsync($"/organizations/{OrganizationId}/sandbox/reset", new { }, Mode.Test);

        // Assert
        reset.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await reset.Content.ReadFromJsonAsync<ResponseDataDto<SandboxResetResponseDto>>();

        body!.Data.Counts.Should().HaveCountGreaterThan(1, "a populated sandbox spans more than one table");

        await using var scope = testFixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var remaining = new[]
        {
            await db.Transactions.IgnoreQueryFilters().CountAsync(x => x.OrganizationId == OrganizationId && x.ModeId == (int)Mode.Test),
            await db.Decisions.IgnoreQueryFilters().CountAsync(x => x.OrganizationId == OrganizationId && x.ModeId == (int)Mode.Test),
            await db.Cases.IgnoreQueryFilters().CountAsync(x => x.OrganizationId == OrganizationId && x.ModeId == (int)Mode.Test),
            await db.Queues.IgnoreQueryFilters().CountAsync(x => x.OrganizationId == OrganizationId && x.ModeId == (int)Mode.Test)
        };

        remaining.Should().OnlyContain(x => x == 0);
    }

    [Fact]
    public async Task ResetSandbox_Always_LeavesTheOrganizationsTestKeys()
    {
        // Arrange
        var created = await CreateApiClientAsync();

        // Act
        var reset = await PostAsync($"/organizations/{OrganizationId}/sandbox/reset", new { }, Mode.Test);

        // Assert
        reset.StatusCode.Should().Be(HttpStatusCode.OK);

        await using var scope = testFixture.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var survives = await context.ApiClients.IgnoreQueryFilters()
            .AnyAsync(x => x.ApiClientId == created.ApiClient.ApiClientId);

        survives.Should().BeTrue();
    }

    [Theory]
    [InlineData("sandbox")]
    [InlineData("testing")]
    [InlineData("production")]
    public async Task AnyRequest_GivenAModeItDoesNotRecognise_IsRefused(string mode)
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, $"/organizations/{OrganizationId}/queues");
        request.Headers.Add(ModeConstants.ModeHeader, mode);

        // Act
        var response = await _httpClient.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ListWebhookDeliveries_InLiveMode_IsRefused()
    {
        // Act
        var response = await GetAsync($"/organizations/{OrganizationId}/sandbox/webhook-deliveries", Mode.Live);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task PlainSandboxWrite_Always_SpendsFromTheAllowance()
    {
        // Arrange
        var before = await ReadCreditsAsync();

        // Act
        var created = await PostAsync($"/organizations/{OrganizationId}/queues", QueueBody("Metered desk"), Mode.Test);

        // Assert
        created.StatusCode.Should().Be(HttpStatusCode.Created);

        var after = await ReadCreditsAsync();

        after.Used.Should().BeGreaterThan(before.Used);
    }

    [Fact]
    public async Task PlainLiveWrite_Always_SpendsNothingFromTheAllowance()
    {
        // Arrange
        var before = await ReadCreditsAsync();

        // Act
        var created = await PostAsync($"/organizations/{OrganizationId}/queues", QueueBody("Live desk"), Mode.Live);

        // Assert
        created.StatusCode.Should().Be(HttpStatusCode.Created);

        var after = await ReadCreditsAsync();

        after.Used.Should().Be(before.Used);
    }

    [Fact]
    public async Task Logout_WithNoSandboxCreditsLeft_IsNotRefused()
    {
        // Arrange
        await SetSandboxAllowanceAsync(0);

        // Act
        var response = await PostAsync("/auth/logout", new { }, Mode.Test);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var after = await ReadCreditsAsync();

        after.Used.Should().Be(0);
    }

    [Fact]
    public async Task NonModeScopedWrite_WithNoSandboxCreditsLeft_IsNotRefused()
    {
        // Arrange
        await SetSandboxAllowanceAsync(0);

        var request = new HttpRequestMessage(HttpMethod.Patch, "/organizations/my/hold-policy")
        {
            Content = JsonContent.Create(new { holdWindow = 45, holdExpiryVerdict = "Release" })
        };

        request.Headers.Add(ModeConstants.ModeHeader, ModeHeaderHelper.ToHeaderValue(Mode.Test));

        // Act
        var response = await _httpClient.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var after = await ReadCreditsAsync();

        after.Used.Should().Be(0);
    }

    private Task<CreatedApiClient> CreateApiClientAsync()
    {
        return AuthenticationHelper.CreateApiClientAsync(testFixture.Services, new CreateApiClientRequest
        {
            FriendlyName = new FriendlyName("sandbox key"),
            UserId = new ApplicationUserId(_userId),
            OrganizationId = new OrganizationId(OrganizationId),
            Scopes = [ApiScope.RulesEvaluate]
        }, Mode.Test);
    }

    private async Task<SandboxCreditResponseDto> ReadCreditsAsync()
    {
        var response = await GetAsync($"/organizations/{OrganizationId}/sandbox/credits", Mode.Test);
        var body = await response.Content.ReadFromJsonAsync<ResponseDataDto<SandboxCreditResponseDto>>();

        return body!.Data;
    }

    private async Task SetSandboxAllowanceAsync(int allowance)
    {
        await using var scope = testFixture.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        await context.Organizations
            .Where(x => x.OrganizationId == OrganizationId)
            .ExecuteUpdateAsync(x => x.SetProperty(organization => organization.SandboxMonthlyCredits, allowance));
    }

    private Task<HttpResponseMessage> EvaluateAsync(Guid ruleSetId, Mode mode)
    {
        return PostAsync($"/rule-sets/{ruleSetId}/evaluate", new { amount = 10 }, mode);
    }

    private async Task<Guid> CreateRuleSetAsync(Mode mode = Mode.Test)
    {
        var response = await PostAsync(
            "/rule-sets",
            new { FriendlyName = $"Sandbox set {Guid.NewGuid()}", IsEnabled = true },
            mode);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await response.Content.ReadFromJsonAsync<ResponseDataDto<RuleSetResourceDto>>();

        return created!.Data.RuleSet.RuleSetId;
    }

    private static CreateQueueRequestBodyDto QueueBody(string name)
    {
        return new CreateQueueRequestBodyDto
        {
            Name = name,
            RoutingPolicy = nameof(QueueRoutingPolicy.Manual),
            IsEnabled = true
        };
    }

    private Task<HttpResponseMessage> PostAsync<T>(string url, T body, Mode mode)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(body)
        };

        request.Headers.Add(ModeConstants.ModeHeader, ModeHeaderHelper.ToHeaderValue(mode));

        return _httpClient.SendAsync(request);
    }

    private Task<HttpResponseMessage> GetAsync(string url, Mode? mode)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        if (mode.HasValue)
        {
            request.Headers.Add(ModeConstants.ModeHeader, ModeHeaderHelper.ToHeaderValue(mode.Value));
        }

        return _httpClient.SendAsync(request);
    }
}
