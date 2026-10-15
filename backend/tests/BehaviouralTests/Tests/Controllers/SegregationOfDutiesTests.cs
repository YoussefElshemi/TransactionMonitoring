using System.Net;
using System.Net.Http.Json;
using System.Text;
using AutoFixture;
using BehaviouralTests.TestHelpers;
using Core.Enums;
using FluentAssertions;
using Infrastructure;
using Infrastructure.Configurations;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Models.Approvals;
using Presentation.Models.AuditLogs;
using Presentation.Models.Cases;
using Presentation.Models.Common;
using Presentation.Models.Rules;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

namespace BehaviouralTests.Tests.Controllers;

public class SegregationOfDutiesTests(TestFixture testFixture) : IClassFixture<TestFixture>, IAsyncLifetime
{
    private readonly Fixture _fixture = AutoFixtureHelper.Create();
    private readonly HttpClient _authorClient = testFixture.CreateClient();
    private readonly HttpClient _reviewerClient = testFixture.CreateClient();

    private const string AuthorEmail = "author@mail.com";
    private const string ReviewerEmail = "reviewer@mail.com";
    private const string Password = "P@$$w0rd";
    private static readonly Guid OrganizationId = new("2f9c6b41-5d83-47ae-9c02-6b1e4f7a8d35");

    private ApplicationUserEntity _author = null!;
    private ApplicationUserEntity _reviewer = null!;

