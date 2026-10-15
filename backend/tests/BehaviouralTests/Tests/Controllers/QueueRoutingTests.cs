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
using Presentation.Models.Cases;
using Presentation.Models.Common;
using Presentation.Models.PartyMappings;
using Presentation.Models.Queues;
using Presentation.Models.Rules;
using UnitTests.TestHelpers.FakeObjects.Core.Models.ApiClients;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Users;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

namespace BehaviouralTests.Tests.Controllers;

public class QueueRoutingTests(TestFixture testFixture) : IClassFixture<TestFixture>, IAsyncLifetime
{
    private readonly Fixture _fixture = AutoFixtureHelper.Create();
    private readonly HttpClient _httpClient = testFixture.CreateClient();
    private readonly HttpClient _analystClient = testFixture.CreateClient();
    private readonly HttpClient _ingestClient = testFixture.CreateClient();

    private const string ManagerEmail = "supervisor@mail.com";
    private const string AnalystEmail = "analyst@mail.com";
    private const string Password = "P@$$w0rd";
    private static readonly Guid OrganizationId = new("8e3a5c17-42bd-4f96-b0d1-6a9c8e2f7534");

    private Guid _analystId;

    [Fact]
    public async Task Evaluate_GivenNoQueues_LeavesTheCaseUnrouted()
    {
        // Arrange
        await ArrangeAsync();
        await CreateRuleAsync();

        // Act
        await EvaluateAsync(20000, "GB");

        // Assert
        var caseEntity = await SingleCaseAsync();

        caseEntity.QueueId.Should().BeNull();
        caseEntity.StatusId.Should().Be((int)CaseStatus.Pending);
    }

    [Fact]
    public async Task Evaluate_GivenAQueueMatchingOnSeverity_RoutesTheCaseToIt()
    {
        // Arrange
        await ArrangeAsync();
        await CreateRuleAsync();

        var queueId = await CreateQueueAsync("Serious", minimumSeverity: RuleSeverity.High);

        // Act
        await EvaluateAsync(20000, "GB");

        // Assert
        var caseEntity = await SingleCaseAsync();

        caseEntity.QueueId.Should().Be(queueId);
    }

    [Fact]
    public async Task Evaluate_GivenAQueueAboveTheCaseSeverity_LeavesItUnrouted()
    {
        // Arrange
        await ArrangeAsync();
        await CreateRuleAsync();

        await CreateQueueAsync("Critical only", minimumSeverity: RuleSeverity.Critical);

        // Act
        await EvaluateAsync(20000, "GB");

        // Assert
        var caseEntity = await SingleCaseAsync();

        caseEntity.QueueId.Should().BeNull();
    }

    [Fact]
    public async Task Evaluate_GivenAQueueMatchingOnAnAmountBand_RoutesOnlyThePaymentsInIt()
    {
        // Arrange
        await ArrangeAsync();
        await CreateRuleAsync(threshold: 1000);

        var bandQueueId = await CreateQueueAsync("High value", priority: 10, matcher: new RuleBodyDto
        {
            And =
            [
                new RuleBodyDto { Field = "amount", Condition = nameof(RuleCondition.GreaterThanEqual), Value = 50000 },
                new RuleBodyDto { Field = "amount", Condition = nameof(RuleCondition.LessThan), Value = 100000 }
            ]
        });

        // Act
        await EvaluateAsync(75000, "GB", sender: "ACC-1");
        await EvaluateAsync(2000, "GB", sender: "ACC-2");

        // Assert
        var cases = await CasesAsync();

        cases.Should().HaveCount(2);
        cases.Count(x => x.QueueId == bandQueueId).Should().Be(1);
        cases.Count(x => x.QueueId == null).Should().Be(1);
    }

