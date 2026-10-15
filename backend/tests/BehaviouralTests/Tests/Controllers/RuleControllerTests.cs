using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Nodes;
using AutoFixture;
using BehaviouralTests.TestHelpers;
using Core.Enums;
using Core.Extensions;
using Core.Models.RuleEvaluations;
using Core.Models.Rules;
using Core.ValueObjects;
using FluentAssertions;
using Infrastructure;
using Infrastructure.Configurations;
using Infrastructure.Entities;
using Infrastructure.Mappers;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Constants;
using Presentation.Models.Rules;
using Presentation.Models.Transactions;
using UnitTests.TestHelpers.FakeObjects.Core.Models.ApiClients;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Users;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.Rules;

namespace BehaviouralTests.Tests.Controllers;

public class RuleControllerTests(TestFixture testFixture) : IClassFixture<TestFixture>, IAsyncLifetime
{
    private readonly Fixture _fixture = AutoFixtureHelper.Create();
    private readonly HttpClient _httpClient = testFixture.CreateClient();

    private const string Email = "username@mail.com";
    private const string Password = "P@$$w0rd";
    private static readonly Guid OrganizationId = new("4d6389e9-f5d1-4325-a004-915865e3adec");
    private static readonly Guid DisabledOrganizationId = new("03a9b48e-4af2-40bd-a180-cac58bd930f3");

    [Fact]
    public async Task ValidateRule_NotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var validateRequest = FakeRuleBodyDto.CreateBasicRule(_fixture);

