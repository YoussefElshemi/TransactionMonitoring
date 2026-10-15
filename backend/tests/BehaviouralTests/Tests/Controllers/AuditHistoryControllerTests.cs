using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using AutoFixture;
using BehaviouralTests.TestHelpers;
using Core.Constants;
using Core.Enums;
using Core.Extensions;
using Core.Helpers;
using FluentAssertions;
using Infrastructure;
using Infrastructure.Configurations;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Constants;
using Presentation.Models.ApiClients;
using Presentation.Models.AuditLogs;
using Presentation.Models.Common;
using Presentation.Models.Organizations;
using Presentation.Models.Rules;
using Presentation.Models.RuleSets;
using Presentation.Models.WebhookConfigurations;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.ApiClients;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.Organizations;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.OrganizationWebhookConfigurations;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.Rules;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.RuleSets;

namespace BehaviouralTests.Tests.Controllers;

public class AuditHistoryControllerTests(TestFixture testFixture) : IClassFixture<TestFixture>, IAsyncLifetime
{
    private readonly Fixture _fixture = AutoFixtureHelper.Create();
    private readonly HttpClient _httpClient = testFixture.CreateClient();

    private const string Email = "username@mail.com";
    private const string Password = "P@$$w0rd";
    private static readonly Guid OrganizationId = new("4d6389e9-f5d1-4325-a004-915865e3adec");