    [Fact]
    public async Task Evaluate_GivenAQueueMatchingOnRegion_RoutesOnlyThatRegion()
    {
        // Arrange
        await ArrangeAsync();
        await CreateRuleAsync();

        var emeaQueueId = await CreateQueueAsync("EMEA", priority: 10, matcher: new RuleBodyDto
        {
            Field = "country",
            Condition = nameof(RuleCondition.In),
            Value = new[] { "GB", "DE", "FR" }
        });

        // Act
        await EvaluateAsync(20000, "DE", sender: "ACC-1");
        await EvaluateAsync(20000, "US", sender: "ACC-2");

        // Assert
        var cases = await CasesAsync();

        cases.Count(x => x.QueueId == emeaQueueId).Should().Be(1);
        cases.Count(x => x.QueueId == null).Should().Be(1);
    }

    [Fact]
    public async Task Evaluate_GivenSeveralMatchingQueues_TheHighestPriorityOneTakesIt()
    {
        // Arrange
        await ArrangeAsync();
        await CreateRuleAsync();

        var specific = await CreateQueueAsync("Specific", priority: 10, minimumSeverity: RuleSeverity.High);
        await CreateQueueAsync("Catch all", priority: 900);

        // Act
        await EvaluateAsync(20000, "GB");

        // Assert
        var caseEntity = await SingleCaseAsync();

        caseEntity.QueueId.Should().Be(specific);
    }

    [Fact]
    public async Task Evaluate_GivenADisabledQueue_SkipsItAndFallsThrough()
    {
        // Arrange
        await ArrangeAsync();
        await CreateRuleAsync();

        var disabled = await CreateQueueAsync("Disabled", priority: 10, isEnabled: false);
        var fallback = await CreateQueueAsync("Fallback", priority: 900);

        // Act
        await EvaluateAsync(20000, "GB");

        // Assert
        var caseEntity = await SingleCaseAsync();

        caseEntity.QueueId.Should().Be(fallback);
        caseEntity.QueueId.Should().NotBe(disabled);
    }

    [Fact]
    public async Task Evaluate_GivenAManualQueue_LeavesTheCaseUnassigned()
    {
        // Arrange
        await ArrangeAsync();
        await CreateRuleAsync();

        await CreateQueueAsync("Manual", memberUserIds: [_analystId]);

        // Act
        await EvaluateAsync(20000, "GB");

        // Assert
        var caseEntity = await SingleCaseAsync();

        caseEntity.AssignedToUserId.Should().BeNull();
        caseEntity.StatusId.Should().Be((int)CaseStatus.Pending);
    }

