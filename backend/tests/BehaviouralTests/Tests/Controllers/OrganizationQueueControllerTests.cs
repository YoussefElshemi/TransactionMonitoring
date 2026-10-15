using System.Net;
using System.Net.Http.Json;
using AutoFixture;
using BehaviouralTests.TestHelpers;
using Core.Enums;
using FluentAssertions;
using Infrastructure;
using Infrastructure.Configurations;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Models.Common;
using Presentation.Models.Queues;
using Presentation.Models.Rules;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

namespace BehaviouralTests.Tests.Controllers;

public class OrganizationQueueControllerTests(TestFixture testFixture) : IClassFixture<TestFixture>, IAsyncLifetime
{
    private readonly Fixture _fixture = AutoFixtureHelper.Create();
    private readonly HttpClient _httpClient = testFixture.CreateClient();

    private const string Email = "queues@mail.com";
    private const string Password = "P@$$w0rd";
    private static readonly Guid OrganizationId = new("2b7c1f4a-6d38-4e52-9a1b-0c8f3e7d5642");

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
        return Task.CompletedTask;
    }

    [Fact]
    public async Task CreateQueue_GivenAValidRequest_ReturnsCreated()
    {
        // Act
        var response = await _httpClient.PostAsJsonAsync($"/organizations/{OrganizationId}/queues", Body("High value", priority: 10));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await response.Content.ReadFromJsonAsync<ResponseDataDto<QueueResponseDto>>();

        created.Should().NotBeNull();
        created!.Data.Name.Should().Be("High value");
        created.Data.Priority.Should().Be(10);
        created.Data.RoutingPolicy.Should().Be(nameof(QueueRoutingPolicy.Manual));
    }

    [Fact]
    public async Task CreateQueue_WithoutAPriority_LandsAtTheBackOfTheLine()
    {
        // Act
        var response = await _httpClient.PostAsJsonAsync($"/organizations/{OrganizationId}/queues", Body("Unranked", priority: null));

        // Assert
        var created = await response.Content.ReadFromJsonAsync<ResponseDataDto<QueueResponseDto>>();

        created!.Data.Priority.Should().Be(1000);
    }

    [Fact]
    public async Task CreateQueue_GivenANameThatDiffersOnlyInCasing_IsRefused()
    {
        // Arrange
        await CreateQueueAsync("High value");

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/organizations/{OrganizationId}/queues", Body("HIGH VALUE"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateQueue_GivenAMatcherWithAVelocityCondition_IsRefused()
    {
        // Arrange
        var body = Body("Velocity") with
        {
            Matcher = new RuleBodyDto
            {
                Condition = nameof(RuleCondition.GreaterThan),
                Value = 3,
                Velocity = new VelocityConditionDto
                {
                    Role = nameof(PartyRole.Remitter),
                    Metric = "count",
                    Function = nameof(VelocityFunction.Count),
                    Window = new VelocityWindowDto
                    {
                        Kind = nameof(VelocityWindowKind.Rolling),
                        Unit = nameof(VelocityWindowUnit.Hour),
                        Length = 1
                    },
                    IncludeCurrent = true
                }
            }
        };

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/organizations/{OrganizationId}/queues", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateQueue_GivenAMatcherReadingThePayload_IsAccepted()
    {
        // Arrange
        var body = Body("High value EMEA") with
        {
            Matcher = new RuleBodyDto
            {
                And =
                [
                    new RuleBodyDto { Field = "amount", Condition = nameof(RuleCondition.GreaterThanEqual), Value = 10000 },
                    new RuleBodyDto { Field = "country", Condition = nameof(RuleCondition.In), Value = new[] { "GB", "DE" } }
                ]
            }
        };

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/organizations/{OrganizationId}/queues", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await response.Content.ReadFromJsonAsync<ResponseDataDto<QueueResponseDto>>();

        created!.Data.Matcher.Should().NotBeNull();
        created.Data.Matcher!.And.Should().HaveCount(2);
    }

    [Fact]
    public async Task UpdateQueue_RenamingAQueue_IsAllowed()
    {
        // Arrange
        var queueId = await CreateQueueAsync("Hgih value");

        // Act
        var response = await _httpClient.PatchAsJsonAsync($"/organizations/{OrganizationId}/queues/{queueId}", new UpdateQueueRequestBodyDto
        {
            Name = "High value"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var queueEntity = await QueueEntityAsync(queueId);

        queueEntity.Name.Should().Be("High value");
    }

    [Fact]
    public async Task UpdateQueue_RenamingAQueueToWhatItIsAlreadyCalled_IsAllowed()
    {
        // Arrange
        var queueId = await CreateQueueAsync("High value");

        // Act
        var response = await _httpClient.PatchAsJsonAsync($"/organizations/{OrganizationId}/queues/{queueId}", new UpdateQueueRequestBodyDto
        {
            Name = "High value",
            Priority = 5
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task UpdateQueue_ClearingTheMinimumSeverity_WidensTheQueueBackOut()
    {
        // Arrange
        var queueId = await CreateQueueAsync("Critical", minimumSeverity: RuleSeverity.Critical);

        // Act
        var response = await _httpClient.PatchAsJsonAsync($"/organizations/{OrganizationId}/queues/{queueId}", new UpdateQueueRequestBodyDto
        {
            ClearMinimumSeverity = true
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var queueEntity = await QueueEntityAsync(queueId);

        queueEntity.MinimumSeverityId.Should().BeNull();
    }

    [Fact]
    public async Task UpdateQueue_WithoutTouchingACriterion_LeavesItAlone()
    {
        // Arrange
        var queueId = await CreateQueueAsync("Critical", minimumSeverity: RuleSeverity.Critical);

        // Act
        var response = await _httpClient.PatchAsJsonAsync($"/organizations/{OrganizationId}/queues/{queueId}", new UpdateQueueRequestBodyDto
        {
            Priority = 3
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var queueEntity = await QueueEntityAsync(queueId);

        queueEntity.MinimumSeverityId.Should().Be((int)RuleSeverity.Critical);
        queueEntity.Priority.Should().Be(3);
    }

    [Fact]
    public async Task UpdateQueue_ClearingAndSettingACriterionTogether_IsRefused()
    {
        // Arrange
        var queueId = await CreateQueueAsync("Critical", minimumSeverity: RuleSeverity.Critical);

        // Act
        var response = await _httpClient.PatchAsJsonAsync($"/organizations/{OrganizationId}/queues/{queueId}", new UpdateQueueRequestBodyDto
        {
            MinimumSeverity = nameof(RuleSeverity.High),
            ClearMinimumSeverity = true
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SetQueueMembers_StatesTheWholeMembership()
    {
        // Arrange
        var queueId = await CreateQueueAsync("High value");
        var second = await CreateAnalystAsync("second@mail.com");

        // Act
        var response = await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/queues/{queueId}/members", new SetQueueMembersRequestBodyDto
        {
            UserIds = [_userId, second]
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var members = await _httpClient.GetFromJsonAsync<ResponseDataDto<QueueMemberResponseDto[]>>(
            $"/organizations/{OrganizationId}/queues/{queueId}/members");

        members!.Data.Should().HaveCount(2);
    }

    [Fact]
    public async Task SetQueueMembers_OmittingAMember_TakesThemOff()
    {
        // Arrange
        var queueId = await CreateQueueAsync("High value");
        var second = await CreateAnalystAsync("leaver@mail.com");

        await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/queues/{queueId}/members", new SetQueueMembersRequestBodyDto
        {
            UserIds = [_userId, second]
        });

        // Act
        await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/queues/{queueId}/members", new SetQueueMembersRequestBodyDto
        {
            UserIds = [_userId]
        });

        // Assert
        var members = await _httpClient.GetFromJsonAsync<ResponseDataDto<QueueMemberResponseDto[]>>(
            $"/organizations/{OrganizationId}/queues/{queueId}/members");

        members!.Data.Should().ContainSingle().Which.UserId.Should().Be(_userId);
    }

    [Fact]
    public async Task SetQueueMembers_ResavingAnUnchangedList_LeavesTheRotationWhereItWas()
    {
        // Arrange
        var queueId = await CreateQueueAsync("High value");

        await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/queues/{queueId}/members", new SetQueueMembersRequestBodyDto
        {
            UserIds = [_userId]
        });

        await TouchRotationAsync(queueId, _userId);

        // Act
        await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/queues/{queueId}/members", new SetQueueMembersRequestBodyDto
        {
            UserIds = [_userId]
        });

        // Assert
        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var member = await context.QueueMembers.FirstAsync(x => x.QueueId == queueId && x.UserId == _userId);

        member.LastAssignedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task SetQueueMembers_GivenAUserFromAnotherOrganization_IsRefused()
    {
        // Arrange
        var queueId = await CreateQueueAsync("High value");

        // Act
        var response = await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/queues/{queueId}/members", new SetQueueMembersRequestBodyDto
        {
            UserIds = [Guid.NewGuid()]
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetQueueUsages_ReportsWhatRetiringItWouldTouch()
    {
        // Arrange
        var queueId = await CreateQueueAsync("High value");

        await CreateCaseAsync(queueId, CaseStatus.Pending);
        await CreateCaseAsync(queueId, CaseStatus.Approved);

        // Act
        var response = await _httpClient.GetFromJsonAsync<ResponseDataDto<QueueUsageResponseDto>>(
            $"/organizations/{OrganizationId}/queues/{queueId}/usages");

        // Assert
        response.Should().NotBeNull();
        response!.Data.CaseCount.Should().Be(2);
        response.Data.OpenCaseCount.Should().Be(1);
    }

    [Fact]
    public async Task GetQueueStatistics_ReportsDepthApartFromWorkInHand()
    {
        // Arrange
        var queueId = await CreateQueueAsync("High value");

        await CreateCaseAsync(queueId, CaseStatus.Pending);
        await CreateCaseAsync(queueId, CaseStatus.Pending);
        await CreateCaseAsync(queueId, CaseStatus.InReview, _userId);

        // Act
        var response = await _httpClient.GetFromJsonAsync<ResponseDataDto<QueueStatisticResponseDto[]>>(
            $"/organizations/{OrganizationId}/queues/statistics");

        // Assert
        response.Should().NotBeNull();

        var statistic = response!.Data.Single(x => x.QueueId == queueId);

        statistic.Depth.Should().Be(2);
        statistic.InProgressCount.Should().Be(1);
        statistic.OldestWaitingForSeconds.Should().NotBeNull();
    }

    [Fact]
    public async Task GetQueueStatistics_GivenAnEmptyQueue_ReportsNoOldestItemRatherThanZero()
    {
        // Arrange
        var queueId = await CreateQueueAsync("Quiet");

        // Act
        var response = await _httpClient.GetFromJsonAsync<ResponseDataDto<QueueStatisticResponseDto[]>>(
            $"/organizations/{OrganizationId}/queues/statistics");

        // Assert
        var statistic = response!.Data.Single(x => x.QueueId == queueId);

        statistic.Depth.Should().Be(0);
        statistic.OldestWaitingForSeconds.Should().BeNull();
    }

    [Fact]
    public async Task GetQueueStatistics_ReportsPerAnalystLoad()
    {
        // Arrange
        var queueId = await CreateQueueAsync("High value");

        await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/queues/{queueId}/members", new SetQueueMembersRequestBodyDto
        {
            UserIds = [_userId]
        });

        await CreateCaseAsync(queueId, CaseStatus.InReview, _userId);
        await CreateCaseAsync(queueId, CaseStatus.InReview, _userId);
        await CreateCaseAsync(queueId, CaseStatus.Approved, _userId);

        // Act
        var response = await _httpClient.GetFromJsonAsync<ResponseDataDto<QueueStatisticResponseDto>>(
            $"/organizations/{OrganizationId}/queues/{queueId}/statistics");

        // Assert
        response.Should().NotBeNull();

        var load = response!.Data.MemberLoads.Should().ContainSingle().Subject;

        load.UserId.Should().Be(_userId);
        load.OpenCaseCount.Should().Be(2);
    }

    [Fact]
    public async Task ListQueues_OrdersByPriority()
    {
        // Arrange
        await CreateQueueAsync("Third", priority: 300);
        await CreateQueueAsync("First", priority: 100);
        await CreateQueueAsync("Second", priority: 200);

        // Act
        var response = await _httpClient.GetFromJsonAsync<PagedResponseDto<QueueResourceDto>>(
            $"/organizations/{OrganizationId}/queues");

        // Assert
        response!.Data.Select(x => x.Queue.Name).Should().ContainInOrder("First", "Second", "Third");
    }

    [Fact]
    public async Task ListQueues_FiltersByRoutingPolicy()
    {
        // Arrange
        await CreateQueueAsync("Manual");
        await CreateQueueAsync("Rotating", routingPolicy: QueueRoutingPolicy.RoundRobin);

        // Act
        var response = await _httpClient.GetFromJsonAsync<PagedResponseDto<QueueResourceDto>>(
            $"/organizations/{OrganizationId}/queues?routingPolicy={nameof(QueueRoutingPolicy.RoundRobin)}");

        // Assert
        response!.Data.Should().ContainSingle().Which.Queue.Name.Should().Be("Rotating");
    }

    [Fact]
    public async Task DeleteQueue_GivenAQueueCasesWereRoutedTo_LeavesThoseCasesPointingAtIt()
    {
        // Arrange
        var queueId = await CreateQueueAsync("High value");
        var caseId = await CreateCaseAsync(queueId, CaseStatus.Pending);

        // Act
        var response = await _httpClient.DeleteAsync($"/organizations/{OrganizationId}/queues/{queueId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var caseEntity = await context.Cases.FirstAsync(x => x.CaseId == caseId);

        caseEntity.QueueId.Should().Be(queueId);
    }

    [Fact]
    public async Task RecoverQueue_PutsARetiredQueueBackIntoUse()
    {
        // Arrange
        var queueId = await CreateQueueAsync("High value");
        await _httpClient.DeleteAsync($"/organizations/{OrganizationId}/queues/{queueId}");

        // Act
        var response = await _httpClient.PostAsync($"/organizations/{OrganizationId}/queues/{queueId}/recover", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var queueEntity = await QueueEntityAsync(queueId);

        queueEntity.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task GetQueueHistory_AfterWideningTheCriteria_ReturnsTheAuditTrail()
    {
        // Arrange
        var queueId = await CreateQueueAsync("Critical", minimumSeverity: RuleSeverity.Critical);

        await _httpClient.PatchAsJsonAsync($"/organizations/{OrganizationId}/queues/{queueId}", new UpdateQueueRequestBodyDto
        {
            ClearMinimumSeverity = true
        });

        // Act
        var response = await _httpClient.GetAsync($"/organizations/{OrganizationId}/queues/{queueId}/history");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static CreateQueueRequestBodyDto Body(
        string name,
        int? priority = 100,
        QueueRoutingPolicy routingPolicy = QueueRoutingPolicy.Manual,
        RuleSeverity? minimumSeverity = null)
    {
        return new CreateQueueRequestBodyDto
        {
            Name = name,
            Description = null,
            Priority = priority,
            RoutingPolicy = routingPolicy.ToString(),
            MinimumSeverity = minimumSeverity?.ToString(),
            RuleIds = null,
            Matcher = null,
            IsEnabled = true,
            MemberUserIds = null
        };
    }

    private async Task<Guid> CreateQueueAsync(
        string name,
        int? priority = 100,
        QueueRoutingPolicy routingPolicy = QueueRoutingPolicy.Manual,
        RuleSeverity? minimumSeverity = null)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"/organizations/{OrganizationId}/queues", Body(name, priority, routingPolicy, minimumSeverity));

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await response.Content.ReadFromJsonAsync<ResponseDataDto<QueueResponseDto>>();

        return created!.Data.QueueId;
    }

    private async Task<Guid> CreateAnalystAsync(string email)
    {
        var user = await AuthenticationHelper.CreateUserAsync(testFixture.Services, email, Password, [UserRole.User], OrganizationId);

        return user.Id;
    }

    private async Task<Guid> CreateCaseAsync(Guid queueId, CaseStatus status, Guid? assignedToUserId = null)
    {
        var caseEntity = FakeCaseEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            QueueId = queueId,
            StatusId = (int)status,
            AssignedToUserId = assignedToUserId,
            AssignedToUser = null!,
            DispositionId = null,
            CaseReasonCodeId = null,
            IsDeleted = false
        };

        await DatabaseHelper.CreateCases(testFixture.Services, [caseEntity]);

        return caseEntity.CaseId;
    }

    private async Task TouchRotationAsync(Guid queueId, Guid userId)
    {
        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var member = await context.QueueMembers.FirstAsync(x => x.QueueId == queueId && x.UserId == userId);

        member.LastAssignedAt = DateTimeOffset.UtcNow;
        context.QueueMembers.Update(member);

        await context.SaveChangesAsync();
    }

    private async Task<QueueEntity> QueueEntityAsync(Guid queueId)
    {
        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        return await context.Queues.FirstAsync(x => x.QueueId == queueId);
    }
}
