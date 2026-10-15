using System.Net;
using System.Net.Http.Json;
using System.Text;
using AutoFixture;
using BehaviouralTests.TestHelpers;
using Core.Enums;
using Core.Extensions;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentAssertions;
using Infrastructure;
using Infrastructure.Configurations;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Models.Cases;
using Presentation.Models.Common;
using Presentation.Models.Escalations;
using Presentation.Models.Queues;
using Presentation.Models.Rules;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

namespace BehaviouralTests.Tests.Controllers;

public class CaseEscalationTests(TestFixture testFixture) : IClassFixture<TestFixture>, IAsyncLifetime
{
    private readonly Fixture _fixture = AutoFixtureHelper.Create();
    private readonly HttpClient _httpClient = testFixture.CreateClient();

    private const string ManagerEmail = "supervisor@mail.com";
    private const string Password = "P@$$w0rd";
    private static readonly Guid OrganizationId = new("2f9c4e18-7a35-4d62-b8e0-51c6f3a9d472");

    [Fact]
    public async Task EscalateDueAsync_WithNoRules_LeavesEveryCaseAlone()
    {
        // Arrange
        await ArrangeAsync();
        var caseId = await SeedCaseAsync(openedHoursAgo: 48);

        // Act
        var escalated = await EscalateAsync();

        // Assert
        ((int)escalated).Should().Be(0);

        var caseEntity = await CaseAsync(caseId);
        caseEntity.EscalatedAt.Should().BeNull();
        caseEntity.EscalationRuleId.Should().BeNull();
    }

    [Fact]
    public async Task EscalateDueAsync_GivenWorkUnassignedTooLong_StampsTheCase()
    {
        // Arrange
        await ArrangeAsync();
        var escalationRuleId = await CreateEscalationRuleAsync("Sitting too long", unassignedForMinutes: 60);
        var caseId = await SeedCaseAsync(openedHoursAgo: 4);

        // Act
        var escalated = await EscalateAsync();

        // Assert
        ((int)escalated).Should().Be(1);

        var caseEntity = await CaseAsync(caseId);
        caseEntity.EscalatedAt.Should().NotBeNull();
        caseEntity.EscalationRuleId.Should().Be(escalationRuleId);
    }

    [Fact]
    public async Task EscalateDueAsync_GivenWorkThatHasNotWaitedLongEnough_LeavesItAlone()
    {
        // Arrange
        await ArrangeAsync();
        await CreateEscalationRuleAsync("Sitting too long", unassignedForMinutes: 600);
        var caseId = await SeedCaseAsync(openedHoursAgo: 1);

        // Act
        await EscalateAsync();

        // Assert
        (await CaseAsync(caseId)).EscalatedAt.Should().BeNull();
    }

    [Fact]
    public async Task EscalateDueAsync_GivenWorkSomebodyIsHolding_DoesNotEscalateOnAge()
    {
        // Arrange
        await ArrangeAsync();
        await CreateEscalationRuleAsync("Sitting too long", unassignedForMinutes: 60);

        var userId = await CreateAnalystAsync();
        var caseId = await SeedCaseAsync(openedHoursAgo: 48, assignedToUserId: userId);

        // Act
        await EscalateAsync();

        // Assert
        (await CaseAsync(caseId)).EscalatedAt.Should().BeNull();
    }

    [Fact]
    public async Task EscalateDueAsync_RunTwice_EscalatesTheSameCaseOnlyOnce()
    {
        // Arrange
        await ArrangeAsync();
        await CreateEscalationRuleAsync("Sitting too long", unassignedForMinutes: 60);
        await SeedCaseAsync(openedHoursAgo: 4);

        // Act
        var first = await EscalateAsync();
        var second = await EscalateAsync();

        // Assert
        ((int)first).Should().Be(1);
        ((int)second).Should().Be(0);
    }

