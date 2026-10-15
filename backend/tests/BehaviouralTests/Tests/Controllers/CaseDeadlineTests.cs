using System.Net;
using System.Net.Http.Json;
using System.Text;
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
using Presentation.Models.CaseSlaTargets;
using Presentation.Models.Common;
using Presentation.Models.Rules;
using Presentation.Models.Statistics;
using UnitTests.TestHelpers.FakeObjects.Core.Models.ApiClients;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Users;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

namespace BehaviouralTests.Tests.Controllers;

public class CaseDeadlineTests(TestFixture testFixture) : IClassFixture<TestFixture>, IAsyncLifetime
{
    private readonly Fixture _fixture = AutoFixtureHelper.Create();
    private readonly HttpClient _httpClient = testFixture.CreateClient();
    private readonly HttpClient _ingestClient = testFixture.CreateClient();

    private const string ManagerEmail = "supervisor@mail.com";
    private const string Password = "P@$$w0rd";
    private static readonly Guid OrganizationId = new("b1d47f0e-3c62-4a58-9e14-7f2d5a8c3b19");

    [Fact]
    public async Task Evaluate_WithNoDeclaredTargets_GivesTheCaseTheHouseDeadlineForItsSeverity()
    {
        // Arrange
        await ArrangeAsync();
        await CreateRuleAsync(severity: RuleSeverity.High);

        // Act
        await EvaluateAsync(20000);

        // Assert
        var caseEntity = await SingleCaseAsync();

        caseEntity.DueAt.Should().BeCloseTo(caseEntity.CreatedAt.AddMinutes(480), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task Evaluate_WithADeclaredTarget_UsesItInsteadOfTheHouseDefault()
    {
        // Arrange
        await ArrangeAsync();
        await SetSlaTargetsAsync((RuleSeverity.High, 45));
        await CreateRuleAsync(severity: RuleSeverity.High);

        // Act
        await EvaluateAsync(20000);

        // Assert
        var caseEntity = await SingleCaseAsync();

        caseEntity.DueAt.Should().BeCloseTo(caseEntity.CreatedAt.AddMinutes(45), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task Evaluate_WithAShorterHoldWindow_CapsTheDeadlineAtTheHold()
    {
        // Arrange
        await ArrangeAsync();
        await SetHoldPolicyAsync(holdWindowMinutes: 60);
        await CreateRuleAsync(severity: RuleSeverity.High);

        // Act
        await EvaluateAsync(20000);

        // Assert
        var caseEntity = await SingleCaseAsync();

        caseEntity.DueAt.Should().BeCloseTo(caseEntity.CreatedAt.AddMinutes(60), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task SetSlaTargets_LeavesTheDeadlinesOnCasesAlreadyOpen()
    {
        // Arrange
        await ArrangeAsync();
        await CreateRuleAsync(severity: RuleSeverity.High);
        await EvaluateAsync(20000);

        var before = await SingleCaseAsync();

        // Act
        await SetSlaTargetsAsync((RuleSeverity.High, 5));

        // Assert
        var after = await SingleCaseAsync();

        after.DueAt.Should().Be(before.DueAt);
    }

    [Fact]
    public async Task GetSlaTargets_ReturnsEveryBandWithTheDeclaredOnesOverridingTheDefaults()
    {
        // Arrange
        await ArrangeAsync();
        await SetSlaTargetsAsync((RuleSeverity.Critical, 15));

        // Act
        var response = await _httpClient.GetFromJsonAsync<ResponseDataDto<SlaTargetResponseDto[]>>(
            $"/organizations/{OrganizationId}/sla-targets");

        // Assert
        response!.Data.Should().HaveCount(Enum.GetValues<RuleSeverity>().Length);
        response.Data.Single(x => x.Severity == nameof(RuleSeverity.Critical)).TargetMinutes.Should().Be(15);
        response.Data.Single(x => x.Severity == nameof(RuleSeverity.High)).TargetMinutes.Should().Be(480);
    }

    [Fact]
    public async Task SetSlaTargets_WithABandOmitted_PutsThatBandBackOnTheHouseDefault()
    {
        // Arrange
        await ArrangeAsync();
        await SetSlaTargetsAsync((RuleSeverity.Critical, 15), (RuleSeverity.High, 45));

        // Act
        await SetSlaTargetsAsync((RuleSeverity.Critical, 15));

        // Assert
        var response = await _httpClient.GetFromJsonAsync<ResponseDataDto<SlaTargetResponseDto[]>>(
            $"/organizations/{OrganizationId}/sla-targets");

        response!.Data.Single(x => x.Severity == nameof(RuleSeverity.High)).TargetMinutes.Should().Be(480);
    }

    [Fact]
    public async Task SetSlaTargets_NamingOneSeverityTwice_IsRefused()
    {
        // Arrange
        await ArrangeAsync();

        // Act
        var response = await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/sla-targets", new SetOrganizationSlaTargetsRequestBodyDto
        {
            Targets =
            [
                new SlaTargetRequestDto { Severity = nameof(RuleSeverity.High), TargetMinutes = 30 },
                new SlaTargetRequestDto { Severity = nameof(RuleSeverity.High), TargetMinutes = 60 }
            ]
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ListCases_SurfacesTheAgeingAndTheBreachFlagOnEveryRow()
    {
        // Arrange
        await ArrangeAsync();
        await SeedCaseAsync(openedHoursAgo: 10, dueHoursAgo: 2);
        await SeedCaseAsync(openedHoursAgo: 1, dueInHours: 5);

        // Act
        var response = await _httpClient.GetFromJsonAsync<PagedResponseDto<CaseResourceDto>>("/cases");

        // Assert
        response!.Data.Should().HaveCount(2);
        response.Data.Should().ContainSingle(x => x.Ageing.IsBreached);
        response.Data.Should().ContainSingle(x => x.Ageing.Bucket == nameof(CaseAgeBucket.Breached));
        response.Data.Should().ContainSingle(x => x.Ageing.Bucket == nameof(CaseAgeBucket.Fresh));

        response.Data.Single(x => x.Ageing.IsBreached).Ageing.TimeToDueInMilliseconds.Should().BeNegative();
    }

    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, 1)]
    public async Task ListCases_FiltersOnTheBreachFlag(bool breached, int expected)
    {
        // Arrange
        await ArrangeAsync();
        await SeedCaseAsync(openedHoursAgo: 10, dueHoursAgo: 2);
        await SeedCaseAsync(openedHoursAgo: 1, dueInHours: 5);

        // Act
        var response = await _httpClient.GetFromJsonAsync<PagedResponseDto<CaseResourceDto>>(
            $"/cases?breached={breached.ToString().ToLowerInvariant()}");

        // Assert
        response!.Data.Should().HaveCount(expected);
        response.Data.Should().OnlyContain(x => x.Ageing.IsBreached == breached);
    }

    [Fact]
    public async Task ListCases_ForACaseResolvedInTime_NeverReadsAsBreached()
    {
        // Arrange
        await ArrangeAsync();
        await SeedCaseAsync(
            openedHoursAgo: 100,
            dueHoursAgo: 98,
            status: CaseStatus.Approved,
            resolvedHoursAgo: 99);

        // Act
        var response = await _httpClient.GetFromJsonAsync<PagedResponseDto<CaseResourceDto>>("/cases");

        // Assert
        response!.Data.Should().ContainSingle().Which.Ageing.IsBreached.Should().BeFalse();
    }

    [Fact]
    public async Task ListCases_ForACaseResolvedLate_StaysBreached()
    {
        // Arrange
        await ArrangeAsync();
        await SeedCaseAsync(
            openedHoursAgo: 100,
            dueHoursAgo: 98,
            status: CaseStatus.Rejected,
            resolvedHoursAgo: 90);

        // Act
        var response = await _httpClient.GetFromJsonAsync<PagedResponseDto<CaseResourceDto>>("/cases");

        // Assert
        response!.Data.Should().ContainSingle().Which.Ageing.IsBreached.Should().BeTrue();
    }

    [Fact]
    public async Task GetStatistics_ReportsTheBreachedCountAndTheRate()
    {
        // Arrange
        await ArrangeAsync();
        await SeedCaseAsync(openedHoursAgo: 10, dueHoursAgo: 2);
        await SeedCaseAsync(openedHoursAgo: 10, dueHoursAgo: 1);
        await SeedCaseAsync(openedHoursAgo: 1, dueInHours: 5);
        await SeedCaseAsync(openedHoursAgo: 1, dueInHours: 5);

        // Act
        var response = await _httpClient.GetFromJsonAsync<ResponseDataDto<StatisticResponseDto>>("/statistics");

        // Assert
        response!.Data.Case.Total.Should().Be(4);
        response.Data.Case.Breached.Should().Be(2);
        response.Data.Case.BreachRate.Should().Be(50m);
    }

    [Fact]
    public async Task GetStatistics_WithNoCases_ReportsAZeroBreachRate()
    {
        // Arrange
        await ArrangeAsync();

        // Act
        var response = await _httpClient.GetFromJsonAsync<ResponseDataDto<StatisticResponseDto>>("/statistics");

        // Assert
        response!.Data.Case.Breached.Should().Be(0);
        response.Data.Case.BreachRate.Should().Be(0m);
    }

    private async Task SetSlaTargetsAsync(params (RuleSeverity Severity, int Minutes)[] targets)
    {
        var response = await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/sla-targets", new SetOrganizationSlaTargetsRequestBodyDto
        {
            Targets = targets.Select(x => new SlaTargetRequestDto
            {
                Severity = x.Severity.ToString(),
                TargetMinutes = x.Minutes
            }).ToArray()
        });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    private async Task SetHoldPolicyAsync(int holdWindowMinutes)
    {
        // Raw JSON: Patch<T> needs the API's converter, which the test client doesn't register.
        var response = await _httpClient.PatchAsync(
            "/organizations/my/hold-policy",
            new StringContent($$"""{"holdWindow":{{holdWindowMinutes}}}""", Encoding.UTF8, "application/json"));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    private async Task SeedCaseAsync(
        int openedHoursAgo,
        int? dueHoursAgo = null,
        int? dueInHours = null,
        CaseStatus status = CaseStatus.Pending,
        int? resolvedHoursAgo = null)
    {
        var now = DateTimeOffset.UtcNow;
        var createdAt = now.AddHours(-openedHoursAgo);

        var caseEntity = FakeCaseEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            StatusId = (int)status,
            SeverityId = (int)RuleSeverity.High,
            AssignedToUserId = null,
            AssignedToUser = null!,
            AssignedAt = null,
            DispositionId = null,
            CaseReasonCodeId = null,
            QueueId = null,
            CreatedAt = createdAt,
            UpdatedAt = createdAt,
            DueAt = dueHoursAgo.HasValue ? now.AddHours(-dueHoursAgo.Value) : now.AddHours(dueInHours!.Value),
            ResolvedAt = resolvedHoursAgo.HasValue ? now.AddHours(-resolvedHoursAgo.Value) : null,
            IsDeleted = false
        };

        await DatabaseHelper.CreateCases(testFixture.Services, [caseEntity]);
    }

    private async Task<CaseEntity> SingleCaseAsync()
    {
        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var cases = await context.Cases
            .Where(x => x.OrganizationId == OrganizationId)
            .ToListAsync();

        cases.Should().ContainSingle();

        return cases[0];
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
        return JsonNode.Parse($$"""{ "amount": {{amount}} }""")!.AsObject();
    }

    private async Task CreateRuleAsync(RuleSeverity severity, int threshold = 10000)
    {
        var ruleSetId = await ExistingRuleSetIdAsync();

        var response = await _httpClient.PostAsJsonAsync($"/rule-sets/{ruleSetId}/rules", new CreateRuleRequestBodyDto
        {
            FriendlyName = "Large payment",
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
        await AuthenticationHelper.CreateUserAsync(
            testFixture.Services, ManagerEmail, Password, [UserRole.User, UserRole.Manager, UserRole.Admin], OrganizationId);

        await AuthenticationHelper.SetTokenAsync(_httpClient, ManagerEmail, Password);

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