    [Fact]
    public async Task DeployRule_WithNoPolicy_DeploysWithoutAskingAnybody()
    {
        // Arrange
        await ArrangeAsync();
        var rule = await CreateRuleWithSecondVersionAsync();

        // Act
        var response = await DeployAsync(rule.Rule.RuleId);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var deployment = await response.Content.ReadDataFromJsonAsync<RuleDeploymentResourceDto>();
        deployment!.RuleDeployment.Status.Should().Be(nameof(RuleDeploymentStatus.Completed));

        (await CurrentVersionNumberAsync(rule.Rule.RuleId)).Should().Be(2);
        (await ApprovalsAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task DeployRule_WithDeploymentApprovalRequired_ProposesInsteadOfDeploying()
    {
        // Arrange
        await ArrangeAsync();
        await SetPolicyAsync(requireDeploymentApproval: true);
        var rule = await CreateRuleWithSecondVersionAsync();

        // Act
        var response = await DeployAsync(rule.Rule.RuleId);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var deployment = await response.Content.ReadDataFromJsonAsync<RuleDeploymentResourceDto>();
        deployment!.RuleDeployment.Status.Should().Be(nameof(RuleDeploymentStatus.PendingApproval));
        deployment.RuleDeployment.CurrentPercentage.Should().Be(0);

        (await CurrentVersionNumberAsync(rule.Rule.RuleId)).Should().Be(1);

        var approvals = await ApprovalsAsync();
        approvals.Should().ContainSingle();
        approvals[0].Approval.Subject.Should().Be(nameof(ApprovalSubject.RuleDeployment));
        approvals[0].Approval.Status.Should().Be(nameof(ApprovalStatus.Pending));
        approvals[0].Approval.RuleDeploymentId.Should().Be(deployment.RuleDeployment.RuleDeploymentId);
        approvals[0].Approval.RequestedByUserId.Should().Be(_author.Id);
    }

    [Fact]
    public async Task ApproveDeployment_ByTheAuthor_IsForbiddenEvenThoughTheyAreAManager()
    {
        // Arrange
        await ArrangeAsync();
        await SetPolicyAsync(requireDeploymentApproval: true);
        var rule = await CreateRuleWithSecondVersionAsync();
        await DeployAsync(rule.Rule.RuleId);

        var approvalId = (await ApprovalsAsync())[0].Approval.ApprovalId;

        // Act
        var response = await _authorClient.PostAsJsonAsync($"/approvals/{approvalId}/approve", new ReviewApprovalRequestBodyDto());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        (await CurrentVersionNumberAsync(rule.Rule.RuleId)).Should().Be(1);
        (await ApprovalsAsync())[0].Approval.Status.Should().Be(nameof(ApprovalStatus.Pending));
    }

    [Fact]
    public async Task ApproveDeployment_BySomebodyElse_RunsTheDeploymentTheProposalDescribed()
    {
        // Arrange
        await ArrangeAsync();
        await SetPolicyAsync(requireDeploymentApproval: true);
        var rule = await CreateRuleWithSecondVersionAsync();
        await DeployAsync(rule.Rule.RuleId);

        var approvalId = (await ApprovalsAsync())[0].Approval.ApprovalId;

        // Act
        var response = await _reviewerClient.PostAsJsonAsync($"/approvals/{approvalId}/approve", new ReviewApprovalRequestBodyDto());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var approval = await response.Content.ReadDataFromJsonAsync<ApprovalResourceDto>();
        approval!.Approval.Status.Should().Be(nameof(ApprovalStatus.Approved));
        approval.Approval.ReviewedByUserId.Should().Be(_reviewer.Id);
        approval.Approval.ReviewedAt.Should().NotBeNull();

        (await CurrentVersionNumberAsync(rule.Rule.RuleId)).Should().Be(2);
        (await DeploymentStatusAsync(rule.Rule.RuleId)).Should().Be((int)RuleDeploymentStatus.Completed);
    }

    [Fact]
    public async Task ApproveDeployment_ForAGradualProposal_StartsTheRolloutAtItsFirstStep()
    {
        // Arrange
        await ArrangeAsync();
        await SetPolicyAsync(requireDeploymentApproval: true);
        var rule = await CreateRuleWithSecondVersionAsync();
        await DeployAsync(rule.Rule.RuleId, gradual: true);

        var approvalId = (await ApprovalsAsync())[0].Approval.ApprovalId;

        // Act
        await _reviewerClient.PostAsJsonAsync($"/approvals/{approvalId}/approve", new ReviewApprovalRequestBodyDto());

        // Assert
        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var deployment = await context.RuleDeployments
            .OrderByDescending(x => x.CreatedAt)
            .FirstAsync(x => x.RuleId == rule.Rule.RuleId);

        deployment.StatusId.Should().Be((int)RuleDeploymentStatus.InProgress);
        deployment.CurrentPercentage.Should().Be(20);

        (await CurrentVersionNumberAsync(rule.Rule.RuleId)).Should().Be(1);

        var ruleEntity = await context.Rules.FirstAsync(x => x.RuleId == rule.Rule.RuleId);
        ruleEntity.HasActiveDeployment.Should().BeTrue();
    }

    [Fact]
    public async Task RejectDeployment_LeavesTheLiveRuleAloneAndCancelsTheProposal()
    {
        // Arrange
        await ArrangeAsync();
        await SetPolicyAsync(requireDeploymentApproval: true);
        var rule = await CreateRuleWithSecondVersionAsync();
        await DeployAsync(rule.Rule.RuleId);

        var approvalId = (await ApprovalsAsync())[0].Approval.ApprovalId;

        // Act
        var response = await _reviewerClient.PostAsJsonAsync(
            $"/approvals/{approvalId}/reject", new ReviewApprovalRequestBodyDto { Note = "Threshold is an order of magnitude out" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        (await CurrentVersionNumberAsync(rule.Rule.RuleId)).Should().Be(1);
        (await DeploymentStatusAsync(rule.Rule.RuleId)).Should().Be((int)RuleDeploymentStatus.Cancelled);

        var approval = (await ApprovalsAsync())[0];
        approval.Approval.Status.Should().Be(nameof(ApprovalStatus.Rejected));
        approval.Approval.ReviewNote.Should().Be("Threshold is an order of magnitude out");
    }

    [Fact]
    public async Task RejectDeployment_WithoutANote_IsRefused()
    {
        // Arrange
        await ArrangeAsync();
        await SetPolicyAsync(requireDeploymentApproval: true);
        var rule = await CreateRuleWithSecondVersionAsync();
        await DeployAsync(rule.Rule.RuleId);

        var approvalId = (await ApprovalsAsync())[0].Approval.ApprovalId;

        // Act
        var response = await _reviewerClient.PostAsJsonAsync($"/approvals/{approvalId}/reject", new ReviewApprovalRequestBodyDto());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ApprovalsAsync())[0].Approval.Status.Should().Be(nameof(ApprovalStatus.Pending));
    }

    [Fact]
    public async Task DeployRule_WhileAProposalIsStillWaiting_IsRefused()
    {
        // Arrange
        await ArrangeAsync();
        await SetPolicyAsync(requireDeploymentApproval: true);
        var rule = await CreateRuleWithSecondVersionAsync();
        await DeployAsync(rule.Rule.RuleId);

        // Act
        var response = await DeployAsync(rule.Rule.RuleId);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ApprovalsAsync()).Should().ContainSingle();
    }

    [Fact]
    public async Task WithdrawDeployment_ByTheAuthor_TakesTheQuestionBackWithoutRejectingIt()
    {
        // Arrange
        await ArrangeAsync();
        await SetPolicyAsync(requireDeploymentApproval: true);
        var rule = await CreateRuleWithSecondVersionAsync();
        await DeployAsync(rule.Rule.RuleId);

        var approvalId = (await ApprovalsAsync())[0].Approval.ApprovalId;

        // Act
        var response = await _authorClient.PostAsJsonAsync($"/approvals/{approvalId}/withdraw", new ReviewApprovalRequestBodyDto());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ApprovalsAsync())[0].Approval.Status.Should().Be(nameof(ApprovalStatus.Withdrawn));
        (await DeploymentStatusAsync(rule.Rule.RuleId)).Should().Be((int)RuleDeploymentStatus.Cancelled);

        (await DeployAsync(rule.Rule.RuleId)).StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task WithdrawDeployment_ByAnybodyElse_IsForbidden()
    {
        // Arrange
        await ArrangeAsync();
        await SetPolicyAsync(requireDeploymentApproval: true);
        var rule = await CreateRuleWithSecondVersionAsync();
        await DeployAsync(rule.Rule.RuleId);

        var approvalId = (await ApprovalsAsync())[0].Approval.ApprovalId;

        // Act
        var response = await _reviewerClient.PostAsJsonAsync($"/approvals/{approvalId}/withdraw", new ReviewApprovalRequestBodyDto());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await ApprovalsAsync())[0].Approval.Status.Should().Be(nameof(ApprovalStatus.Pending));
    }

    [Fact]
    public async Task ApproveDeployment_Twice_IsRefusedTheSecondTime()
    {
        // Arrange
        await ArrangeAsync();
        await SetPolicyAsync(requireDeploymentApproval: true);
        var rule = await CreateRuleWithSecondVersionAsync();
        await DeployAsync(rule.Rule.RuleId);

        var approvalId = (await ApprovalsAsync())[0].Approval.ApprovalId;
        await _reviewerClient.PostAsJsonAsync($"/approvals/{approvalId}/approve", new ReviewApprovalRequestBodyDto());

        // Act
        var response = await _reviewerClient.PostAsJsonAsync($"/approvals/{approvalId}/approve", new ReviewApprovalRequestBodyDto());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ResolveCase_WithNoPolicy_ClosesTheCaseOutright()
    {
        // Arrange
        await ArrangeAsync();
        var caseId = await SeedAssignedCaseAsync(RuleSeverity.Critical);

        // Act
        var response = await ResolveAsync(caseId);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await CaseStatusAsync(caseId)).Should().Be((int)CaseStatus.Approved);
        (await ApprovalsAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task ResolveCase_AboveTheSeverityFloor_IsHeldBackAndAnswered202()
    {
        // Arrange
        await ArrangeAsync();
        await SetPolicyAsync(caseDecisionMinimumSeverity: RuleSeverity.High);
        var caseId = await SeedAssignedCaseAsync(RuleSeverity.Critical);

        // Act
        var response = await ResolveAsync(caseId);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var approval = await response.Content.ReadDataFromJsonAsync<ApprovalResourceDto>();
        approval!.Approval.Subject.Should().Be(nameof(ApprovalSubject.CaseResolution));
        approval.Approval.CaseId.Should().Be(caseId);
        approval.Approval.ProposedCaseResolution!.Status.Should().Be(nameof(CaseStatus.Approved));
        approval.Approval.ProposedCaseResolution.Disposition.Should().Be(nameof(CaseDisposition.FalsePositive));

        (await CaseStatusAsync(caseId)).Should().Be((int)CaseStatus.Assigned);
    }

    [Fact]
    public async Task ResolveCase_BelowTheSeverityFloor_ClosesWithoutAsking()
    {
        // Arrange
        await ArrangeAsync();
        await SetPolicyAsync(caseDecisionMinimumSeverity: RuleSeverity.Critical);
        var caseId = await SeedAssignedCaseAsync(RuleSeverity.Low);

        // Act
        var response = await ResolveAsync(caseId);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await CaseStatusAsync(caseId)).Should().Be((int)CaseStatus.Approved);
    }

    [Fact]
    public async Task ResolveCase_OverTheAmountInTheMatcher_IsHeldBackWhateverTheSeverity()
    {
        // Arrange
        await ArrangeAsync();
        await SetPolicyAsync(caseDecisionMatcher: new RuleBodyDto
        {
            Field = "amount",
            Condition = nameof(RuleCondition.GreaterThan),
            Value = 10000
        });

        var large = await SeedAssignedCaseAsync(RuleSeverity.Info, amount: 250000);
        var small = await SeedAssignedCaseAsync(RuleSeverity.Info, amount: 20);

        // Act
        var largeResponse = await ResolveAsync(large);
        var smallResponse = await ResolveAsync(small);

        // Assert
        largeResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);
        smallResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ApproveCaseResolution_ByTheAnalystWhoProposedIt_IsForbidden()
    {
        // Arrange
        await ArrangeAsync();
        await SetPolicyAsync(caseDecisionMinimumSeverity: RuleSeverity.High);
        var caseId = await SeedAssignedCaseAsync(RuleSeverity.Critical);
        await ResolveAsync(caseId);

        var approvalId = (await ApprovalsAsync())[0].Approval.ApprovalId;

        // Act
        var response = await _authorClient.PostAsJsonAsync($"/approvals/{approvalId}/approve", new ReviewApprovalRequestBodyDto());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await CaseStatusAsync(caseId)).Should().Be((int)CaseStatus.Assigned);
    }

    [Fact]
    public async Task ApproveCaseResolution_ClosesTheCaseInTheNameOfTheAnalystWhoProposedIt()
    {
        // Arrange
        await ArrangeAsync();
        await SetPolicyAsync(caseDecisionMinimumSeverity: RuleSeverity.High);
        var caseId = await SeedAssignedCaseAsync(RuleSeverity.Critical);
        await ResolveAsync(caseId);

        var approvalId = (await ApprovalsAsync())[0].Approval.ApprovalId;

        // Act
        var response = await _reviewerClient.PostAsJsonAsync($"/approvals/{approvalId}/approve", new ReviewApprovalRequestBodyDto());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var caseEntity = await context.Cases.FirstAsync(x => x.CaseId == caseId);
        caseEntity.StatusId.Should().Be((int)CaseStatus.Approved);
        caseEntity.DispositionId.Should().Be((int)CaseDisposition.FalsePositive);
        caseEntity.ResolvedAt.Should().NotBeNull();

        var decisions = await context.Decisions.Where(x => x.CaseId == caseId).ToListAsync();
        decisions.Should().OnlyContain(x => x.ResolvedByUserId == _author.Id);
    }

    [Fact]
    public async Task RejectCaseResolution_LeavesTheCaseOpenAndPutsTheReasonOnIt()
    {
        // Arrange
        await ArrangeAsync();
        await SetPolicyAsync(caseDecisionMinimumSeverity: RuleSeverity.High);
        var caseId = await SeedAssignedCaseAsync(RuleSeverity.Critical);
        await ResolveAsync(caseId);

        var approvalId = (await ApprovalsAsync())[0].Approval.ApprovalId;

        // Act
        var response = await _reviewerClient.PostAsJsonAsync(
            $"/approvals/{approvalId}/reject", new ReviewApprovalRequestBodyDto { Note = "Check the counterparty before closing" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await CaseStatusAsync(caseId)).Should().Be((int)CaseStatus.Assigned);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var notes = await context.CaseNotes.Where(x => x.CaseId == caseId).ToListAsync();
        notes.Should().ContainSingle().Which.Message.Should().Be("Check the counterparty before closing");
    }

    [Fact]
    public async Task ResolveCase_TwiceWhileTheFirstIsStillWaiting_IsRefused()
    {
        // Arrange
        await ArrangeAsync();
        await SetPolicyAsync(caseDecisionMinimumSeverity: RuleSeverity.High);
        var caseId = await SeedAssignedCaseAsync(RuleSeverity.Critical);
        await ResolveAsync(caseId);

        // Act
        var response = await ResolveAsync(caseId);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ApprovalsAsync()).Should().ContainSingle();
    }

    [Fact]
    public async Task ListApprovals_FiltersOnStatusAndSubject()
    {
        // Arrange
        await ArrangeAsync();
        await SetPolicyAsync(requireDeploymentApproval: true, caseDecisionMinimumSeverity: RuleSeverity.High);

        var rule = await CreateRuleWithSecondVersionAsync();
        await DeployAsync(rule.Rule.RuleId);
        await ResolveAsync(await SeedAssignedCaseAsync(RuleSeverity.Critical));

        // Act
        var deployments = await _reviewerClient.GetFromJsonAsync<PagedResponseDto<ApprovalResourceDto>>(
            $"/approvals?subject={nameof(ApprovalSubject.RuleDeployment)}");
        var pending = await _reviewerClient.GetFromJsonAsync<PagedResponseDto<ApprovalResourceDto>>(
            $"/approvals?status={nameof(ApprovalStatus.Pending)}");

        // Assert
        deployments!.Data.Should().ContainSingle();
        pending!.Data.Should().HaveCount(2);
    }

    [Fact]
    public async Task ListMyApprovals_ReturnsOnlyWhatTheCallerRaised()
    {
        // Arrange
        await ArrangeAsync();
        await SetPolicyAsync(requireDeploymentApproval: true);
        var rule = await CreateRuleWithSecondVersionAsync();
        await DeployAsync(rule.Rule.RuleId);

        // Act
        var mine = await _authorClient.GetFromJsonAsync<PagedResponseDto<ApprovalResourceDto>>("/approvals/mine");
        var theirs = await _reviewerClient.GetFromJsonAsync<PagedResponseDto<ApprovalResourceDto>>("/approvals/mine");

        // Assert
        mine!.Data.Should().ContainSingle();
        theirs!.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task GetApprovalHistory_RecordsTheReviewThatSettledIt()
    {
        // Arrange
        await ArrangeAsync();
        await SetPolicyAsync(requireDeploymentApproval: true);
        var rule = await CreateRuleWithSecondVersionAsync();
        await DeployAsync(rule.Rule.RuleId);

        var approvalId = (await ApprovalsAsync())[0].Approval.ApprovalId;
        await _reviewerClient.PostAsJsonAsync($"/approvals/{approvalId}/approve", new ReviewApprovalRequestBodyDto());

        // Act
        var history = await _reviewerClient.GetFromJsonAsync<PagedResponseDto<AuditLogResourceDto>>($"/approvals/{approvalId}/history");

        // Assert
        history!.Data.Should().NotBeEmpty();
        history.Data.Should().Contain(x => x.AuditLog.Changes.Any(change =>
            change.Field == "Status" &&
            change.NewValue!.GetValue<string>() == nameof(ApprovalStatus.Approved)));
    }

    [Fact]
    public async Task GetApproval_ForAnIdentifierTheOrganizationDoesNotHold_IsNotFound()
    {
        // Arrange
        await ArrangeAsync();

        // Act
        var response = await _reviewerClient.GetAsync($"/approvals/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetApprovalPolicy_BeforeOneIsStated_IsNotFound()
    {
        // Arrange
        await ArrangeAsync();

        // Act
        var response = await _reviewerClient.GetAsync($"/organizations/{OrganizationId}/approval-policy");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SetApprovalPolicy_ThenGet_ReturnsWhatWasStated()
    {
        // Arrange
        await ArrangeAsync();

        // Act
        await SetPolicyAsync(requireDeploymentApproval: true, caseDecisionMinimumSeverity: RuleSeverity.High);

        // Assert
        var response = await _reviewerClient.GetFromJsonAsync<ResponseDataDto<ApprovalPolicyResponseDto>>(
            $"/organizations/{OrganizationId}/approval-policy");

        response!.Data.RequireDeploymentApproval.Should().BeTrue();
        response.Data.CaseDecisionMinimumSeverity.Should().Be(nameof(RuleSeverity.High));
    }

    [Fact]
    public async Task SetApprovalPolicy_WithAConditionOmitted_DropsIt()
    {
        // Arrange
        await ArrangeAsync();
        await SetPolicyAsync(requireDeploymentApproval: true, caseDecisionMinimumSeverity: RuleSeverity.High);

        // Act
        await SetPolicyAsync(requireDeploymentApproval: true);

        // Assert
        var response = await _reviewerClient.GetFromJsonAsync<ResponseDataDto<ApprovalPolicyResponseDto>>(
            $"/organizations/{OrganizationId}/approval-policy");

        response!.Data.CaseDecisionMinimumSeverity.Should().BeNull();

        var caseId = await SeedAssignedCaseAsync(RuleSeverity.Critical);
        (await ResolveAsync(caseId)).StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task SetApprovalPolicy_WithAVelocityMatcher_IsRefused()
    {
        // Arrange
        await ArrangeAsync();

        // Act
        var response = await _reviewerClient.PutAsJsonAsync(
            $"/organizations/{OrganizationId}/approval-policy",
            new SetOrganizationApprovalPolicyRequestBodyDto
            {
                RequireDeploymentApproval = false,
                CaseDecisionMatcher = new RuleBodyDto
                {
                    Condition = nameof(RuleCondition.GreaterThan),
                    Velocity = new VelocityConditionDto
                    {
                        Role = nameof(PartyRole.Remitter),
                        Metric = "payment_amount",
                        Function = nameof(VelocityFunction.Sum),
                        Window = new VelocityWindowDto
                        {
                            Kind = nameof(VelocityWindowKind.Rolling),
                            Unit = nameof(VelocityWindowUnit.Hour),
                            Length = 24
                        },
                        IncludeCurrent = false
                    },
                    Value = 10000
                }
            });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SetApprovalPolicy_TurningTheGateOff_LeavesRequestsAlreadyPendingAlone()
    {
        // Arrange
        await ArrangeAsync();
        await SetPolicyAsync(caseDecisionMinimumSeverity: RuleSeverity.High);
        var caseId = await SeedAssignedCaseAsync(RuleSeverity.Critical);
        await ResolveAsync(caseId);

        // Act
        await SetPolicyAsync();

        // Assert
        (await ApprovalsAsync())[0].Approval.Status.Should().Be(nameof(ApprovalStatus.Pending));
        (await CaseStatusAsync(caseId)).Should().Be((int)CaseStatus.Assigned);
    }

    private Task<HttpResponseMessage> DeployAsync(Guid ruleId, bool gradual = false)
    {
        return gradual
            ? _authorClient.PostAsJsonAsync(
                $"/rules/{ruleId}/deployments",
                new { versionNumber = 2, strategy = nameof(DeploymentStrategy.Gradual), stepPercentage = 20, stepIntervalSeconds = 3600 })
            : _authorClient.PostAsJsonAsync(
                $"/rules/{ruleId}/deployments",
                new { versionNumber = 2, strategy = nameof(DeploymentStrategy.AllAtOnce) });
    }

    private Task<HttpResponseMessage> ResolveAsync(Guid caseId)
    {
        return _authorClient.PostAsJsonAsync($"/cases/{caseId}/resolve", new ResolveCaseRequestBodyDto
        {
            Status = nameof(CaseStatus.Approved),
            Disposition = nameof(CaseDisposition.FalsePositive)
        });
    }

    private async Task SetPolicyAsync(
        bool requireDeploymentApproval = false,
        RuleSeverity? caseDecisionMinimumSeverity = null,
        RuleBodyDto? caseDecisionMatcher = null)
    {
        var response = await _reviewerClient.PutAsJsonAsync(
            $"/organizations/{OrganizationId}/approval-policy",
            new SetOrganizationApprovalPolicyRequestBodyDto
            {
                RequireDeploymentApproval = requireDeploymentApproval,
                CaseDecisionMinimumSeverity = caseDecisionMinimumSeverity?.ToString(),
                CaseDecisionMatcher = caseDecisionMatcher
            });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    private async Task<ApprovalResourceDto[]> ApprovalsAsync()
    {
        var response = await _reviewerClient.GetFromJsonAsync<PagedResponseDto<ApprovalResourceDto>>("/approvals?pageSize=50");

        return response!.Data;
    }

    private async Task<int> CurrentVersionNumberAsync(Guid ruleId)
    {
        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var ruleEntity = await context.Rules.FirstAsync(x => x.RuleId == ruleId);

        return ruleEntity.CurrentVersionNumber;
    }

    private async Task<int> DeploymentStatusAsync(Guid ruleId)
    {
        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var deployment = await context.RuleDeployments
            .OrderByDescending(x => x.CreatedAt)
            .FirstAsync(x => x.RuleId == ruleId);

        return deployment.StatusId;
    }

    private async Task<int> CaseStatusAsync(Guid caseId)
    {
        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var caseEntity = await context.Cases.FirstAsync(x => x.CaseId == caseId);

        return caseEntity.StatusId;
    }

    private async Task<Guid> SeedAssignedCaseAsync(RuleSeverity severity, int amount = 100)
    {
        var now = DateTimeOffset.UtcNow;

        var transactionEntity = FakeTransactionEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            Payload = Payload(amount),
            IsDeleted = false
        };

        await DatabaseHelper.CreateTransactions(testFixture.Services, [transactionEntity]);

        var caseEntity = FakeCaseEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            TransactionId = transactionEntity.TransactionId,
            Transaction = null!,
            StatusId = (int)CaseStatus.Assigned,
            SeverityId = (int)severity,
            AssignedToUserId = _author.Id,
            AssignedToUser = null!,
            AssignedAt = now,
            DispositionId = null,
            CaseReasonCodeId = null,
            QueueId = null,
            CreatedAt = now,
            UpdatedAt = now,
            DueAt = now.AddHours(4),
            ResolvedAt = null,
            IsDeleted = false
        };

        await DatabaseHelper.CreateCases(testFixture.Services, [caseEntity]);

        var decisionEntity = FakeDecisionEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            TransactionId = transactionEntity.TransactionId,
            Transaction = null!,
            CaseId = caseEntity.CaseId,
            Case = null!,
            StatusId = (int)DecisionStatus.Held,
            ResolvedByUserId = null,
            ResolvedAt = null,
            HeldAt = now
        };

        await DatabaseHelper.CreateDecisions(testFixture.Services, [decisionEntity]);

        return caseEntity.CaseId;
    }

    private static string Payload(int amount)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes($$"""{"amount":{{amount}}}"""));
    }

    private async Task<RuleResourceDto> CreateRuleWithSecondVersionAsync()
    {
        var ruleSetEntity = FakeRuleSetEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            IsEnabled = true,
            IsDeleted = false,
            Rules = []
        };

        await DatabaseHelper.CreateRuleSets(testFixture.Services, [ruleSetEntity]);

        var createResponse = await _authorClient.PostAsJsonAsync($"/rule-sets/{ruleSetEntity.RuleSetId}/rules", new CreateRuleRequestBodyDto
        {
            FriendlyName = "Large payment",
            IsEnabled = true,
            Action = nameof(RuleAction.Review),
            Severity = nameof(RuleSeverity.High),
            RuleBody = new RuleBodyDto
            {
                Field = "amount",
                Condition = nameof(RuleCondition.GreaterThan),
                Value = 10000
            }
        });

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdRule = await createResponse.Content.ReadDataFromJsonAsync<RuleResourceDto>();

        var versionResponse = await _authorClient.PostAsJsonAsync($"/rules/{createdRule!.Rule.RuleId}/versions", new CreateRuleVersionRequestBodyDto
        {
            Action = nameof(RuleAction.Block),
            Severity = nameof(RuleSeverity.Critical),
            RuleBody = new RuleBodyDto
            {
                Field = "amount",
                Condition = nameof(RuleCondition.GreaterThan),
                Value = 5000
            }
        });

        versionResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        return createdRule;
    }

    private async Task ArrangeAsync()
    {
        _author = await AuthenticationHelper.CreateUserAsync(
            testFixture.Services, AuthorEmail, Password, [UserRole.User, UserRole.Manager, UserRole.Admin], OrganizationId);

        _reviewer = await AuthenticationHelper.CreateUserAsync(
            testFixture.Services, ReviewerEmail, Password, [UserRole.User, UserRole.Manager, UserRole.Admin], OrganizationId);

        await AuthenticationHelper.SetTokenAsync(_authorClient, AuthorEmail, Password);
        await AuthenticationHelper.SetTokenAsync(_reviewerClient, ReviewerEmail, Password);
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