        // Act
        var response = await _httpClient.PostAsJsonAsync("/rules/validate", validateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task ValidateRule_GivenInvalidRequest_ShouldReturnBadRequest()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var validateRequest = FakeRuleBodyDto.CreateBasicRule(_fixture) with
        {
            Condition = RuleCondition.IsNull.ToString()
        };

        // Act
        var response = await _httpClient.PostAsJsonAsync("/rules/validate", validateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task ValidateRule_GivenValidRequest_ShouldReturnOk()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var validateRequest = FakeRuleBodyDto.CreateBasicRule(_fixture);

        // Act
        var response = await _httpClient.PostAsJsonAsync("/rules/validate", validateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task TestRule_NotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var testRequest = FakeTestRuleRequestDto.CreateValid(_fixture);

        // Act
        var response = await _httpClient.PostAsJsonAsync("/rules/test", testRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task TestRule_GivenInvalidRequest_ShouldReturnBadRequest()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var testRequest = FakeTestRuleRequestDto.CreateValid(_fixture) with
        {
            RuleBody = FakeRuleBodyDto.CreateBasicRule(_fixture) with
            {
                Condition = RuleCondition.IsNull.ToString()
            }
        };

        // Act
        var response = await _httpClient.PostAsJsonAsync("/rules/test", testRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task TestRule_GivenValidRequest_ShouldReturnOk()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var testRequest = FakeTestRuleRequestDto.CreateValid(_fixture);

        // Act
        var response = await _httpClient.PostAsJsonAsync("/rules/test", testRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);

        var createdRule = await response.Content.ReadDataFromJsonAsync<TestRuleResponseDto>();
        createdRule.Should().NotBeNull();
        createdRule!.Matched.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateRule_NotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var updateRequest = FakeUpdateRuleRequestDto.CreateValid(_fixture);

        // Act
        var response = await _httpClient.PatchAsJsonAsync($"/rules/{updateRequest.RuleId}", updateRequest.Body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task UpdateRule_NotManager_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var updateRequest = FakeUpdateRuleRequestDto.CreateValid(_fixture);

        // Act
        var response = await _httpClient.PatchAsJsonAsync($"/rules/{updateRequest.RuleId}", updateRequest.Body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task UpdateRule_GivenValidRequest_ShouldUpdateMetadataWithoutTouchingVersion()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var ruleSetEntity = FakeRuleSetEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            IsDeleted = false,
            Rules =
            [
                FakeRuleEntity.CreateValid(_fixture) with
                {
                    OrganizationId = OrganizationId,
                    Organization = null!,
                    IsEnabled = true,
                    IsDeleted = false
                }
            ]
        };

        await DatabaseHelper.CreateRuleSets(testFixture.Services, [ruleSetEntity]);

        var seededRule = ruleSetEntity.Rules.First();
        var updateRequest = FakeUpdateRuleRequestDto.CreateValid(_fixture) with
        {
            RuleId = seededRule.RuleId,
            OrganizationId = OrganizationId,
            Body = new UpdateRuleRequestBodyDto
            {
                FriendlyName = "Renamed rule",
                IsEnabled = false
            }
        };

        // Act
        var response = await _httpClient.PatchAsJsonAsync($"/rules/{updateRequest.RuleId}", updateRequest.Body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var ruleEntity = context.Rules.First(x => x.RuleId == updateRequest.RuleId);
        ruleEntity.FriendlyName.Should().Be("Renamed rule");
        ruleEntity.IsEnabled.Should().BeFalse();

        ruleEntity.CurrentRuleVersionId.Should().Be(seededRule.CurrentRuleVersionId);
        ruleEntity.CurrentVersionNumber.Should().Be(seededRule.CurrentVersionNumber);
        context.RuleVersions.Count(x => x.RuleId == updateRequest.RuleId).Should().Be(1);
    }

    [Fact]
    public async Task UpdateRule_GivenNoFields_ShouldReturnBadRequest()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var ruleSetEntity = FakeRuleSetEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            IsDeleted = false,
            Rules =
            [
                FakeRuleEntity.CreateValid(_fixture) with
                {
                    OrganizationId = OrganizationId,
                    Organization = null!,
                    IsEnabled = true,
                    IsDeleted = false
                }
            ]
        };

        await DatabaseHelper.CreateRuleSets(testFixture.Services, [ruleSetEntity]);

        var updateRequest = FakeUpdateRuleRequestDto.CreateValid(_fixture) with
        {
            RuleId = ruleSetEntity.Rules.First().RuleId,
            OrganizationId = OrganizationId,
            Body = new UpdateRuleRequestBodyDto
            {
                FriendlyName = null,
                IsEnabled = null
            }
        };

        // Act
        var response = await _httpClient.PatchAsJsonAsync($"/rules/{updateRequest.RuleId}", updateRequest.Body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Theory]
    [InlineData(RuleAction.Flag, false)]
    [InlineData(RuleAction.Review, true)]
    [InlineData(RuleAction.Block, true)]
    public async Task EvaluateRule_RuleFires_ActionDecidesWhetherACaseIsOpened(RuleAction action, bool expectsCase)
    {
        // Arrange
        var ruleEntity = await SeedFiringRuleAsync(action);
        await AuthenticateAsApiClientAsync();

        // Act
        var response = await _httpClient.PostAsJsonAsync(
            $"/rules/{ruleEntity.RuleId}/evaluate",
            FakeJsonObject.CreateValid(_fixture));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadDataFromJsonAsync<EvaluateRuleResourceDto>();
        body.Should().NotBeNull();
        body!.Result.Matched.Should().BeTrue();
        body.Result.Policy.Action.Should().Be(action.ToString());
        body.Result.Outcome!.Action.Should().Be(action.ToString());

        if (expectsCase)
        {
            body.Result.Case.Should().NotBeNull();
        }
        else
        {
            body.Result.Case.Should().BeNull();
        }
    }

    [Fact]
    public async Task EvaluateRule_FlagRuleClearsRuleSetThreshold_EscalatesToReviewAndOpensCase()
    {
        // Arrange
        var ruleEntity = await SeedFiringRuleAsync(RuleAction.Flag, riskWeight: 30, reviewThreshold: 30);
        await AuthenticateAsApiClientAsync();

        // Act
        var response = await _httpClient.PostAsJsonAsync(
            $"/rules/{ruleEntity.RuleId}/evaluate",
            FakeJsonObject.CreateValid(_fixture));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadDataFromJsonAsync<EvaluateRuleResourceDto>();
        body.Should().NotBeNull();
        body!.Result.Policy.Action.Should().Be(nameof(RuleAction.Flag));
        body.Result.Outcome!.Action.Should().Be(nameof(RuleAction.Review));
        body.Result.Outcome.RiskScore.Should().Be(30);
        body.Result.Case.Should().NotBeNull();
    }

    [Theory]
    [InlineData(UserRole.Manager)]
    [InlineData(UserRole.Admin)]
    public async Task EvaluateRule_AuthenticatedAsPrivilegedUser_ShouldReturnOk(UserRole role)
    {
        // Arrange
        var ruleEntity = await SeedFiringRuleAsync(RuleAction.Flag);
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [role], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.PostAsJsonAsync(
            $"/rules/{ruleEntity.RuleId}/evaluate",
            FakeJsonObject.CreateValid(_fixture));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadDataFromJsonAsync<EvaluateRuleResourceDto>();
        body.Should().NotBeNull();
        body!.Result.Matched.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateRule_RuleOpensCase_ShouldRecordAHeldDecisionCarriedByTheTransaction()
    {
        // Arrange
        var ruleEntity = await SeedFiringRuleAsync(RuleAction.Review);
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.Manager], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.CreateCase, "true");
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.TransactionReference, "pay_single_rule");

        // Act
        var response = await _httpClient.PostAsJsonAsync(
            $"/rules/{ruleEntity.RuleId}/evaluate",
            FakeJsonObject.CreateValid(_fixture));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadDataFromJsonAsync<EvaluateRuleResourceDto>();
        body.Should().NotBeNull();
        body!.Result.Case.Should().NotBeNull();
        body.Result.Decision.Should().NotBeNull();
        body.Result.Decision!.Status.Should().Be(nameof(DecisionStatus.Held));
        body.Result.Decision.ReasonCode.Should().Be(nameof(DecisionReasonCode.RuleReviewRequired));
        body.Result.Decision.CaseId.Should().Be(body.Result.Case!.CaseId);
        body.Result.Decision.TransactionReference.Should().Be("pay_single_rule");

        var lookupResponse = await _httpClient.GetAsync($"/transactions/{body.Result.TransactionId}");
        lookupResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var transaction = await lookupResponse.Content.ReadDataFromJsonAsync<TransactionResourceDto>();
        transaction.Should().NotBeNull();
        transaction!.Transaction.Decision.Should().NotBeNull();
        transaction.Transaction.Decision!.DecisionId.Should().Be(body.Result.Decision.DecisionId);
        transaction.Transaction.Decision.Status.Should().Be(nameof(DecisionStatus.Held));
    }

    [Fact]
    public async Task EvaluateRule_ReviewWithoutACase_ShouldReleaseTheDecisionAndStillReportItsFiredRules()
    {
        // Arrange
        var ruleEntity = await SeedFiringRuleAsync(RuleAction.Review);
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.Manager], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.CreateCase, "false");
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.TransactionReference, "pay_no_case");

        // Act
        var response = await _httpClient.PostAsJsonAsync(
            $"/rules/{ruleEntity.RuleId}/evaluate",
            FakeJsonObject.CreateValid(_fixture));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadDataFromJsonAsync<EvaluateRuleResourceDto>();
        body.Should().NotBeNull();
        body!.Result.Case.Should().BeNull();
        body.Result.Decision.Should().NotBeNull();
        body.Result.Decision!.CaseId.Should().BeNull();

        body.Result.Decision.Status.Should().Be(nameof(DecisionStatus.Released));
        body.Result.Decision.ReasonCode.Should().Be(nameof(DecisionReasonCode.RuleReviewRequired));

        body.Result.Decision.FiredRules.Should().ContainSingle();
        body.Result.Decision.FiredRules.Single().RuleId.Should().Be(ruleEntity.RuleId);
    }