    [Fact]
    public async Task EscalateDueAsync_GivenAPaymentApproachingItsHoldExpiry_StampsTheCase()
    {
        // Arrange
        await ArrangeAsync();
        await CreateEscalationRuleAsync("Running out of time", holdExpiryWithinMinutes: 30);

        var caseId = await SeedCaseAsync(openedHoursAgo: 1);
        await SeedHeldDecisionAsync(caseId, expiresInMinutes: 10);

        // Act
        await EscalateAsync();

        // Assert
        (await CaseAsync(caseId)).EscalatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task EscalateDueAsync_GivenAHoldStillFarOff_LeavesTheCaseAlone()
    {
        // Arrange
        await ArrangeAsync();
        await CreateEscalationRuleAsync("Running out of time", holdExpiryWithinMinutes: 30);

        var caseId = await SeedCaseAsync(openedHoursAgo: 1);
        await SeedHeldDecisionAsync(caseId, expiresInMinutes: 600);

        // Act
        await EscalateAsync();

        // Assert
        (await CaseAsync(caseId)).EscalatedAt.Should().BeNull();
    }

    [Fact]
    public async Task EscalateDueAsync_GivenASettledPayment_IgnoresItsExpiry()
    {
        // Arrange
        await ArrangeAsync();
        await CreateEscalationRuleAsync("Running out of time", holdExpiryWithinMinutes: 30);

        var caseId = await SeedCaseAsync(openedHoursAgo: 1);
        await SeedHeldDecisionAsync(caseId, expiresInMinutes: 10, status: DecisionStatus.Released);

        // Act
        await EscalateAsync();

        // Assert
        (await CaseAsync(caseId)).EscalatedAt.Should().BeNull();
    }

    [Fact]
    public async Task EscalateDueAsync_GivenAnAmountMatcher_TakesOnlyThePaymentsAboveIt()
    {
        // Arrange
        await ArrangeAsync();
        await CreateEscalationRuleAsync("Big money", matcher: new RuleBodyDto
        {
            Field = "amount",
            Condition = nameof(RuleCondition.GreaterThan),
            Value = 10000
        });

        var largeCaseId = await SeedCaseAsync(openedHoursAgo: 1, amount: 50000);
        var smallCaseId = await SeedCaseAsync(openedHoursAgo: 1, amount: 100);

        // Act
        await EscalateAsync();

        // Assert
        (await CaseAsync(largeCaseId)).EscalatedAt.Should().NotBeNull();
        (await CaseAsync(smallCaseId)).EscalatedAt.Should().BeNull();
    }

    [Fact]
    public async Task EscalateDueAsync_GivenSeveralConditions_RequiresAllOfThem()
    {
        // Arrange
        await ArrangeAsync();
        await CreateEscalationRuleAsync("Old and serious", unassignedForMinutes: 60, minimumSeverity: RuleSeverity.Critical);

        var criticalCaseId = await SeedCaseAsync(openedHoursAgo: 4, severity: RuleSeverity.Critical);
        var lowCaseId = await SeedCaseAsync(openedHoursAgo: 4, severity: RuleSeverity.Low);

        // Act
        await EscalateAsync();

        // Assert
        (await CaseAsync(criticalCaseId)).EscalatedAt.Should().NotBeNull();
        (await CaseAsync(lowCaseId)).EscalatedAt.Should().BeNull();
    }

    [Fact]
    public async Task EscalateDueAsync_WithATargetQueue_MovesTheCaseToIt()
    {
        // Arrange
        await ArrangeAsync();
        var queueId = await CreateQueueAsync("Senior desk");
        await CreateEscalationRuleAsync("Sitting too long", unassignedForMinutes: 60, targetQueueId: queueId);

        var caseId = await SeedCaseAsync(openedHoursAgo: 4);

        // Act
        await EscalateAsync();

        // Assert
        (await CaseAsync(caseId)).QueueId.Should().Be(queueId);
    }

    [Fact]
    public async Task EscalateDueAsync_RaisingTheSeverity_MovesTheCaseUpTheBands()
    {
        // Arrange
        await ArrangeAsync();
        await CreateEscalationRuleAsync("Sitting too long", unassignedForMinutes: 60, raiseSeverityTo: RuleSeverity.Critical);

        var caseId = await SeedCaseAsync(openedHoursAgo: 4, severity: RuleSeverity.Medium);

        // Act
        await EscalateAsync();

        // Assert
        (await CaseAsync(caseId)).SeverityId.Should().Be((int)RuleSeverity.Critical);
    }

    [Fact]
    public async Task EscalateDueAsync_RaisingTheSeverity_LeavesTheDeadlineWhereItWas()
    {
        // Arrange
        await ArrangeAsync();
        await CreateEscalationRuleAsync("Sitting too long", unassignedForMinutes: 60, raiseSeverityTo: RuleSeverity.Critical);

        var caseId = await SeedCaseAsync(openedHoursAgo: 4);
        var before = await CaseAsync(caseId);

        // Act
        await EscalateAsync();

        // Assert
        (await CaseAsync(caseId)).DueAt.Should().Be(before.DueAt);
    }

    [Fact]
    public async Task EscalateDueAsync_LeavesTheCaseClaimable()
    {
        // Arrange
        await ArrangeAsync();
        await CreateEscalationRuleAsync("Sitting too long", unassignedForMinutes: 60);

        var caseId = await SeedCaseAsync(openedHoursAgo: 4);

        // Act
        await EscalateAsync();

        // Assert
        var caseEntity = await CaseAsync(caseId);
        caseEntity.StatusId.Should().Be((int)CaseStatus.Pending);
        caseEntity.AssignedToUserId.Should().BeNull();
    }

    [Fact]
    public async Task EscalateDueAsync_WithTwoMatchingRules_TakesTheHigherPriorityOne()
    {
        // Arrange
        await ArrangeAsync();
        var first = await CreateEscalationRuleAsync("First", priority: 10, unassignedForMinutes: 60);
        await CreateEscalationRuleAsync("Second", priority: 20, unassignedForMinutes: 60);

        var caseId = await SeedCaseAsync(openedHoursAgo: 4);

        // Act
        await EscalateAsync();

        // Assert
        (await CaseAsync(caseId)).EscalationRuleId.Should().Be(first);
    }

    [Fact]
    public async Task EscalateDueAsync_WithADisabledRule_LeavesTheCaseAlone()
    {
        // Arrange
        await ArrangeAsync();
        await CreateEscalationRuleAsync("Sitting too long", unassignedForMinutes: 60, isEnabled: false);

        var caseId = await SeedCaseAsync(openedHoursAgo: 4);

        // Act
        await EscalateAsync();

        // Assert
        (await CaseAsync(caseId)).EscalatedAt.Should().BeNull();
    }

    [Fact]
    public async Task EscalateDueAsync_GivenAResolvedCase_LeavesItAlone()
    {
        // Arrange
        await ArrangeAsync();
        await CreateEscalationRuleAsync("Sitting too long", unassignedForMinutes: 60);

        var caseId = await SeedCaseAsync(openedHoursAgo: 48, status: CaseStatus.Approved);

        // Act
        await EscalateAsync();

        // Assert
        (await CaseAsync(caseId)).EscalatedAt.Should().BeNull();
    }

    [Fact]
    public async Task EscalateDueAsync_WithMoreOpenWorkThanOneBatch_StillReachesTheEndOfTheBacklog()
    {
        // Arrange
        await ArrangeAsync();
        await CreateEscalationRuleAsync("Only the serious", unassignedForMinutes: 60, minimumSeverity: RuleSeverity.Critical);

        for (var index = 0; index < 100; index++)
        {
            await SeedCaseAsync(openedHoursAgo: 48, severity: RuleSeverity.Low);
        }

        var criticalCaseId = await SeedCaseAsync(openedHoursAgo: 4, severity: RuleSeverity.Critical);

        // Act
        var escalated = await EscalateAsync();

        // Assert
        ((int)escalated).Should().Be(1);
        (await CaseAsync(criticalCaseId)).EscalatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ListCases_FiltersOnWhetherAnEscalationHasTakenTheCase()
    {
        // Arrange
        await ArrangeAsync();
        await CreateEscalationRuleAsync("Sitting too long", unassignedForMinutes: 60);
        await SeedCaseAsync(openedHoursAgo: 4);
        await SeedCaseAsync(openedHoursAgo: 0);

        await EscalateAsync();

        // Act
        var escalated = await _httpClient.GetFromJsonAsync<PagedResponseDto<CaseResourceDto>>("/cases?escalated=true");
        var untouched = await _httpClient.GetFromJsonAsync<PagedResponseDto<CaseResourceDto>>("/cases?escalated=false");

        // Assert
        escalated!.Data.Should().ContainSingle().Which.Case.EscalatedAt.Should().NotBeNull();
        untouched!.Data.Should().ContainSingle().Which.Case.EscalatedAt.Should().BeNull();
    }

    [Fact]
    public async Task CreateEscalationRule_WithNoConditions_IsRefused()
    {
        // Arrange
        await ArrangeAsync();

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/organizations/{OrganizationId}/escalation-rules", new CreateEscalationRuleRequestBodyDto
        {
            Name = "Everything",
            IsEnabled = true
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateEscalationRule_WithAVelocityMatcher_IsRefused()
    {
        // Arrange
        await ArrangeAsync();

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/organizations/{OrganizationId}/escalation-rules", new CreateEscalationRuleRequestBodyDto
        {
            Name = "Velocity",
            IsEnabled = true,
            Matcher = new RuleBodyDto
            {
                Condition = nameof(RuleCondition.GreaterThan),
                Value = 5,
                Velocity = new VelocityConditionDto
                {
                    Role = nameof(PartyRole.Remitter),
                    Metric = "count",
                    Function = nameof(VelocityFunction.Count),
                    IncludeCurrent = true,
                    Window = new VelocityWindowDto
                    {
                        Kind = nameof(VelocityWindowKind.Rolling),
                        Unit = nameof(VelocityWindowUnit.Hour),
                        Length = 1
                    }
                }
            }
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateEscalationRule_WithADuplicateName_IsRefused()
    {
        // Arrange
        await ArrangeAsync();
        await CreateEscalationRuleAsync("Sitting too long", unassignedForMinutes: 60);

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/organizations/{OrganizationId}/escalation-rules", new CreateEscalationRuleRequestBodyDto
        {
            Name = "SITTING TOO LONG",
            IsEnabled = true,
            UnassignedForMinutes = 60
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateEscalationRule_ClearingItsOnlyCondition_IsRefused()
    {
        // Arrange
        await ArrangeAsync();
        var escalationRuleId = await CreateEscalationRuleAsync("Sitting too long", unassignedForMinutes: 60);

        // Act
        var response = await _httpClient.PatchAsJsonAsync(
            $"/organizations/{OrganizationId}/escalation-rules/{escalationRuleId}",
            new UpdateEscalationRuleRequestBodyDto { ClearUnassignedFor = true });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateEscalationRule_ClearingOneOfTwoConditions_IsAllowed()
    {
        // Arrange
        await ArrangeAsync();
        var escalationRuleId = await CreateEscalationRuleAsync(
            "Old and serious", unassignedForMinutes: 60, minimumSeverity: RuleSeverity.High);

        // Act
        var response = await _httpClient.PatchAsJsonAsync(
            $"/organizations/{OrganizationId}/escalation-rules/{escalationRuleId}",
            new UpdateEscalationRuleRequestBodyDto { ClearUnassignedFor = true });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteEscalationRule_StopsItFiringAndLeavesTheCasesItStampedReadable()
    {
        // Arrange
        await ArrangeAsync();
        var escalationRuleId = await CreateEscalationRuleAsync("Sitting too long", unassignedForMinutes: 60);

        var stampedCaseId = await SeedCaseAsync(openedHoursAgo: 4);
        await EscalateAsync();

        // Act
        var response = await _httpClient.DeleteAsync($"/organizations/{OrganizationId}/escalation-rules/{escalationRuleId}");

        var freshCaseId = await SeedCaseAsync(openedHoursAgo: 4);
        var escalated = await EscalateAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        ((int)escalated).Should().Be(0);

        (await CaseAsync(stampedCaseId)).EscalationRuleId.Should().Be(escalationRuleId);
        (await CaseAsync(freshCaseId)).EscalatedAt.Should().BeNull();
    }

    private async Task<Count> EscalateAsync()
    {
        using var scope = testFixture.Services.CreateScope();
        var escalationRuleService = scope.ServiceProvider.GetRequiredService<IEscalationRuleService>();

        return await escalationRuleService.EscalateDueAsync(CancellationToken.None);
    }

    private async Task<Guid> CreateEscalationRuleAsync(
        string name,
        int priority = 100,
        int? unassignedForMinutes = null,
        int? holdExpiryWithinMinutes = null,
        RuleSeverity? minimumSeverity = null,
        RuleBodyDto? matcher = null,
        Guid? targetQueueId = null,
        RuleSeverity? raiseSeverityTo = null,
        bool isEnabled = true)
    {
        var response = await _httpClient.PostAsJsonAsync($"/organizations/{OrganizationId}/escalation-rules", new CreateEscalationRuleRequestBodyDto
        {
            Name = name,
            Description = null,
            Priority = priority,
            IsEnabled = isEnabled,
            UnassignedForMinutes = unassignedForMinutes,
            HoldExpiryWithinMinutes = holdExpiryWithinMinutes,
            MinimumSeverity = minimumSeverity?.ToString(),
            Matcher = matcher,
            TargetQueueId = targetQueueId,
            RaiseSeverityTo = raiseSeverityTo?.ToString()
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await response.Content.ReadFromJsonAsync<ResponseDataDto<EscalationRuleResponseDto>>();

        return created!.Data.EscalationRuleId;
    }

    private async Task<Guid> CreateQueueAsync(string name)
    {
        var response = await _httpClient.PostAsJsonAsync($"/organizations/{OrganizationId}/queues", new CreateQueueRequestBodyDto
        {
            Name = name,
            Description = null,
            Priority = 100,
            RoutingPolicy = nameof(QueueRoutingPolicy.Manual),
            MinimumSeverity = null,
            RuleIds = null,
            Matcher = null,
            IsEnabled = true,
            MemberUserIds = null
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await response.Content.ReadFromJsonAsync<ResponseDataDto<QueueResponseDto>>();

        return created!.Data.QueueId;
    }

    private async Task<Guid> CreateAnalystAsync()
    {
        var user = await AuthenticationHelper.CreateUserAsync(
            testFixture.Services, "analyst@mail.com", Password, [UserRole.User], OrganizationId);

        return user.Id;
    }

    private async Task<Guid> SeedCaseAsync(
        int openedHoursAgo,
        RuleSeverity severity = RuleSeverity.Medium,
        CaseStatus status = CaseStatus.Pending,
        Guid? assignedToUserId = null,
        int amount = 100)
    {
        var now = DateTimeOffset.UtcNow;
        var createdAt = now.AddHours(-openedHoursAgo);

        var transactionEntity = FakeTransactionEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            Payload = Convert.ToBase64String(Encoding.UTF8.GetBytes($$"""{"amount":{{amount}}}""")),
            IsDeleted = false
        };

        await DatabaseHelper.CreateTransactions(testFixture.Services, [transactionEntity]);

        var caseEntity = FakeCaseEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            TransactionId = transactionEntity.TransactionId,
            StatusId = (int)status,
            SeverityId = (int)severity,
            AssignedToUserId = assignedToUserId,
            AssignedToUser = null!,
            AssignedAt = assignedToUserId.HasValue ? createdAt : null,
            DispositionId = null,
            CaseReasonCodeId = null,
            QueueId = null,
            CreatedAt = createdAt,
            UpdatedAt = createdAt,
            DueAt = createdAt.AddHours(24),
            ResolvedAt = status.IsTerminal() ? now : null,
            EscalatedAt = null,
            EscalationRuleId = null,
            IsDeleted = false
        };

        await DatabaseHelper.CreateCases(testFixture.Services, [caseEntity]);

        return caseEntity.CaseId;
    }

    private async Task SeedHeldDecisionAsync(Guid caseId, int expiresInMinutes, DecisionStatus status = DecisionStatus.Held)
    {
        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var transactionId = await context.Cases
            .Where(x => x.CaseId == caseId)
            .Select(x => x.TransactionId!.Value)
            .FirstAsync();

        var decisionEntity = FakeDecisionEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            TransactionId = transactionId,
            Transaction = null!,
            CaseId = caseId,
            StatusId = (int)status,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(expiresInMinutes),
            HoldExpiryVerdictId = (int)HoldExpiryVerdict.Block
        };

        await DatabaseHelper.CreateDecisions(testFixture.Services, [decisionEntity]);
    }

    private async Task<CaseEntity> CaseAsync(Guid caseId)
    {
        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        return await context.Cases.FirstAsync(x => x.CaseId == caseId);
    }

    private async Task ArrangeAsync()
    {
        await AuthenticationHelper.CreateUserAsync(
            testFixture.Services, ManagerEmail, Password, [UserRole.User, UserRole.Manager, UserRole.Admin], OrganizationId);

        await AuthenticationHelper.SetTokenAsync(_httpClient, ManagerEmail, Password);
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
