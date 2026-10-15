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
using Presentation.Models.PartyMappings;
using Presentation.Models.Rules;
using Presentation.Models.VelocityProfiles;
using UnitTests.TestHelpers.FakeObjects.Core.Models.ApiClients;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Users;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

namespace BehaviouralTests.Tests.Controllers;

public class VelocityRuleEvaluationTests(TestFixture testFixture) : IClassFixture<TestFixture>, IAsyncLifetime
{
    private readonly Fixture _fixture = AutoFixtureHelper.Create();
    private readonly HttpClient _httpClient = testFixture.CreateClient();
    private readonly HttpClient _ingestClient = testFixture.CreateClient();

    private const string Email = "username@mail.com";
    private const string Password = "P@$$w0rd";
    private static readonly Guid OrganizationId = new("bd1c93a4-2f2e-45f6-9a26-6b0a35a3a2d0");

    [Fact]
    public async Task Evaluate_GivenARollingSumOverTheWindow_ShouldFireOnThePaymentThatCrossesTheLimit()
    {
        // Arrange
        await ArrangeAsync();
        await CreateVelocityRuleAsync("Daily total", SumBody(10000));

        // Act
        var first = await EvaluateAsync(4000);
        var second = await EvaluateAsync(4000);
        var third = await EvaluateAsync(4000);

        // Assert
        (await MatchedAsync(first)).Should().BeFalse();
        (await MatchedAsync(second)).Should().BeFalse();
        (await MatchedAsync(third)).Should().BeTrue();
    }

    [Fact]
    public async Task Evaluate_GivenAWindowExcludingTheCurrentPayment_ShouldFireOnePaymentLater()
    {
        // Arrange
        await ArrangeAsync();
        await CreateVelocityRuleAsync("Daily total, prior only", SumBody(10000, includeCurrent: false));

        // Act
        await EvaluateAsync(4000);
        await EvaluateAsync(4000);
        var third = await EvaluateAsync(4000);
        var fourth = await EvaluateAsync(4000);

        // Assert
        (await MatchedAsync(third)).Should().BeFalse();
        (await MatchedAsync(fourth)).Should().BeTrue();
    }

    [Fact]
    public async Task Evaluate_GivenADistinctCountOfBeneficiaries_ShouldFireOnTheThirdDistinctOne()
    {
        // Arrange
        await ArrangeAsync();
        await CreateVelocityRuleAsync("Fan-out", DistinctCountBody(2));

        // Act
        var first = await EvaluateAsync(100, beneficiary: "IBAN-1");
        var repeat = await EvaluateAsync(100, beneficiary: "IBAN-1");
        var third = await EvaluateAsync(100, beneficiary: "IBAN-2");
        var fourth = await EvaluateAsync(100, beneficiary: "IBAN-3");

        // Assert
        (await MatchedAsync(first)).Should().BeFalse();
        (await MatchedAsync(repeat)).Should().BeFalse();
        (await MatchedAsync(third)).Should().BeFalse();
        (await MatchedAsync(fourth)).Should().BeTrue();
    }

    [Fact]
    public async Task Evaluate_GivenAStructuringCount_ShouldFireOnRepeatedAmountsJustUnderTheThreshold()
    {
        // Arrange
        await ArrangeAsync();
        await CreateVelocityRuleAsync("Structuring", StructuringBody(3));

        // Act
        await EvaluateAsync(9500);
        var justUnderTwice = await EvaluateAsync(9200);
        var justUnderThrice = await EvaluateAsync(8100);

        // Assert
        (await MatchedAsync(justUnderTwice)).Should().BeFalse();
        (await MatchedAsync(justUnderThrice)).Should().BeTrue();
    }

    [Fact]
    public async Task Evaluate_GivenAStructuringCount_ShouldNotCountPaymentsWellUnderTheThreshold()
    {
        // Arrange
        await ArrangeAsync();
        await CreateVelocityRuleAsync("Structuring", StructuringBody(3));

        // Act
        await EvaluateAsync(1000);
        await EvaluateAsync(2000);
        var third = await EvaluateAsync(3000);

        // Assert
        (await MatchedAsync(third)).Should().BeFalse();
    }

    [Fact]
    public async Task Evaluate_GivenASecondRemitter_ShouldNotReadTheFirstsHistory()
    {
        // Arrange
        await ArrangeAsync();
        await CreateVelocityRuleAsync("Daily total", SumBody(10000));

        await EvaluateAsync(9000);

        // Act
        var otherParty = await EvaluateAsync(9000, sender: "ACC-2");

        // Assert
        (await MatchedAsync(otherParty)).Should().BeFalse();
    }