    [Fact]
    public async Task UpdateRule_GivenAlertPolicy_ShouldSetIt()
    {
        // Arrange
        var ruleEntity = await SeedFiringRuleAsync(RuleAction.Review);
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.PatchAsync(
            $"/rules/{ruleEntity.RuleId}",
            new StringContent("""{"alertSubjectRole":"Beneficiary","deduplicationWindow":30}""", Encoding.UTF8, "application/json"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var updated = context.Rules.First(x => x.RuleId == ruleEntity.RuleId);
        updated.AlertSubjectRoleId.Should().Be((int)PartyRole.Beneficiary);
        updated.DeduplicationWindow.Should().Be(30);
    }

    [Fact]
    public async Task UpdateRule_GivenExplicitNullAlertPolicy_ShouldClearItBackToInherit()
    {
        // Arrange
        var ruleEntity = await SeedFiringRuleAsync(
            RuleAction.Review, alertSubjectRole: PartyRole.Beneficiary, deduplicationWindow: 30);

        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.PatchAsync(
            $"/rules/{ruleEntity.RuleId}",
            new StringContent("""{"alertSubjectRole":null,"deduplicationWindow":null}""", Encoding.UTF8, "application/json"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var updated = context.Rules.First(x => x.RuleId == ruleEntity.RuleId);
        updated.AlertSubjectRoleId.Should().BeNull();
        updated.DeduplicationWindow.Should().BeNull();
    }

    [Fact]
    public async Task UpdateRule_OmittingAlertPolicy_ShouldLeaveItAlone()
    {
        // Arrange
        var ruleEntity = await SeedFiringRuleAsync(
            RuleAction.Review, alertSubjectRole: PartyRole.Beneficiary, deduplicationWindow: 30);

        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.PatchAsync(
            $"/rules/{ruleEntity.RuleId}",
            new StringContent("""{"friendlyName":"Renamed"}""", Encoding.UTF8, "application/json"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var updated = context.Rules.First(x => x.RuleId == ruleEntity.RuleId);
        updated.AlertSubjectRoleId.Should().Be((int)PartyRole.Beneficiary);
        updated.DeduplicationWindow.Should().Be(30);
    }

    [Theory]
    [InlineData("""{"alertSubjectRole":"Nobody"}""")]
    [InlineData("""{"deduplicationWindow":0}""")]
    [InlineData("""{"deduplicationWindow":-5}""")]
    public async Task UpdateRule_GivenAnUnusableAlertPolicy_ShouldReturnBadRequest(string json)
    {
        // Arrange
        var ruleEntity = await SeedFiringRuleAsync(RuleAction.Review);
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.PatchAsync(
            $"/rules/{ruleEntity.RuleId}",
            new StringContent(json, Encoding.UTF8, "application/json"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateRule_GivenExplicitNullHoldPolicy_ShouldClearItBackToInherit()
    {
        // Arrange
        var ruleEntity = await SeedFiringRuleAsync(RuleAction.Review, holdWindow: 90, holdExpiryVerdict: HoldExpiryVerdict.Never);
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.PatchAsync(
            $"/rules/{ruleEntity.RuleId}",
            new StringContent("""{"holdWindow":null,"holdExpiryVerdict":null}""", Encoding.UTF8, "application/json"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var updated = context.Rules.First(x => x.RuleId == ruleEntity.RuleId);
        updated.HoldWindow.Should().BeNull();
        updated.HoldExpiryVerdictId.Should().BeNull();
    }

    [Fact]
    public async Task UpdateRule_OmittingHoldPolicy_ShouldLeaveItAlone()
    {
        // Arrange
        var ruleEntity = await SeedFiringRuleAsync(RuleAction.Review, holdWindow: 90, holdExpiryVerdict: HoldExpiryVerdict.Never);
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.PatchAsync(
            $"/rules/{ruleEntity.RuleId}",
            new StringContent("""{"friendlyName":"Renamed"}""", Encoding.UTF8, "application/json"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var updated = context.Rules.First(x => x.RuleId == ruleEntity.RuleId);
        updated.FriendlyName.Should().Be("Renamed");
        updated.HoldWindow.Should().Be(90);
        updated.HoldExpiryVerdictId.Should().Be((int)HoldExpiryVerdict.Never);
    }

    [Fact]
    public async Task UpdateRule_GivenHoldPolicyValues_ShouldSetThem()
    {
        // Arrange
        var ruleEntity = await SeedFiringRuleAsync(RuleAction.Review);
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.PatchAsync(
            $"/rules/{ruleEntity.RuleId}",
            new StringContent("""{"holdWindow":45,"holdExpiryVerdict":"Release"}""", Encoding.UTF8, "application/json"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var updated = context.Rules.First(x => x.RuleId == ruleEntity.RuleId);
        updated.HoldWindow.Should().Be(45);
        updated.HoldExpiryVerdictId.Should().Be((int)HoldExpiryVerdict.Release);
    }

    [Fact]
    public async Task UpdateRule_GivenOnlyAClearedHoldPolicy_ShouldCountAsAFieldProvided()
    {
        // Arrange
        var ruleEntity = await SeedFiringRuleAsync(RuleAction.Review, holdWindow: 90);
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.PatchAsync(
            $"/rules/{ruleEntity.RuleId}",
            new StringContent("""{"holdWindow":null}""", Encoding.UTF8, "application/json"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task EvaluateRule_RuleCarriesAHoldWindow_ShouldStampTheDecisionDeadlineFromIt()
    {
        // Arrange
        var ruleEntity = await SeedFiringRuleAsync(RuleAction.Review, holdWindow: 90, holdExpiryVerdict: HoldExpiryVerdict.Release);
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.Manager], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.CreateCase, "true");
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.TransactionReference, "pay_hold_window");

        // Act
        var response = await _httpClient.PostAsJsonAsync(
            $"/rules/{ruleEntity.RuleId}/evaluate",
            FakeJsonObject.CreateValid(_fixture));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadDataFromJsonAsync<EvaluateRuleResourceDto>();
        body.Should().NotBeNull();
        body!.Result.Decision.Should().NotBeNull();
        body.Result.Decision!.Status.Should().Be(nameof(DecisionStatus.Held));
        body.Result.Decision.HoldExpiryVerdict.Should().Be(nameof(HoldExpiryVerdict.Release));
        body.Result.Decision.HeldAt.Should().NotBeNull();
        body.Result.Decision.ExpiresAt.Should().Be(body.Result.Decision.HeldAt!.Value.AddMinutes(90));
    }

    [Fact]
    public async Task EvaluateRule_RuleCarriesNoHoldWindow_ShouldFallBackToTheConfiguredDefault()
    {
        // Arrange
        var ruleEntity = await SeedFiringRuleAsync(RuleAction.Review);
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.Manager], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.CreateCase, "true");
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.TransactionReference, "pay_default_hold");

        // Act
        var response = await _httpClient.PostAsJsonAsync(
            $"/rules/{ruleEntity.RuleId}/evaluate",
            FakeJsonObject.CreateValid(_fixture));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadDataFromJsonAsync<EvaluateRuleResourceDto>();
        body.Should().NotBeNull();
        body!.Result.Decision.Should().NotBeNull();
        body.Result.Decision!.HoldExpiryVerdict.Should().Be(nameof(HoldExpiryVerdict.Block));
        body.Result.Decision.ExpiresAt.Should().Be(body.Result.Decision.HeldAt!.Value.AddMinutes(1440));
    }

    [Fact]
    public async Task EvaluateRule_RuleNeverExpires_ShouldHoldWithoutADeadline()
    {
        // Arrange
        var ruleEntity = await SeedFiringRuleAsync(RuleAction.Review, holdWindow: 30, holdExpiryVerdict: HoldExpiryVerdict.Never);
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.Manager], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.CreateCase, "true");
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.TransactionReference, "pay_never_expires");

        // Act
        var response = await _httpClient.PostAsJsonAsync(
            $"/rules/{ruleEntity.RuleId}/evaluate",
            FakeJsonObject.CreateValid(_fixture));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadDataFromJsonAsync<EvaluateRuleResourceDto>();
        body.Should().NotBeNull();
        body!.Result.Decision.Should().NotBeNull();
        body.Result.Decision!.Status.Should().Be(nameof(DecisionStatus.Held));
        body.Result.Decision.HoldExpiryVerdict.Should().Be(nameof(HoldExpiryVerdict.Never));

        body.Result.Decision.ExpiresAt.Should().BeNull();
    }

    [Fact]
    public async Task EvaluateRule_RuleDoesNotHold_ShouldLeaveTheDecisionDeadlineUnset()
    {
        // Arrange
        var ruleEntity = await SeedFiringRuleAsync(RuleAction.Block, holdWindow: 90, holdExpiryVerdict: HoldExpiryVerdict.Release);
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.Manager], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.CreateCase, "true");
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.TransactionReference, "pay_no_hold");

        // Act
        var response = await _httpClient.PostAsJsonAsync(
            $"/rules/{ruleEntity.RuleId}/evaluate",
            FakeJsonObject.CreateValid(_fixture));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadDataFromJsonAsync<EvaluateRuleResourceDto>();
        body.Should().NotBeNull();
        body!.Result.Decision.Should().NotBeNull();
        body.Result.Decision!.Status.Should().Be(nameof(DecisionStatus.Blocked));
        body.Result.Decision.ExpiresAt.Should().BeNull();
        body.Result.Decision.HoldExpiryVerdict.Should().BeNull();
    }

    [Fact]
    public async Task TestRule_GivenANestedPath_ShouldReportAMatch()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        var body = new JsonObject
        {
            ["ruleBody"] = new JsonObject
            {
                ["condition"] = nameof(RuleCondition.StartsWith),
                ["field"] = "card.bin",
                ["value"] = "4532"
            },
            ["payload"] = new JsonObject
            {
                ["card"] = new JsonObject { ["bin"] = "453201" }
            }
        };

        // Act
        var response = await _httpClient.PostAsJsonAsync("/rules/test", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadDataFromJsonAsync<TestRuleResponseDto>();
        result!.Matched.Should().BeTrue();
    }

    [Fact]
    public async Task TestRule_GivenAQuantifiedPathOverAnArray_ShouldReportAMatch()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        var body = new JsonObject
        {
            ["ruleBody"] = new JsonObject
            {
                ["condition"] = nameof(RuleCondition.In),
                ["field"] = "parties[*].country",
                ["quantifier"] = nameof(RuleQuantifier.Any),
                ["value"] = new JsonArray("IR", "KP")
            },
            ["payload"] = Parties()
        };

        // Act
        var response = await _httpClient.PostAsJsonAsync("/rules/test", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadDataFromJsonAsync<TestRuleResponseDto>();
        result!.Matched.Should().BeTrue();
    }

    [Fact]
    public async Task TestRule_GivenAnAggregateExpression_ShouldReportAMatch()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        var body = new JsonObject
        {
            ["ruleBody"] = new JsonObject
            {
                ["condition"] = nameof(RuleCondition.GreaterThan),
                ["expression"] = new JsonObject
                {
                    ["function"] = nameof(RuleFunction.Sum),
                    ["arguments"] = new JsonArray(new JsonObject { ["field"] = "parties[*].amount" })
                },
                ["value"] = 250
            },
            ["payload"] = Parties()
        };

        // Act
        var response = await _httpClient.PostAsJsonAsync("/rules/test", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadDataFromJsonAsync<TestRuleResponseDto>();
        result!.Matched.Should().BeTrue();
    }

    [Fact]
    public async Task TestRule_GivenABetweenCondition_ShouldReportAMatch()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        var body = new JsonObject
        {
            ["ruleBody"] = new JsonObject
            {
                ["condition"] = nameof(RuleCondition.Between),
                ["field"] = "amount",
                ["value"] = new JsonArray(200, 400)
            },
            ["payload"] = Parties()
        };

        // Act
        var response = await _httpClient.PostAsJsonAsync("/rules/test", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadDataFromJsonAsync<TestRuleResponseDto>();
        result!.Matched.Should().BeTrue();
    }

    [Fact]
    public async Task TestRule_GivenAWildcardWithoutAQuantifier_ShouldReturnBadRequest()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        var body = new JsonObject
        {
            ["ruleBody"] = new JsonObject
            {
                ["condition"] = nameof(RuleCondition.EqualTo),
                ["field"] = "parties[*].country",
                ["value"] = "IR"
            },
            ["payload"] = Parties()
        };

        // Act
        var response = await _httpClient.PostAsJsonAsync("/rules/test", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task EvaluateRule_GivenAQuantifiedPath_ShouldRecordEveryValueItWeighed()
    {
        // Arrange
        var ruleBody = new RuleBody
        {
            Condition = RuleCondition.In,
            Field = new RuleField("parties[*].country"),
            Quantifier = RuleQuantifier.Any,
            Value = new RuleValue(new List<object?> { "IR", "KP" }),
            And = null,
            Or = null,
            Not = null
        };

        var ruleEntity = await SeedFiringRuleAsync(RuleAction.Flag, ruleBody: ruleBody);
        await AuthenticateAsApiClientAsync();

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/rules/{ruleEntity.RuleId}/evaluate", Parties());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
        var evaluation = context.RuleEvaluations.Single(x => x.RuleId == ruleEntity.RuleId);
        evaluation.Matched.Should().BeTrue();

        var conditionMatches = evaluation.ConditionMatches!.FromBase64String().TryDeserialize<List<ConditionMatch>>();
        conditionMatches.Should().ContainSingle();
        conditionMatches[0].Field.Should().Be("parties[*].country");
        conditionMatches[0].Quantifier.Should().Be(RuleQuantifier.Any);
        conditionMatches[0].Actual!.ToJsonString().Should().Be("""["GB","IR"]""");
    }

    private static JsonObject Parties()
    {
        return new JsonObject
        {
            ["amount"] = 300,
            ["parties"] = new JsonArray(
                new JsonObject { ["country"] = "GB", ["amount"] = 100 },
                new JsonObject { ["country"] = "IR", ["amount"] = 200 })
        };
    }

    private async Task<RuleEntity> SeedFiringRuleAsync(
        RuleAction action,
        int riskWeight = 0,
        int? reviewThreshold = null,
        int? holdWindow = null,
        HoldExpiryVerdict? holdExpiryVerdict = null,
        PartyRole? alertSubjectRole = null,
        int? deduplicationWindow = null,
        RuleBody? ruleBody = null)
    {
        ruleBody ??= new RuleBody
        {
            Condition = RuleCondition.EqualTo,
            Field = new RuleField("Key"),
            Value = new RuleValue("Value"),
            And = null,
            Or = null,
            Not = null
        };

        var ruleEntity = FakeRuleEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            RuleBody = ruleBody.ToBase64String(),
            ActionId = (int)action,
            SeverityId = (int)RuleSeverity.Medium,
            RiskWeight = riskWeight,
            HoldWindow = holdWindow,
            HoldExpiryVerdictId = (int?)holdExpiryVerdict,
            AlertSubjectRoleId = (int?)alertSubjectRole,
            DeduplicationWindow = deduplicationWindow,
            IsEnabled = true,
            IsDeleted = false
        };

        var ruleSetEntity = FakeRuleSetEntity.CreateValid(_fixture) with
        {
            RuleSetId = ruleEntity.RuleSetId,
            OrganizationId = OrganizationId,
            Organization = null!,
            ReviewThreshold = reviewThreshold,
            IsEnabled = true,
            IsDeleted = false,
            Rules = [ruleEntity]
        };

        await DatabaseHelper.CreateRuleSets(testFixture.Services, [ruleSetEntity]);

        return ruleEntity;
    }

    private async Task AuthenticateAsApiClientAsync()
    {
        var applicationUserEntity = (FakeApplicationUser.CreateValid(_fixture) with
        {
            OrganizationId = new OrganizationId(OrganizationId),
            IsDeleted = new IsDeleted(false)
        }).ToEntity();

        await DatabaseHelper.CreateApplicationUsers(testFixture.Services, [applicationUserEntity]);

        var createApiClientRequest = FakeCreateApiClientRequest.CreateValid(_fixture) with
        {
            OrganizationId = new OrganizationId(OrganizationId),
            UserId = new ApplicationUserId(applicationUserEntity.Id)
        };

        var createdApiClient = await AuthenticationHelper.CreateApiClientAsync(testFixture.Services, createApiClientRequest);
        await AuthenticationHelper.SetClientCredentialsTokenAsync(_httpClient, createdApiClient.ApiClient.ClientId, createdApiClient.ClientSecret);
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.CreateCase, "true");
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
            },
            FakeOrganizationEntity.CreateValid(_fixture) with
            {
                OrganizationId = DisabledOrganizationId,
                StatusId = (int)OrganizationStatus.Disabled,
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