    [Fact]
    public async Task GetRuleHistory_NotAuthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _httpClient.GetAsync($"/rules/{_fixture.Create<Guid>()}/history");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task GetRuleHistory_AfterCreatingAndUpdatingRule_ReturnsAuditTrail()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        var ruleSetEntity = FakeRuleSetEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            IsEnabled = true,
            IsDeleted = false
        };

        await DatabaseHelper.CreateRuleSets(testFixture.Services, [ruleSetEntity]);

        var createRequest = FakeCreateRuleRequestDto.CreateValid(_fixture) with
        {
            RuleSetId = ruleSetEntity.RuleSetId
        };

        var createResponse = await _httpClient.PostAsJsonAsync($"/rule-sets/{createRequest.RuleSetId}/rules", createRequest.Body);
        var createdRule = await createResponse.Content.ReadDataFromJsonAsync<RuleResourceDto>();

        var updateRequest = FakeCreateRuleVersionRequestDto.CreateValid(_fixture) with
        {
            RuleId = createdRule!.Rule.RuleId,
            OrganizationId = OrganizationId
        };

        await _httpClient.PostAsJsonAsync($"/rules/{updateRequest.RuleId}/versions", updateRequest.Body);
        await _httpClient.PostAsJsonAsync($"/rules/{updateRequest.RuleId}/deployments", new { versionNumber = 2, strategy = "AllAtOnce" });

        // Act
        var response = await _httpClient.GetAsync($"/rules/{createdRule.Rule.RuleId}/history");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);

        var history = await response.Content.ReadFromJsonAsync<PagedResponseDto<AuditLogResourceDto>>();
        history.Should().NotBeNull();
        history!.Data.Should().OnlyContain(x => x.AuditLog.EntityType == nameof(AuditableEntityType.Rule));
        history.Data.Should().OnlyContain(x => x.AuditLog.EntityId == createdRule.Rule.RuleId);
        history.Data.Should().OnlyContain(x => x.AuditLog.ChangedByUserId != null);
        history.Data.Should().Contain(x => x.AuditLog.Operation == nameof(AuditOperation.Created));
        history.Data.Should().Contain(x => x.AuditLog.Operation == nameof(AuditOperation.Updated));

        var createdEntry = history.Data.Single(x => x.AuditLog.Operation == nameof(AuditOperation.Created));
        createdEntry.AuditLog.Changes.Should().NotBeEmpty();
        createdEntry.AuditLog.Changes.Should().OnlyContain(x => x.OldValue == null);

        var updatedEntry = history.Data.First(x => x.AuditLog.Operation == nameof(AuditOperation.Updated));
        updatedEntry.AuditLog.Changes.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetRuleHistory_SnapshotDiff_OnlyIncludesAuditedFields()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        var ruleSetEntity = FakeRuleSetEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            IsEnabled = true,
            IsDeleted = false
        };

        await DatabaseHelper.CreateRuleSets(testFixture.Services, [ruleSetEntity]);

        var createRequest = FakeCreateRuleRequestDto.CreateValid(_fixture) with
        {
            RuleSetId = ruleSetEntity.RuleSetId
        };

        var createResponse = await _httpClient.PostAsJsonAsync($"/rule-sets/{createRequest.RuleSetId}/rules", createRequest.Body);
        var createdRule = await createResponse.Content.ReadDataFromJsonAsync<RuleResourceDto>();

        var updateRequest = FakeCreateRuleVersionRequestDto.CreateValid(_fixture) with
        {
            RuleId = createdRule!.Rule.RuleId,
            OrganizationId = OrganizationId
        };

        await _httpClient.PostAsJsonAsync($"/rules/{updateRequest.RuleId}/versions", updateRequest.Body);
        await _httpClient.PostAsJsonAsync($"/rules/{updateRequest.RuleId}/deployments", new { versionNumber = 2, strategy = "AllAtOnce" });

        // Act
        var response = await _httpClient.GetAsync($"/rules/{createdRule.Rule.RuleId}/history");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var history = await response.Content.ReadFromJsonAsync<PagedResponseDto<AuditLogResourceDto>>();
        history.Should().NotBeNull();

        string[] auditedFields =
        [
            "FriendlyName",
            "RuleBody",
            "Action",
            "Severity",
            "RiskWeight",
            "IsEnabled",
            "RuleSetId",
            "CurrentVersionNumber",
            "IsDeleted"
        ];

        var changedFields = history!.Data.SelectMany(x => x.AuditLog.Changes).Select(x => x.Field).Distinct().ToList();
        changedFields.Should().NotBeEmpty();
        changedFields.Should().OnlyContain(field => auditedFields.Contains(field));
        changedFields.Should().NotContain("CurrentRuleVersionId");
        changedFields.Should().NotContain("CreatedByUserId");
        changedFields.Should().NotContain("UpdatedAt");
        changedFields.Should().NotContain("CreatedAt");
    }

    [Fact]
    public async Task GetRuleHistory_RuleBody_IsBase64Decoded()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        var ruleSetEntity = FakeRuleSetEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            IsEnabled = true,
            IsDeleted = false
        };

        await DatabaseHelper.CreateRuleSets(testFixture.Services, [ruleSetEntity]);

        var createRequest = FakeCreateRuleRequestDto.CreateValid(_fixture) with
        {
            RuleSetId = ruleSetEntity.RuleSetId
        };

        var createResponse = await _httpClient.PostAsJsonAsync($"/rule-sets/{createRequest.RuleSetId}/rules", createRequest.Body);
        var createdRule = await createResponse.Content.ReadDataFromJsonAsync<RuleResourceDto>();
        var ruleId = createdRule!.Rule.RuleId;

        string storedRuleBody;
        using (var scope = testFixture.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
            storedRuleBody = (await context.Rules.FirstAsync(x => x.RuleId == ruleId)).RuleBody;
        }

        // Act
        var response = await _httpClient.GetAsync($"/rules/{ruleId}/history");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var history = await response.Content.ReadFromJsonAsync<PagedResponseDto<AuditLogResourceDto>>();
        var createdEntry = history!.Data.Single(x => x.AuditLog.Operation == nameof(AuditOperation.Created));
        var ruleBodyChange = createdEntry.AuditLog.Changes.Single(x => x.Field == "RuleBody");

        ruleBodyChange.NewValue!.GetValue<string>().Should().Be(storedRuleBody.FromBase64String());
        ruleBodyChange.NewValue!.GetValue<string>().Should().NotBe(storedRuleBody);
    }

    [Fact]
    public async Task SavingRule_WithOnlyNonAuditedFieldChanged_DoesNotCreateAuditEntry()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        var ruleSetEntity = FakeRuleSetEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            IsEnabled = true,
            IsDeleted = false
        };

        await DatabaseHelper.CreateRuleSets(testFixture.Services, [ruleSetEntity]);

        var createRequest = FakeCreateRuleRequestDto.CreateValid(_fixture) with
        {
            RuleSetId = ruleSetEntity.RuleSetId
        };

        var createResponse = await _httpClient.PostAsJsonAsync($"/rule-sets/{createRequest.RuleSetId}/rules", createRequest.Body);
        var createdRule = await createResponse.Content.ReadDataFromJsonAsync<RuleResourceDto>();
        var ruleId = createdRule!.Rule.RuleId;

        var auditCountBefore = await CountRuleAuditEntries(ruleId);

        // Act
        await MutateRule(ruleId, rule => rule.UpdatedAt = rule.UpdatedAt.AddMinutes(5));

        // Assert
        (await CountRuleAuditEntries(ruleId)).Should().Be(auditCountBefore);

        // Act
        await MutateRule(ruleId, rule => rule.FriendlyName = "Renamed directly");

        // Assert
        (await CountRuleAuditEntries(ruleId)).Should().Be(auditCountBefore + 1);
    }

    private async Task<int> CountRuleAuditEntries(Guid ruleId)
    {
        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        return await context.AuditLogs
            .CountAsync(x => x.EntityId == ruleId && x.EntityTypeId == (int)AuditableEntityType.Rule);
    }

    private async Task MutateRule(Guid ruleId, Action<RuleEntity> mutate)
    {
        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        // No-tracking by default, so attach before mutating.
        var rule = await context.Rules.FirstAsync(x => x.RuleId == ruleId);
        context.Attach(rule);
        mutate(rule);

        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetRuleSetHistory_AfterCreatingAndUpdatingRuleSet_ReturnsAuditTrail()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        var createRequest = FakeCreateRuleSetRequestDto.CreateValid(_fixture);
        var createResponse = await _httpClient.PostAsJsonAsync("/rule-sets", createRequest);
        var createdRuleSet = await createResponse.Content.ReadDataFromJsonAsync<RuleSetResourceDto>();

        var updateRequest = FakeUpdateRuleSetRequestDto.CreateValid(_fixture) with
        {
            RuleSetId = createdRuleSet!.RuleSet.RuleSetId,
            OrganizationId = OrganizationId
        };

        await _httpClient.PatchAsJsonAsync($"/rule-sets/{updateRequest.RuleSetId}", updateRequest.Body);

        // Act
        var response = await _httpClient.GetAsync($"/rule-sets/{createdRuleSet.RuleSet.RuleSetId}/history");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);

        var history = await response.Content.ReadFromJsonAsync<PagedResponseDto<AuditLogResourceDto>>();
        history.Should().NotBeNull();
        history!.Data.Should().OnlyContain(x => x.AuditLog.EntityType == nameof(AuditableEntityType.RuleSet));
        history.Data.Should().OnlyContain(x => x.AuditLog.EntityId == createdRuleSet.RuleSet.RuleSetId);
        history.Data.Should().OnlyContain(x => x.AuditLog.Changes != null);
        history.Data.Should().Contain(x => x.AuditLog.Operation == nameof(AuditOperation.Created));
        history.Data.Should().Contain(x => x.AuditLog.Operation == nameof(AuditOperation.Updated));
    }

    [Fact]
    public async Task GetRuleHistory_ReturnsPerFieldDiffForEachOperation()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        var ruleId = _fixture.Create<Guid>();
        var baseTime = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        var created = Snapshot(friendlyName: "Original", isEnabled: true, isDeleted: false, version: 1);
        var renamed = Snapshot(friendlyName: "Renamed", isEnabled: true, isDeleted: false, version: 1);
        var noop = Snapshot(friendlyName: "Renamed", isEnabled: true, isDeleted: false, version: 1);
        var deleted = Snapshot(friendlyName: "Renamed", isEnabled: true, isDeleted: true, version: 1);

        var auditLogs = new[]
        {
            AuditLog(ruleId, AuditOperation.Created, created, baseTime),
            AuditLog(ruleId, AuditOperation.Updated, renamed, baseTime.AddMinutes(1)),
            AuditLog(ruleId, AuditOperation.Updated, noop, baseTime.AddMinutes(2)),
            AuditLog(ruleId, AuditOperation.Deleted, deleted, baseTime.AddMinutes(3))
        };

        await DatabaseHelper.CreateAuditLogs(testFixture.Services, auditLogs);

        // Act
        var response = await _httpClient.GetAsync($"/rules/{ruleId}/history");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var history = await response.Content.ReadFromJsonAsync<PagedResponseDto<AuditLogResourceDto>>();
        history.Should().NotBeNull();
        history!.Data.Should().HaveCount(4);

        var createdEntry = history.Data.Single(x => x.AuditLog.Operation == nameof(AuditOperation.Created));
        createdEntry.AuditLog.Changes.Should().OnlyContain(x => x.OldValue == null);
        createdEntry.AuditLog.Changes.Should().Contain(x => x.Field == "FriendlyName" && x.NewValue!.GetValue<string>() == "Original");

        var subsetEntry = history.Data.Single(x => x.AuditLog.Operation == nameof(AuditOperation.Updated) && x.AuditLog.Changes.Any());
        subsetEntry.AuditLog.Changes.Should().ContainSingle();
        subsetEntry.AuditLog.Changes.Should().Contain(x =>
            x.Field == "FriendlyName" &&
            x.OldValue!.GetValue<string>() == "Original" &&
            x.NewValue!.GetValue<string>() == "Renamed");

        var noopEntry = history.Data.Single(x => x.AuditLog.Operation == nameof(AuditOperation.Updated) && !x.AuditLog.Changes.Any());
        noopEntry.AuditLog.Changes.Should().BeEmpty();

        var deletedEntry = history.Data.Single(x => x.AuditLog.Operation == nameof(AuditOperation.Deleted));
        deletedEntry.AuditLog.Changes.Should().Contain(x =>
            x.Field == "IsDeleted" &&
            !x.OldValue!.GetValue<bool>() &&
            x.NewValue!.GetValue<bool>());
    }

    private static string Snapshot(string friendlyName, bool isEnabled, bool isDeleted, int version)
    {
        return new JsonObject
        {
            ["FriendlyName"] = friendlyName,
            ["IsEnabled"] = isEnabled,
            ["IsDeleted"] = isDeleted,
            ["CurrentVersionNumber"] = version
        }.ToJsonString();
    }

    private AuditLogEntity AuditLog(Guid ruleId, AuditOperation operation, string snapshot, DateTimeOffset createdAt)
    {
        return FakeAuditLogEntity.CreateValid(_fixture) with
        {
            EntityId = ruleId,
            OrganizationId = OrganizationId,
            OperationId = (int)operation,
            Snapshot = snapshot,
            CreatedAt = createdAt
        };
    }

    [Fact]
    public async Task GetCaseHistory_AfterAssigningAndResolving_ReturnsAuditTrail()
    {
        // Arrange
        var user = await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        var caseEntity = FakeCaseEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            StatusId = (int)CaseStatus.Pending,
            AssignedToUserId = null,
            AssignedToUser = null,
            AssignedAt = null,
            ResolvedAt = null,
            IsDeleted = false
        };

        await DatabaseHelper.CreateCases(testFixture.Services, [caseEntity]);

        await _httpClient.PostAsJsonAsync($"/cases/{caseEntity.CaseId}/assign", new { userId = user.Id });
        await _httpClient.PostAsJsonAsync($"/cases/{caseEntity.CaseId}/resolve", new { status = nameof(CaseStatus.Approved), disposition = nameof(CaseDisposition.FalsePositive) });

        // Act
        var response = await _httpClient.GetAsync($"/cases/{caseEntity.CaseId}/history");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);

        var history = await response.Content.ReadFromJsonAsync<PagedResponseDto<AuditLogResourceDto>>();
        history.Should().NotBeNull();
        history!.Data.Should().OnlyContain(x => x.AuditLog.EntityType == nameof(AuditableEntityType.Case));
        history.Data.Should().OnlyContain(x => x.AuditLog.EntityId == caseEntity.CaseId);
        history.Data.Should().Contain(x => x.AuditLog.Operation == nameof(AuditOperation.Created));
        history.Data.Count(x => x.AuditLog.Operation == nameof(AuditOperation.Updated)).Should().Be(2);

        var statusChanges = history.Data.SelectMany(x => x.AuditLog.Changes).Where(x => x.Field == "Status").ToList();
        statusChanges.Should().Contain(x => x.NewValue!.GetValue<string>() == nameof(CaseStatus.Assigned));
        statusChanges.Should().Contain(x => x.NewValue!.GetValue<string>() == nameof(CaseStatus.Approved));

        var assignmentChanges = history.Data.SelectMany(x => x.AuditLog.Changes).Where(x => x.Field == "AssignedToUserId").ToList();
        assignmentChanges.Should().Contain(x => x.NewValue!.GetValue<Guid>() == user.Id);
    }

    [Fact]
    public async Task GetApiClientHistory_AfterCreatingAndRenaming_ReturnsAuditTrailWithoutTheSecret()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        var createRequest = FakeCreateApiClientRequestDto.CreateValid(_fixture);
        var createResponse = await _httpClient.PostAsJsonAsync("/api-clients", createRequest);
        var createdApiClient = await createResponse.Content.ReadDataFromJsonAsync<CreatedApiClientResourceDto>();

        var updateRequest = FakeUpdateApiClientRequestDto.CreateValid(_fixture);
        await _httpClient.PatchAsJsonAsync($"/api-clients/{createdApiClient!.ApiClient.ApiClientId}", updateRequest.Body);

        // Act
        var response = await _httpClient.GetAsync($"/api-clients/{createdApiClient.ApiClient.ApiClientId}/history");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);

        var history = await response.Content.ReadFromJsonAsync<PagedResponseDto<AuditLogResourceDto>>();
        history.Should().NotBeNull();
        history!.Data.Should().OnlyContain(x => x.AuditLog.EntityType == nameof(AuditableEntityType.ApiClient));
        history.Data.Should().OnlyContain(x => x.AuditLog.EntityId == createdApiClient.ApiClient.ApiClientId);
        history.Data.Should().Contain(x => x.AuditLog.Operation == nameof(AuditOperation.Created));
        history.Data.Should().Contain(x => x.AuditLog.Operation == nameof(AuditOperation.Updated));

        var changedFields = history.Data.SelectMany(x => x.AuditLog.Changes).Select(x => x.Field).ToList();
        changedFields.Should().Contain("FriendlyName");
        changedFields.Should().NotContain(nameof(CreatedApiClientResponseDto.ClientSecret));

        var renamed = history.Data.First(x => x.AuditLog.Operation == nameof(AuditOperation.Updated));
        renamed.AuditLog.Changes.Should().Contain(x =>
            x.Field == "FriendlyName" &&
            x.OldValue!.GetValue<string>() == createRequest.FriendlyName &&
            x.NewValue!.GetValue<string>() == updateRequest.Body.FriendlyName);
    }

    [Fact]
    public async Task GetApiClientHistory_AfterChangingOnlyTheScopes_ReturnsTheScopeChange()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        var createRequest = new CreateApiClientRequestDto
        {
            FriendlyName = "Scoped client",
            Scopes = [ApiScopeHelper.ToScopeString(ApiScope.RulesEvaluate)]
        };

        var createResponse = await _httpClient.PostAsJsonAsync("/api-clients", createRequest);
        var createdApiClient = await createResponse.Content.ReadDataFromJsonAsync<CreatedApiClientResourceDto>();

        var updateBody = new UpdateApiClientRequestBodyDto
        {
            Scopes = [ApiScopeHelper.ToScopeString(ApiScope.RuleSetsEvaluate)]
        };

        await _httpClient.PatchAsJsonAsync($"/api-clients/{createdApiClient!.ApiClient.ApiClientId}", updateBody);

        // Act
        var response = await _httpClient.GetAsync($"/api-clients/{createdApiClient.ApiClient.ApiClientId}/history");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);

        var history = await response.Content.ReadFromJsonAsync<PagedResponseDto<AuditLogResourceDto>>();
        history.Should().NotBeNull();

        var created = history!.Data.Single(x => x.AuditLog.Operation == nameof(AuditOperation.Created));
        ReadStrings(created.AuditLog.Changes.Single(x => x.Field == nameof(ApiClientEntity.Scopes)).NewValue)
            .Should().BeEquivalentTo(ApiScopeHelper.ToScopeString(ApiScope.RulesEvaluate));

        var updated = history.Data.Single(x => x.AuditLog.Operation == nameof(AuditOperation.Updated));
        var scopeChange = updated.AuditLog.Changes.Single(x => x.Field == nameof(ApiClientEntity.Scopes));
        ReadStrings(scopeChange.OldValue).Should().BeEquivalentTo(ApiScopeHelper.ToScopeString(ApiScope.RulesEvaluate));
        ReadStrings(scopeChange.NewValue).Should().BeEquivalentTo(ApiScopeHelper.ToScopeString(ApiScope.RuleSetsEvaluate));
    }

    [Fact]
    public async Task GetApiClientHistory_AfterChangingOneOfSeveralScopes_ReturnsTheWholeResultingSet()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        var createRequest = new CreateApiClientRequestDto
        {
            FriendlyName = "Scoped client",
            Scopes = [ApiScopeHelper.ToScopeString(ApiScope.RulesEvaluate), ApiScopeHelper.ToScopeString(ApiScope.RuleSetsEvaluate)]
        };

        var createResponse = await _httpClient.PostAsJsonAsync("/api-clients", createRequest);
        var createdApiClient = await createResponse.Content.ReadDataFromJsonAsync<CreatedApiClientResourceDto>();

        var updateBody = new UpdateApiClientRequestBodyDto
        {
            Scopes = [ApiScopeHelper.ToScopeString(ApiScope.RulesEvaluate)]
        };

        await _httpClient.PatchAsJsonAsync($"/api-clients/{createdApiClient!.ApiClient.ApiClientId}", updateBody);

        await _httpClient.PatchAsJsonAsync($"/api-clients/{createdApiClient.ApiClient.ApiClientId}", new UpdateApiClientRequestBodyDto
        {
            FriendlyName = "Renamed client"
        });

        // Act
        var response = await _httpClient.GetAsync($"/api-clients/{createdApiClient.ApiClient.ApiClientId}/history");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var history = await response.Content.ReadFromJsonAsync<PagedResponseDto<AuditLogResourceDto>>();
        history.Should().NotBeNull();

        var scopeChanges = history!.Data
            .Where(x => x.AuditLog.Operation == nameof(AuditOperation.Updated))
            .SelectMany(x => x.AuditLog.Changes)
            .Where(x => x.Field == nameof(ApiClientEntity.Scopes))
            .ToList();

        var narrowed = scopeChanges.Should().ContainSingle().Subject;
        ReadStrings(narrowed.OldValue).Should().BeEquivalentTo(
            ApiScopeHelper.ToScopeString(ApiScope.RulesEvaluate),
            ApiScopeHelper.ToScopeString(ApiScope.RuleSetsEvaluate));

        ReadStrings(narrowed.NewValue).Should().BeEquivalentTo(ApiScopeHelper.ToScopeString(ApiScope.RulesEvaluate));

        var renamed = history.Data
            .Single(x => x.AuditLog.Changes.Any(change => change.Field == "FriendlyName" && change.OldValue != null));

        renamed.AuditLog.Changes.Should().OnlyContain(x => x.Field == "FriendlyName");
    }

    [Fact]
    public async Task GetApiClientSecretHistory_AfterIssuingAndRevoking_ReturnsAuditTrailWithoutTheSecret()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        var createRequest = FakeCreateApiClientRequestDto.CreateValid(_fixture);
        var createResponse = await _httpClient.PostAsJsonAsync("/api-clients", createRequest);
        var createdApiClient = await createResponse.Content.ReadDataFromJsonAsync<CreatedApiClientResourceDto>();

        var secretResponse = await _httpClient.PostAsJsonAsync(
            $"/api-clients/{createdApiClient!.ApiClient.ApiClientId}/secrets", new CreateApiClientSecretRequestBodyDto
            {
                ExpiresAt = null
            });

        var createdSecret = await secretResponse.Content.ReadDataFromJsonAsync<CreatedApiClientSecretResponseDto>();

        await _httpClient.DeleteAsync($"/api-clients/{createdApiClient.ApiClient.ApiClientId}/secrets/{createdSecret!.ApiClientSecretId}");

        // Act
        var response = await _httpClient.GetAsync(
            $"/api-clients/{createdApiClient.ApiClient.ApiClientId}/secrets/{createdSecret.ApiClientSecretId}/history");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);

        var history = await response.Content.ReadFromJsonAsync<PagedResponseDto<AuditLogResourceDto>>();
        history.Should().NotBeNull();
        history!.Data.Should().OnlyContain(x => x.AuditLog.EntityType == nameof(AuditableEntityType.ApiClientSecret));
        history.Data.Should().OnlyContain(x => x.AuditLog.EntityId == createdSecret.ApiClientSecretId);
        history.Data.Should().Contain(x => x.AuditLog.Operation == nameof(AuditOperation.Created));

        var revoked = history.Data.Single(x => x.AuditLog.Operation == nameof(AuditOperation.Updated));
        revoked.AuditLog.Changes.Should().Contain(x => x.Field == nameof(ApiClientSecretEntity.RevokedAt) && x.NewValue != null);

        var changes = history.Data.SelectMany(x => x.AuditLog.Changes).ToList();
        changes.Select(x => x.Field).Should().NotContain([nameof(ApiClientSecretEntity.SecretHash), nameof(ApiClientSecretEntity.SecretPrefix)]);
        SerialisedValues(changes).Should().NotContain(x => x.Contains(createdSecret.ClientSecret));
    }

    [Fact]
    public async Task GetOrganizationHistory_AfterCreatingAndUpdating_ReturnsAuditTrail()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Admin], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        var createRequest = FakeCreateOrganizationRequestDto.CreateValid(_fixture);
        var createResponse = await _httpClient.PostAsJsonAsync("/organizations", createRequest);
        var createdOrganization = await createResponse.Content.ReadDataFromJsonAsync<OrganizationResponseDto>();

        var updateBody = new UpdateOrganizationRequestBodyDto
        {
            FriendlyName = "Renamed organization",
            Status = nameof(OrganizationStatus.Disabled)
        };

        await _httpClient.PatchAsJsonAsync($"/organizations/{createdOrganization!.OrganizationId}", updateBody);

        // Act
        var response = await _httpClient.GetAsync($"/organizations/{createdOrganization.OrganizationId}/history");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);

        var history = await response.Content.ReadFromJsonAsync<PagedResponseDto<AuditLogResourceDto>>();
        history.Should().NotBeNull();
        history!.Data.Should().OnlyContain(x => x.AuditLog.EntityType == nameof(AuditableEntityType.Organization));
        history.Data.Should().OnlyContain(x => x.AuditLog.EntityId == createdOrganization.OrganizationId);
        history.Data.Should().Contain(x => x.AuditLog.Operation == nameof(AuditOperation.Created));

        var updated = history.Data.First(x => x.AuditLog.Operation == nameof(AuditOperation.Updated));
        updated.AuditLog.Changes.Should().Contain(x => x.Field == "FriendlyName" && x.NewValue!.GetValue<string>() == updateBody.FriendlyName);
        updated.AuditLog.Changes.Should().Contain(x => x.Field == "Status" && x.NewValue!.GetValue<string>() == nameof(OrganizationStatus.Disabled));
    }

    [Fact]
    public async Task GetWebhookConfigurationHistory_AfterCreatingAndUpdating_ReturnsAuditTrail()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Admin], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        var createRequest = FakeCreateWebhookConfigurationRequestDto.CreateValid(_fixture);
        var createResponse = await _httpClient.PostAsJsonAsync($"/organizations/{OrganizationId}/webhook-configurations", createRequest.Body);
        var createdWebhookConfiguration = await createResponse.Content.ReadDataFromJsonAsync<WebhookConfigurationSecretResponseDto>();

        var updateRequest = FakeUpdateWebhookConfigurationRequestDto.CreateValid(_fixture);
        await _httpClient.PatchAsJsonAsync(
            $"/organizations/{OrganizationId}/webhook-configurations/{createdWebhookConfiguration!.WebhookConfigurationId}", updateRequest.Body);

        // Act
        var response = await _httpClient.GetAsync(
            $"/organizations/{OrganizationId}/webhook-configurations/{createdWebhookConfiguration.WebhookConfigurationId}/history");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);

        var history = await response.Content.ReadFromJsonAsync<PagedResponseDto<AuditLogResourceDto>>();
        history.Should().NotBeNull();
        history!.Data.Should().OnlyContain(x => x.AuditLog.EntityType == nameof(AuditableEntityType.WebhookConfiguration));
        history.Data.Should().OnlyContain(x => x.AuditLog.EntityId == createdWebhookConfiguration.WebhookConfigurationId);
        history.Data.Should().Contain(x => x.AuditLog.Operation == nameof(AuditOperation.Created));

        var updated = history.Data.First(x => x.AuditLog.Operation == nameof(AuditOperation.Updated));
        updated.AuditLog.Changes.Should().Contain(x => x.Field == "EndpointUrl" && x.NewValue!.GetValue<string>() == updateRequest.Body.EndpointUrl);
    }

    [Fact]
    public async Task GetUserHistory_AfterProvisioningAndTransferring_ReturnsAuditTrail()
    {
        // Arrange
        var admin = await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Admin], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        var member = await AuthenticationHelper.CreateUserAsync(
            testFixture.Services, "member@mail.com", Password, [UserRole.User], SystemConstants.DefaultOrganization);

        await _httpClient.PutAsync($"/organizations/{OrganizationId}/users/{member.Id}", null);

        // Act
        var response = await _httpClient.GetAsync($"/organizations/{OrganizationId}/users/{member.Id}/history");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);

        var history = await response.Content.ReadFromJsonAsync<PagedResponseDto<AuditLogResourceDto>>();
        history.Should().NotBeNull();
        history!.Data.Should().OnlyContain(x => x.AuditLog.EntityType == nameof(AuditableEntityType.ApplicationUser));
        history.Data.Should().OnlyContain(x => x.AuditLog.EntityId == member.Id);

        var transferred = history.Data.Single(x => x.AuditLog.Operation == nameof(AuditOperation.Updated));
        transferred.AuditLog.ChangedByUserId.Should().Be(admin.Id);
        transferred.AuditLog.Changes.Should().Contain(x =>
            x.Field == "OrganizationId" &&
            x.NewValue!.GetValue<Guid>() == OrganizationId);

        var changedFields = history.Data.SelectMany(x => x.AuditLog.Changes).Select(x => x.Field).ToList();
        changedFields.Should().NotContain("PasswordHash");
        changedFields.Should().NotContain("SecurityStamp");
    }

    [Fact]
    public async Task GetWebhookConfigurationHistory_AfterRotatingTheSecret_RecordsTheRotationWithoutTheSecret()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Admin], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        var createRequest = FakeCreateWebhookConfigurationRequestDto.CreateValid(_fixture);
        var createResponse = await _httpClient.PostAsJsonAsync($"/organizations/{OrganizationId}/webhook-configurations", createRequest.Body);
        var createdWebhookConfiguration = await createResponse.Content.ReadDataFromJsonAsync<WebhookConfigurationSecretResponseDto>();

        var rotateResponse = await _httpClient.PostAsync(
            $"/organizations/{OrganizationId}/webhook-configurations/{createdWebhookConfiguration!.WebhookConfigurationId}/rotate-secret", null);

        var rotatedWebhookConfiguration = await rotateResponse.Content.ReadDataFromJsonAsync<WebhookConfigurationSecretResponseDto>();

        // Act
        var response = await _httpClient.GetAsync(
            $"/organizations/{OrganizationId}/webhook-configurations/{createdWebhookConfiguration.WebhookConfigurationId}/history");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);

        var history = await response.Content.ReadFromJsonAsync<PagedResponseDto<AuditLogResourceDto>>();
        history.Should().NotBeNull();

        var rotated = history!.Data.Single(x => x.AuditLog.Operation == nameof(AuditOperation.Updated));
        rotated.AuditLog.Changes.Should().ContainSingle();
        rotated.AuditLog.Changes.Single().Field.Should().Be(nameof(WebhookConfigurationEntity.SecretKeyRotatedAt));
        rotated.AuditLog.Changes.Single().NewValue.Should().NotBeNull();

        var changes = history.Data.SelectMany(x => x.AuditLog.Changes).ToList();
        changes.Select(x => x.Field).Should().NotContain(nameof(WebhookConfigurationEntity.SecretKey));
        SerialisedValues(changes).Should().NotContain(x => x.Contains(rotatedWebhookConfiguration!.SecretKey));
        SerialisedValues(changes).Should().NotContain(x => x.Contains(createdWebhookConfiguration.SecretKey));
    }

    [Fact]
    public async Task GetUserHistory_AfterGrantingAndRevokingARole_ReturnsTheRoleChanges()
    {
        // Arrange
        var admin = await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Admin], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        var member = await AuthenticationHelper.CreateUserAsync(
            testFixture.Services, "member@mail.com", Password, [UserRole.User], OrganizationId);

        await _httpClient.PutAsync($"/users/{member.Id}/roles/{nameof(UserRole.Manager)}", null);
        await _httpClient.DeleteAsync($"/users/{member.Id}/roles/{nameof(UserRole.Manager)}");

        // Act
        var response = await _httpClient.GetAsync($"/organizations/{OrganizationId}/users/{member.Id}/history");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);

        var history = await response.Content.ReadFromJsonAsync<PagedResponseDto<AuditLogResourceDto>>();
        history.Should().NotBeNull();
        history!.Data.Should().OnlyContain(x => x.AuditLog.EntityType == nameof(AuditableEntityType.ApplicationUser));
        history.Data.Should().OnlyContain(x => x.AuditLog.EntityId == member.Id);

        var roleChanges = history.Data
            .Where(x => x.AuditLog.Operation == nameof(AuditOperation.Updated))
            .SelectMany(x => x.AuditLog.Changes)
            .Where(x => x.Field == nameof(ApplicationUserEntity.Roles))
            .ToList();

        var granted = roleChanges.Single(x => ReadStrings(x.NewValue).Contains(nameof(UserRole.Manager)));
        ReadStrings(granted.OldValue).Should().BeEquivalentTo(nameof(UserRole.User));
        ReadStrings(granted.NewValue).Should().BeEquivalentTo(nameof(UserRole.Manager), nameof(UserRole.User));

        var revoked = roleChanges.Single(x => ReadStrings(x.OldValue).Contains(nameof(UserRole.Manager)));
        ReadStrings(revoked.NewValue).Should().BeEquivalentTo(nameof(UserRole.User));

        history.Data.Should().OnlyContain(x => x.AuditLog.ChangedByUserId == null || x.AuditLog.ChangedByUserId == admin.Id);
    }

    private static List<string> SerialisedValues(IEnumerable<AuditLogChangeDto> changes)
    {
        return changes
            .SelectMany(x => new[] { x.OldValue, x.NewValue })
            .Select(x => x?.ToJsonString() ?? string.Empty)
            .ToList();
    }

    private static List<string> ReadStrings(JsonNode? node)
    {
        return node?.AsArray().Select(x => x!.GetValue<string>()).ToList() ?? [];
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