    [Fact]
    public async Task Evaluate_ShouldRecordOneObservationPerPartyPerMetric()
    {
        // Arrange
        await ArrangeAsync();

        // Act
        await EvaluateAsync(100);

        // Assert
        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        context.VelocityObservations.Count(x => x.OrganizationId == OrganizationId).Should().Be(4);
    }

    [Fact]
    public async Task Evaluate_GivenTheSameExternalReferenceTwice_ShouldNotRecordTheObservationsTwice()
    {
        // Arrange
        await ArrangeAsync();
        var payload = Payload(100, "ACC-1", "IBAN-1");

        // Act
        await ReEvaluateAsync(payload);
        await ReEvaluateAsync(payload);

        // Assert
        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        context.VelocityObservations.Count(x => x.OrganizationId == OrganizationId).Should().Be(4);
    }

    [Fact]
    public async Task Evaluate_ShouldReduceTheWindowInTheDatabase()
    {
        // Arrange
        await ArrangeAsync();
        await CreateVelocityRuleAsync("Daily total", SumBody(10000));
        await EvaluateAsync(100);

        // Act
        testFixture.CommandCounter.Start();
        await EvaluateAsync(100);
        testFixture.CommandCounter.Stop();

        // Assert
        var observationCommands = testFixture.CommandCounter.Commands
            .Where(x => x.Contains("VelocityObservations", StringComparison.Ordinal))
            .ToList();

        observationCommands.Should().NotBeEmpty();
        observationCommands.Should().Contain(x => x.Contains("GROUP BY", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Evaluate_GivenTheSameExternalReferenceConcurrently_ShouldSucceedAndRecordOneSetOfRows()
    {
        // Arrange
        await ArrangeAsync();
        var payload = Payload(100, "ACC-1", "IBAN-1");

        // Act
        var responses = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => ReEvaluateAsync(payload)));

        // Assert
        responses.Should().OnlyContain(x => x.StatusCode == HttpStatusCode.OK);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        context.Transactions.Count(x => x.OrganizationId == OrganizationId).Should().Be(1);
        context.TransactionParties.Count().Should().Be(2);
        context.VelocityObservations.Count(x => x.OrganizationId == OrganizationId).Should().Be(4);
    }

    [Fact]
    public async Task CreateRule_GivenAMetricTheProfileDoesNotDeclare_ShouldReturnBadRequest()
    {
        // Arrange
        await ArrangeAsync();

        // Act
        var response = await PostRuleAsync("Unknown metric", SumBody(10000, metric: "fee"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateRule_GivenAStructuringCountWithoutAThreshold_ShouldReturnBadRequest()
    {
        // Arrange
        await ArrangeAsync();

        var body = StructuringBody(3) with { };
        body = body with { Velocity = body.Velocity! with { Threshold = null, Band = null } };

        // Act
        var response = await PostRuleAsync("No threshold", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateRule_WithoutStatingWhetherTheCurrentPaymentCounts_ShouldReturnBadRequest()
    {
        // Arrange
        await ArrangeAsync();

        var body = SumBody(10000);
        body = body with { Velocity = body.Velocity! with { IncludeCurrent = null } };

        // Act
        var response = await PostRuleAsync("No stance", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateRule_GivenACalendarWindowCarryingALength_ShouldReturnBadRequest()
    {
        // Arrange
        await ArrangeAsync();

        var body = SumBody(10000);
        body = body with
        {
            Velocity = body.Velocity! with
            {
                Window = new VelocityWindowDto { Kind = nameof(VelocityWindowKind.Calendar), Unit = nameof(VelocityWindowUnit.Day), Length = 3 }
            }
        };

        // Act
        var response = await PostRuleAsync("Calendar with a length", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteVelocityProfile_WhileARuleReadsTheStore_ShouldReturnConflict()
    {
        // Arrange
        await ArrangeAsync();
        await CreateVelocityRuleAsync("Daily total", SumBody(10000));

        // Act
        var response = await _httpClient.DeleteAsync("/organizations/my/velocity-profile");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ListVelocityProfileUsages_ShouldNameTheRulesAndWhatTheyRead()
    {
        // Arrange
        await ArrangeAsync();
        await CreateVelocityRuleAsync("Structuring", StructuringBody(3));

        // Act
        var response = await _httpClient.GetAsync("/organizations/my/velocity-profile/usages");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var usages = await response.Content.ReadDataFromJsonAsync<VelocityProfileUsageResponseDto[]>();
        usages.Should().ContainSingle();
        usages![0].FriendlyName.Should().Be("Structuring");
        usages[0].Metrics.Should().Equal("amount");
        usages[0].Thresholds.Should().Equal("reporting");
    }

    [Fact]
    public async Task TestRule_ShouldCountTheCurrentPayloadWithoutRecordingIt()
    {
        // Arrange
        await ArrangeAsync();

        await EvaluateAsync(100);

        // Act
        var response = await _httpClient.PostAsJsonAsync("/rules/test", new TestRuleRequestDto
        {
            Payload = Payload(11000, "ACC-1", "IBAN-1"),
            RuleBody = SumBody(10000),
            MatchPolarity = nameof(RuleMatchPolarity.MatchesWhenTrue),
            Action = nameof(RuleAction.Flag),
            Severity = nameof(RuleSeverity.Medium)
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadDataFromJsonAsync<TestRuleResponseDto>();
        result!.Matched.Should().BeTrue();

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        context.VelocityObservations.Count(x => x.OrganizationId == OrganizationId).Should().Be(4);
    }

    private static RuleBodyDto SumBody(int limit, bool includeCurrent = true, string metric = "amount")
    {
        return new RuleBodyDto
        {
            Condition = nameof(RuleCondition.GreaterThan),
            Velocity = new VelocityConditionDto
            {
                Role = nameof(PartyRole.Remitter),
                Metric = metric,
                Function = nameof(VelocityFunction.Sum),
                Window = new VelocityWindowDto { Kind = nameof(VelocityWindowKind.Rolling), Unit = nameof(VelocityWindowUnit.Day), Length = 1 },
                IncludeCurrent = includeCurrent,
                Threshold = null,
                Band = null
            },
            Value = limit
        };
    }

    private static RuleBodyDto DistinctCountBody(int limit)
    {
        return new RuleBodyDto
        {
            Condition = nameof(RuleCondition.GreaterThan),
            Velocity = new VelocityConditionDto
            {
                Role = nameof(PartyRole.Remitter),
                Metric = "beneficiaryAccount",
                Function = nameof(VelocityFunction.DistinctCount),
                Window = new VelocityWindowDto { Kind = nameof(VelocityWindowKind.Rolling), Unit = nameof(VelocityWindowUnit.Hour), Length = 1 },
                IncludeCurrent = true,
                Threshold = null,
                Band = null
            },
            Value = limit
        };
    }

    private static RuleBodyDto StructuringBody(int limit)
    {
        return new RuleBodyDto
        {
            Condition = nameof(RuleCondition.GreaterThanEqual),
            Velocity = new VelocityConditionDto
            {
                Role = nameof(PartyRole.Remitter),
                Metric = "amount",
                Function = nameof(VelocityFunction.StructuringCount),
                Window = new VelocityWindowDto { Kind = nameof(VelocityWindowKind.Rolling), Unit = nameof(VelocityWindowUnit.Day), Length = 1 },
                IncludeCurrent = true,
                Threshold = "reporting",
                Band = 0.2m
            },
            Value = limit
        };
    }

    private static async Task<bool> MatchedAsync(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadDataFromJsonAsync<EvaluateAllRulesResourceDto>();

        return body!.Result.Results.Any(x => x.Matched);
    }

    private Task<HttpResponseMessage> EvaluateAsync(int amount, string sender = "ACC-1", string beneficiary = "IBAN-1")
    {
        return _ingestClient.PostAsJsonAsync("/rule-sets/evaluate", Payload(amount, sender, beneficiary));
    }

    private Task<HttpResponseMessage> ReEvaluateAsync(JsonObject payload)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/rule-sets/evaluate")
        {
            Content = JsonContent.Create(payload)
        };

        request.Headers.Add(HeaderNames.TransactionReference, "PMT-1");

        return _ingestClient.SendAsync(request);
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

    private async Task CreateVelocityRuleAsync(string friendlyName, RuleBodyDto ruleBody)
    {
        var response = await PostRuleAsync(friendlyName, ruleBody);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    private async Task<HttpResponseMessage> PostRuleAsync(string friendlyName, RuleBodyDto ruleBody)
    {
        var ruleSetId = await ExistingRuleSetIdAsync();

        return await _httpClient.PostAsJsonAsync($"/rule-sets/{ruleSetId}/rules", new CreateRuleRequestBodyDto
        {
            FriendlyName = friendlyName,
            IsEnabled = true,
            Action = nameof(RuleAction.Flag),
            Severity = nameof(RuleSeverity.Medium),
            RuleBody = ruleBody
        });
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
        await SetProfileAsync();
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

    private async Task SetProfileAsync()
    {
        var body = new SetVelocityProfileRequestBodyDto
        {
            Metrics =
            [
                new VelocityMetricDto { Name = "amount", Path = "amount" },
                new VelocityMetricDto { Name = "beneficiaryAccount", Path = "beneficiaryAccount" }
            ],
            Thresholds = [new VelocityThresholdDto { Name = "reporting", Value = 10000m }]
        };

        var response = await _httpClient.PutAsJsonAsync("/organizations/my/velocity-profile", body);
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