    [Fact]
    public async Task Evaluate_GivenARoundRobinQueue_AssignsTheCaseOnArrival()
    {
        // Arrange
        await ArrangeAsync();
        await CreateRuleAsync();

        await CreateQueueAsync("Rotating", routingPolicy: QueueRoutingPolicy.RoundRobin, memberUserIds: [_analystId]);

        // Act
        await EvaluateAsync(20000, "GB");

        // Assert
        var caseEntity = await SingleCaseAsync();

        caseEntity.AssignedToUserId.Should().Be(_analystId);
        caseEntity.StatusId.Should().Be((int)CaseStatus.Assigned);
        caseEntity.AssignedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Evaluate_GivenARoundRobinQueue_SpreadsSuccessiveCasesAcrossTheMembers()
    {
        // Arrange
        await ArrangeAsync();
        await CreateRuleAsync();

        var second = await CreateAnalystAsync("second@mail.com");

        await CreateQueueAsync("Rotating", routingPolicy: QueueRoutingPolicy.RoundRobin, memberUserIds: [_analystId, second]);

        // Act
        await EvaluateAsync(20000, "GB", sender: "ACC-1");
        await EvaluateAsync(20000, "GB", sender: "ACC-2");

        // Assert
        var cases = await CasesAsync();

        cases.Should().HaveCount(2);
        cases.Select(x => x.AssignedToUserId).Distinct().Should().HaveCount(2);
    }

    [Fact]
    public async Task Evaluate_GivenARoundRobinQueueWithNobodyOnIt_StillRoutesButLeavesItWaiting()
    {
        // Arrange
        await ArrangeAsync();
        await CreateRuleAsync();

        var queueId = await CreateQueueAsync("Empty desk", routingPolicy: QueueRoutingPolicy.RoundRobin);

        // Act
        await EvaluateAsync(20000, "GB");

        // Assert
        var caseEntity = await SingleCaseAsync();

        caseEntity.QueueId.Should().Be(queueId);
        caseEntity.AssignedToUserId.Should().BeNull();
        caseEntity.StatusId.Should().Be((int)CaseStatus.Pending);
    }

    [Fact]
    public async Task Evaluate_GivenALoadAwareQueue_AssignsToWhoeverIsCarryingLeast()
    {
        // Arrange
        await ArrangeAsync();
        await CreateRuleAsync();

        var second = await CreateAnalystAsync("second@mail.com");

        var queueId = await CreateQueueAsync("Load aware", routingPolicy: QueueRoutingPolicy.LeastLoaded, memberUserIds: [_analystId, second]);

        await SeedOpenCasesAsync(queueId, _analystId, 3);

        // Act
        await EvaluateAsync(20000, "GB");

        // Assert
        var caseEntity = await NewestCaseAsync();

        caseEntity.AssignedToUserId.Should().Be(second);
    }

    [Fact]
    public async Task Evaluate_GivenAFurtherPaymentGroupingOntoAnOpenCase_DoesNotRerouteIt()
    {
        // Arrange
        await ArrangeAsync();
        await SetPartyMappingAsync();
        await CreateRuleAsync(threshold: 1000);

        var lowValue = await CreateQueueAsync("Low value", priority: 10, matcher: new RuleBodyDto
        {
            Field = "amount",
            Condition = nameof(RuleCondition.LessThan),
            Value = 50000
        });

        await CreateQueueAsync("High value", priority: 20, matcher: new RuleBodyDto
        {
            Field = "amount",
            Condition = nameof(RuleCondition.GreaterThanEqual),
            Value = 50000
        });

        // Act
        await EvaluateAsync(2000, "GB");
        await EvaluateAsync(90000, "GB");

        // Assert
        var cases = await CasesAsync();

        cases.Should().ContainSingle();
        cases[0].QueueId.Should().Be(lowValue);
    }

    [Fact]
    public async Task ClaimNext_GivenWorkWaitingOnTheAnalystQueue_HandsThemTheCase()
    {
        // Arrange
        await ArrangeAsync();

        var queueId = await CreateQueueAsync("Manual", memberUserIds: [_analystId]);
        await SeedWaitingCaseAsync(queueId, RuleSeverity.High, DateTimeOffset.UtcNow.AddHours(-1));

        // Act
        var response = await _analystClient.PostAsync("/queues/claim-next", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var claimed = await response.Content.ReadFromJsonAsync<ResponseDataDto<CaseResourceDto>>();

        claimed!.Data.Case.AssignedToUserId.Should().Be(_analystId);
        claimed.Data.Case.Status.Should().Be(nameof(CaseStatus.Assigned));
        claimed.Data.Case.QueueId.Should().Be(queueId);
    }

    [Fact]
    public async Task ClaimNext_TakesTheMostSevereCaseFirst()
    {
        // Arrange
        await ArrangeAsync();

        var queueId = await CreateQueueAsync("Manual", memberUserIds: [_analystId]);

        await SeedWaitingCaseAsync(queueId, RuleSeverity.Low, DateTimeOffset.UtcNow.AddDays(-2));
        var criticalId = await SeedWaitingCaseAsync(queueId, RuleSeverity.Critical, DateTimeOffset.UtcNow.AddMinutes(-5));

        // Act
        var response = await _analystClient.PostAsync("/queues/claim-next", null);

        // Assert
        var claimed = await response.Content.ReadFromJsonAsync<ResponseDataDto<CaseResourceDto>>();

        claimed!.Data.Case.CaseId.Should().Be(criticalId);
    }

    [Fact]
    public async Task ClaimNext_AtEqualSeverity_TakesTheLongestWaiting()
    {
        // Arrange
        await ArrangeAsync();

        var queueId = await CreateQueueAsync("Manual", memberUserIds: [_analystId]);

        var oldestId = await SeedWaitingCaseAsync(queueId, RuleSeverity.High, DateTimeOffset.UtcNow.AddDays(-3));
        await SeedWaitingCaseAsync(queueId, RuleSeverity.High, DateTimeOffset.UtcNow.AddMinutes(-5));

        // Act
        var response = await _analystClient.PostAsync("/queues/claim-next", null);

        // Assert
        var claimed = await response.Content.ReadFromJsonAsync<ResponseDataDto<CaseResourceDto>>();

        claimed!.Data.Case.CaseId.Should().Be(oldestId);
    }

    [Fact]
    public async Task ClaimNext_GivenAnEmptyQueue_ReturnsNoContent()
    {
        // Arrange
        await ArrangeAsync();
        await CreateQueueAsync("Manual", memberUserIds: [_analystId]);

        // Act
        var response = await _analystClient.PostAsync("/queues/claim-next", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ClaimNext_GivenAQueueTheCallerIsNotOn_IsRefused()
    {
        // Arrange
        await ArrangeAsync();

        var queueId = await CreateQueueAsync("Somebody else's desk");
        await SeedWaitingCaseAsync(queueId, RuleSeverity.High, DateTimeOffset.UtcNow.AddHours(-1));

        // Act
        var response = await _analystClient.PostAsync($"/queues/{queueId}/claim-next", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ClaimNext_WithoutNamingAQueue_IgnoresQueuesTheCallerIsNotOn()
    {
        // Arrange
        await ArrangeAsync();

        var otherQueueId = await CreateQueueAsync("Somebody else's desk");
        await SeedWaitingCaseAsync(otherQueueId, RuleSeverity.Critical, DateTimeOffset.UtcNow.AddDays(-1));

        var myQueueId = await CreateQueueAsync("My desk", priority: 200, memberUserIds: [_analystId]);
        var mineId = await SeedWaitingCaseAsync(myQueueId, RuleSeverity.Low, DateTimeOffset.UtcNow.AddMinutes(-1));

        // Act
        var response = await _analystClient.PostAsync("/queues/claim-next", null);

        // Assert
        var claimed = await response.Content.ReadFromJsonAsync<ResponseDataDto<CaseResourceDto>>();

        claimed!.Data.Case.CaseId.Should().Be(mineId);
    }

    [Fact]
    public async Task ClaimNext_GivenARetiredQueue_HandsOutNothing()
    {
        // Arrange
        await ArrangeAsync();

        var queueId = await CreateQueueAsync("Manual", memberUserIds: [_analystId]);
        await SeedWaitingCaseAsync(queueId, RuleSeverity.High, DateTimeOffset.UtcNow.AddHours(-1));

        await _httpClient.DeleteAsync($"/organizations/{OrganizationId}/queues/{queueId}");

        // Act
        var response = await _analystClient.PostAsync("/queues/claim-next", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ClaimNext_GivenTwoAnalystsPullingAtOnce_HandsEachADifferentCase()
    {
        // Arrange
        await ArrangeAsync();

        var second = await CreateAnalystAsync("second@mail.com");
        var secondClient = testFixture.CreateClient();
        await AuthenticationHelper.SetTokenAsync(secondClient, "second@mail.com", Password);

        var queueId = await CreateQueueAsync("Manual", memberUserIds: [_analystId, second]);

        await SeedWaitingCaseAsync(queueId, RuleSeverity.High, DateTimeOffset.UtcNow.AddHours(-2));
        await SeedWaitingCaseAsync(queueId, RuleSeverity.High, DateTimeOffset.UtcNow.AddHours(-1));

        // Act
        var responses = await Task.WhenAll(
            _analystClient.PostAsync("/queues/claim-next", null),
            secondClient.PostAsync("/queues/claim-next", null));

        // Assert
        responses.Should().OnlyContain(x => x.StatusCode == HttpStatusCode.OK);

        var claimed = new List<Guid>();

        foreach (var response in responses)
        {
            var resource = await response.Content.ReadFromJsonAsync<ResponseDataDto<CaseResourceDto>>();
            claimed.Add(resource!.Data.Case.CaseId);
        }

        claimed.Distinct().Should().HaveCount(2);
    }

    [Fact]
    public async Task ListMyQueues_ReturnsOnlyTheQueuesTheCallerIsOn()
    {
        // Arrange
        await ArrangeAsync();

        var mine = await CreateQueueAsync("My desk", memberUserIds: [_analystId]);
        await CreateQueueAsync("Somebody else's desk", priority: 200);

        // Act
        var response = await _analystClient.GetFromJsonAsync<PagedResponseDto<QueueResourceDto>>("/queues/my");

        // Assert
        response!.Data.Should().ContainSingle().Which.Queue.QueueId.Should().Be(mine);
    }

    [Fact]
    public async Task ListCases_FiltersToTheUnroutedPile()
    {
        // Arrange
        await ArrangeAsync();
        await CreateRuleAsync(threshold: 1000);

        await CreateQueueAsync("High value", matcher: new RuleBodyDto
        {
            Field = "amount",
            Condition = nameof(RuleCondition.GreaterThanEqual),
            Value = 50000
        });

        await EvaluateAsync(90000, "GB", sender: "ACC-1");
        await EvaluateAsync(2000, "GB", sender: "ACC-2");

        // Act
        var response = await _httpClient.GetFromJsonAsync<PagedResponseDto<CaseResourceDto>>("/cases?unrouted=true");

        // Assert
        response!.Data.Should().ContainSingle().Which.Case.QueueId.Should().BeNull();
    }

    [Fact]
    public async Task ListCases_FiltersByQueue()
    {
        // Arrange
        await ArrangeAsync();
        await CreateRuleAsync();

        var queueId = await CreateQueueAsync("Serious", minimumSeverity: RuleSeverity.High);

        await EvaluateAsync(20000, "GB");

        // Act
        var response = await _httpClient.GetFromJsonAsync<PagedResponseDto<CaseResourceDto>>($"/cases?queueId={queueId}");

        // Assert
        response!.Data.Should().ContainSingle().Which.Case.QueueName.Should().Be("Serious");
    }

    private async Task<Guid> CreateQueueAsync(
        string name,
        int priority = 100,
        QueueRoutingPolicy routingPolicy = QueueRoutingPolicy.Manual,
        RuleSeverity? minimumSeverity = null,
        RuleBodyDto? matcher = null,
        bool isEnabled = true,
        Guid[]? memberUserIds = null)
    {
        var response = await _httpClient.PostAsJsonAsync($"/organizations/{OrganizationId}/queues", new CreateQueueRequestBodyDto
        {
            Name = name,
            Description = null,
            Priority = priority,
            RoutingPolicy = routingPolicy.ToString(),
            MinimumSeverity = minimumSeverity?.ToString(),
            RuleIds = null,
            Matcher = matcher,
            IsEnabled = isEnabled,
            MemberUserIds = memberUserIds
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await response.Content.ReadFromJsonAsync<ResponseDataDto<QueueResponseDto>>();

        return created!.Data.QueueId;
    }

    private async Task SetPartyMappingAsync()
    {
        var response = await _httpClient.PutAsJsonAsync("/organizations/my/party-mapping", new SetPartyMappingRequestBodyDto
        {
            Mappings =
            [
                new PartyRoleMappingDto
                {
                    Role = nameof(PartyRole.Remitter),
                    Path = "sender",
                    IdentifierPath = "accountId",
                    DisplayNamePath = "name"
                }
            ]
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task<Guid> CreateAnalystAsync(string email)
    {
        var user = await AuthenticationHelper.CreateUserAsync(testFixture.Services, email, Password, [UserRole.User], OrganizationId);

        return user.Id;
    }

    private async Task<Guid> SeedWaitingCaseAsync(Guid queueId, RuleSeverity severity, DateTimeOffset createdAt)
    {
        var caseEntity = FakeCaseEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            QueueId = queueId,
            StatusId = (int)CaseStatus.Pending,
            SeverityId = (int)severity,
            AssignedToUserId = null,
            AssignedToUser = null!,
            AssignedAt = null,
            ResolvedAt = null,
            DispositionId = null,
            CaseReasonCodeId = null,
            CreatedAt = createdAt,
            UpdatedAt = createdAt,
            IsDeleted = false
        };

        await DatabaseHelper.CreateCases(testFixture.Services, [caseEntity]);

        return caseEntity.CaseId;
    }

    private async Task SeedOpenCasesAsync(Guid queueId, Guid assignedToUserId, int count)
    {
        var caseEntities = Enumerable.Range(0, count).Select(_ => FakeCaseEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            QueueId = queueId,
            StatusId = (int)CaseStatus.InReview,
            AssignedToUserId = assignedToUserId,
            AssignedToUser = null!,
            ResolvedAt = null,
            DispositionId = null,
            CaseReasonCodeId = null,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-1),
            UpdatedAt = DateTimeOffset.UtcNow.AddDays(-1),
            IsDeleted = false
        }).ToList();

        await DatabaseHelper.CreateCases(testFixture.Services, caseEntities);
    }

    private async Task<CaseEntity> SingleCaseAsync()
    {
        var cases = await CasesAsync();

        cases.Should().ContainSingle();

        return cases[0];
    }

    private async Task<CaseEntity> NewestCaseAsync()
    {
        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        return await context.Cases
            .Where(x => x.OrganizationId == OrganizationId)
            .OrderByDescending(x => x.CreatedAt)
            .FirstAsync();
    }

    private async Task<List<CaseEntity>> CasesAsync()
    {
        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        return await context.Cases
            .Where(x => x.OrganizationId == OrganizationId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync();
    }

    private async Task EvaluateAsync(int amount, string country, string sender = "ACC-1")
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/rule-sets/evaluate")
        {
            Content = JsonContent.Create(Payload(amount, country, sender))
        };

        request.Headers.Add(HeaderNames.CreateCase, "true");

        var response = await _ingestClient.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static JsonObject Payload(int amount, string country, string sender)
    {
        return JsonNode.Parse(
            $$"""
              {
                "amount": {{amount}},
                "country": "{{country}}",
                "sender": { "accountId": "{{sender}}", "name": "Ada" }
              }
              """)!.AsObject();
    }

    private async Task CreateRuleAsync(int threshold = 10000)
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
        await AuthenticationHelper.CreateUserAsync(
            testFixture.Services, ManagerEmail, Password, [UserRole.User, UserRole.Manager, UserRole.Admin], OrganizationId);

        await AuthenticationHelper.SetTokenAsync(_httpClient, ManagerEmail, Password);

        _analystId = await CreateAnalystAsync(AnalystEmail);

        await AuthenticationHelper.SetTokenAsync(_analystClient, AnalystEmail, Password);

        await ArrangeIngestAsync();
    }

    private async Task ArrangeIngestAsync()
    {
        var applicationUserEntity = (FakeApplicationUser.CreateValid(_fixture) with
        {
            OrganizationId = new OrganizationId(OrganizationId),
            IsDeleted = new IsDeleted(false)
        }).ToEntity();

        await DatabaseHelper.CreateApplicationUsers(testFixture.Services, [applicationUserEntity]);

        await DatabaseHelper.CreateRuleSets(testFixture.Services,
        [
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
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}
