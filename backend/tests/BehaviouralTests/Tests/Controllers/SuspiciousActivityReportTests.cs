using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
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
using Presentation.Models.Common;
using Presentation.Models.SuspiciousActivityReports;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

namespace BehaviouralTests.Tests.Controllers;

public class SuspiciousActivityReportTests(TestFixture testFixture) : IClassFixture<TestFixture>, IAsyncLifetime
{
    private readonly Fixture _fixture = AutoFixtureHelper.Create();
    private readonly HttpClient _analystClient = testFixture.CreateClient();
    private readonly HttpClient _officerClient = testFixture.CreateClient();

    private const string AnalystEmail = "analyst@mail.com";
    private const string OfficerEmail = "officer@mail.com";
    private const string Password = "P@$$w0rd";
    private static readonly Guid OrganizationId = new("8b1d4c72-3ea6-4f19-b5d0-92c7a3e18f64");

    private ApplicationUserEntity _analyst = null!;
    private ApplicationUserEntity _officer = null!;

    [Fact]
    public async Task OpenReport_ComposesADraftNarrativeFromTheCase()
    {
        // Arrange
        await ArrangeAsync();
        var caseId = await SeedCaseAsync();

        // Act
        var response = await _analystClient.PostAsJsonAsync(
            $"/cases/{caseId}/suspicious-activity-report", new OpenSuspiciousActivityReportRequestBodyDto());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var report = await response.Content.ReadDataFromJsonAsync<SuspiciousActivityReportResourceDto>();

        report!.SuspiciousActivityReport.Status.Should().Be(nameof(SuspiciousActivityReportStatus.Draft));
        report.SuspiciousActivityReport.CaseId.Should().Be(caseId);
        report.SuspiciousActivityReport.Narrative.Should().Contain("SUMMARY");
        report.SuspiciousActivityReport.Narrative.Should().Contain("requires review before filing");
        report.SuspiciousActivityReport.OpenedByUserId.Should().Be(_analyst.Id);
    }

    [Fact]
    public async Task OpenReport_WithASuppliedNarrative_UsesItRatherThanTheComposedDraft()
    {
        // Arrange
        await ArrangeAsync();
        var caseId = await SeedCaseAsync();

        // Act
        var response = await _analystClient.PostAsJsonAsync(
            $"/cases/{caseId}/suspicious-activity-report",
            new OpenSuspiciousActivityReportRequestBodyDto { Narrative = "Written by hand." });

        // Assert
        var report = await response.Content.ReadDataFromJsonAsync<SuspiciousActivityReportResourceDto>();

        report!.SuspiciousActivityReport.Narrative.Should().Be("Written by hand.");
    }

    [Fact]
    public async Task OpenReport_MeasuresTheDeadlineFromWhenTheCaseWasOpened()
    {
        // Arrange
        await ArrangeAsync();
        var caseOpenedAt = DateTimeOffset.UtcNow.AddDays(-21);
        var caseId = await SeedCaseAsync(createdAt: caseOpenedAt);

        // Act
        var response = await _analystClient.PostAsJsonAsync(
            $"/cases/{caseId}/suspicious-activity-report", new OpenSuspiciousActivityReportRequestBodyDto());

        // Assert
        var report = await response.Content.ReadDataFromJsonAsync<SuspiciousActivityReportResourceDto>();

        report!.SuspiciousActivityReport.DueAt.Should().BeCloseTo(caseOpenedAt.AddDays(30), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task OpenReport_WithADeclaredDeadline_UsesTheOrganizationsFigure()
    {
        // Arrange
        await ArrangeAsync();
        await SetPolicyAsync(filingDeadlineDays: 5);
        var caseOpenedAt = DateTimeOffset.UtcNow;
        var caseId = await SeedCaseAsync(createdAt: caseOpenedAt);

        // Act
        var response = await _analystClient.PostAsJsonAsync(
            $"/cases/{caseId}/suspicious-activity-report", new OpenSuspiciousActivityReportRequestBodyDto());

        // Assert
        var report = await response.Content.ReadDataFromJsonAsync<SuspiciousActivityReportResourceDto>();

        report!.SuspiciousActivityReport.DueAt.Should().BeCloseTo(caseOpenedAt.AddDays(5), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task OpenReport_Twice_IsRefused()
    {
        // Arrange
        await ArrangeAsync();
        var caseId = await SeedCaseAsync();
        await OpenReportAsync(caseId);

        // Act
        var response = await _analystClient.PostAsJsonAsync(
            $"/cases/{caseId}/suspicious-activity-report", new OpenSuspiciousActivityReportRequestBodyDto());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task OpenReport_Twice_NamesTheReportThatAlreadyExists()
    {
        // Arrange
        await ArrangeAsync();
        var caseId = await SeedCaseAsync();
        var existing = await OpenReportAsync(caseId);

        // Act
        var response = await _analystClient.PostAsJsonAsync(
            $"/cases/{caseId}/suspicious-activity-report", new OpenSuspiciousActivityReportRequestBodyDto());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        problem.RootElement.GetProperty("suspiciousActivityReportId").GetGuid()
            .Should().Be(existing.SuspiciousActivityReportId);
        problem.RootElement.GetProperty("detail").GetString().Should().Contain("already has a report");
    }

    [Fact]
    public async Task OpenReport_OnACaseAlreadyRecordedAsNotFiled_NamesThatReport()
    {
        // Arrange
        await ArrangeAsync();
        var caseId = await SeedCaseAsync();
        var existing = await OpenReportAsync(caseId);

        await _officerClient.PostAsJsonAsync(
            $"/suspicious-activity-reports/{existing.SuspiciousActivityReportId}/decline",
            new DeclineSuspiciousActivityReportRequestBodyDto { Reason = "Invoice produced." });

        // Act
        var response = await _analystClient.PostAsJsonAsync(
            $"/cases/{caseId}/suspicious-activity-report", new OpenSuspiciousActivityReportRequestBodyDto());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        problem.RootElement.GetProperty("detail").GetString()
            .Should().Be("This case was already considered and recorded as not filed");
        problem.RootElement.GetProperty("suspiciousActivityReportId").GetGuid()
            .Should().Be(existing.SuspiciousActivityReportId);
    }

    [Fact]
    public async Task UpdateNarrative_AfterSubmission_RefusesWithoutNamingTheReport()
    {
        // Arrange
        await ArrangeAsync();
        var report = await OpenReportAsync(await SeedCaseAsync());
        await SubmitAsync(report.SuspiciousActivityReportId);

        // Act
        var response = await _analystClient.PatchAsJsonAsync(
            $"/suspicious-activity-reports/{report.SuspiciousActivityReportId}",
            new UpdateSuspiciousActivityReportRequestBodyDto { Narrative = "Too late." });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        problem.RootElement.TryGetProperty("suspiciousActivityReportId", out _).Should().BeFalse();
    }

    [Fact]
    public async Task UpdateNarrative_WhileDraft_ReplacesTheDocument()
    {
        // Arrange
        await ArrangeAsync();
        var report = await OpenReportAsync(await SeedCaseAsync());

        // Act
        var response = await _analystClient.PatchAsJsonAsync(
            $"/suspicious-activity-reports/{report.SuspiciousActivityReportId}",
            new UpdateSuspiciousActivityReportRequestBodyDto { Narrative = "Rewritten by the officer." });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await response.Content.ReadDataFromJsonAsync<SuspiciousActivityReportResourceDto>();
        updated!.SuspiciousActivityReport.Narrative.Should().Be("Rewritten by the officer.");
    }

    [Fact]
    public async Task UpdateNarrative_AfterItLeavesDraft_IsRefused()
    {
        // Arrange
        await ArrangeAsync();
        var report = await OpenReportAsync(await SeedCaseAsync());
        await SubmitAsync(report.SuspiciousActivityReportId);

        // Act
        var response = await _analystClient.PatchAsJsonAsync(
            $"/suspicious-activity-reports/{report.SuspiciousActivityReportId}",
            new UpdateSuspiciousActivityReportRequestBodyDto { Narrative = "Too late." });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task RecomposeNarrative_WhileDraft_RedrawsItFromTheCaseAndDiscardsTheEdit()
    {
        // Arrange
        await ArrangeAsync();
        var report = await OpenReportAsync(await SeedCaseAsync());

        await _analystClient.PatchAsJsonAsync(
            $"/suspicious-activity-reports/{report.SuspiciousActivityReportId}",
            new UpdateSuspiciousActivityReportRequestBodyDto { Narrative = "Scratch this." });

        // Act
        var response = await _analystClient.PostAsJsonAsync(
            $"/suspicious-activity-reports/{report.SuspiciousActivityReportId}/narrative", new { });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var recomposed = await response.Content.ReadDataFromJsonAsync<SuspiciousActivityReportResourceDto>();
        recomposed!.SuspiciousActivityReport.Narrative.Should().NotBe("Scratch this.");
        recomposed.SuspiciousActivityReport.Narrative.Should().Contain("SUMMARY");
    }

    [Fact]
    public async Task Submit_WithNoApprovalRequired_ApprovesOutrightAndAsksNobody()
    {
        // Arrange
        await ArrangeAsync();
        var report = await OpenReportAsync(await SeedCaseAsync());

        // Act
        var response = await _analystClient.PostAsJsonAsync(
            $"/suspicious-activity-reports/{report.SuspiciousActivityReportId}/submit", new { });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var submitted = await response.Content.ReadDataFromJsonAsync<SuspiciousActivityReportResourceDto>();
        submitted!.SuspiciousActivityReport.Status.Should().Be(nameof(SuspiciousActivityReportStatus.Approved));

        (await ApprovalsAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task Submit_WithApprovalRequired_RaisesAnApprovalOnTheSharedInbox()
    {
        // Arrange
        await ArrangeAsync();
        await SetPolicyAsync(requireFilingApproval: true);
        var report = await OpenReportAsync(await SeedCaseAsync());

        // Act
        var response = await _analystClient.PostAsJsonAsync(
            $"/suspicious-activity-reports/{report.SuspiciousActivityReportId}/submit", new { });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var approvals = await ApprovalsAsync();
        approvals.Should().ContainSingle();
        approvals[0].Approval.Subject.Should().Be(nameof(ApprovalSubject.SuspiciousActivityReport));
        approvals[0].Approval.Status.Should().Be(nameof(ApprovalStatus.Pending));
        approvals[0].Approval.SuspiciousActivityReportId.Should().Be(report.SuspiciousActivityReportId);
        approvals[0].Approval.RequestedByUserId.Should().Be(_analyst.Id);

        (await StatusAsync(report.SuspiciousActivityReportId))
            .Should().Be((int)SuspiciousActivityReportStatus.PendingApproval);
    }

    [Fact]
    public async Task Submit_WithApprovalRequired_CarriesTheReportOnTheApproval()
    {
        // Arrange
        await ArrangeAsync();
        await SetPolicyAsync(requireFilingApproval: true);
        var caseId = await SeedCaseAsync();
        var report = await OpenReportAsync(caseId);

        await _analystClient.PatchAsJsonAsync(
            $"/suspicious-activity-reports/{report.SuspiciousActivityReportId}",
            new UpdateSuspiciousActivityReportRequestBodyDto { Narrative = "The narrative being signed off." });

        // Act
        var response = await _analystClient.PostAsJsonAsync(
            $"/suspicious-activity-reports/{report.SuspiciousActivityReportId}/submit", new { });

        // Assert
        var accepted = await response.Content.ReadDataFromJsonAsync<ApprovalResourceDto>();
        accepted!.SuspiciousActivityReport.Should().NotBeNull();
        accepted.SuspiciousActivityReport!.Narrative.Should().Be("The narrative being signed off.");
        accepted.SuspiciousActivityReport.SuspiciousActivityReportId.Should().Be(report.SuspiciousActivityReportId);

        accepted.SuspiciousActivityReport.Status.Should().Be(nameof(SuspiciousActivityReportStatus.PendingApproval));

        var approvals = await ApprovalsAsync();
        approvals[0].SuspiciousActivityReport.Should().NotBeNull();
        approvals[0].SuspiciousActivityReport!.Narrative.Should().Be("The narrative being signed off.");
    }

    [Fact]
    public async Task ListApprovals_ForAFilingRequest_CarriesNoDeployment()
    {
        // Arrange
        await ArrangeAsync();
        await SetPolicyAsync(requireFilingApproval: true);
        var report = await OpenReportAsync(await SeedCaseAsync());
        await SubmitAsync(report.SuspiciousActivityReportId);

        // Act
        var approvals = await ApprovalsAsync();

        // Assert
        approvals.Should().ContainSingle();
        approvals[0].RuleDeployment.Should().BeNull();
        approvals[0].SuspiciousActivityReport.Should().NotBeNull();
    }

    [Fact]
    public async Task ApproveSubmission_BySomebodyElse_MovesTheReportToApproved()
    {
        // Arrange
        await ArrangeAsync();
        await SetPolicyAsync(requireFilingApproval: true);
        var report = await OpenReportAsync(await SeedCaseAsync());
        await SubmitAsync(report.SuspiciousActivityReportId);

        var approvalId = (await ApprovalsAsync())[0].Approval.ApprovalId;

        // Act
        var response = await _officerClient.PostAsJsonAsync($"/approvals/{approvalId}/approve", new ReviewApprovalRequestBodyDto());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        (await StatusAsync(report.SuspiciousActivityReportId))
            .Should().Be((int)SuspiciousActivityReportStatus.Approved);
    }

    [Fact]
    public async Task ApproveSubmission_ByTheOfficerWhoRaisedIt_IsForbidden()
    {
        // Arrange
        await ArrangeAsync();
        await SetPolicyAsync(requireFilingApproval: true);
        var report = await OpenReportAsync(await SeedCaseAsync());
        await SubmitAsync(report.SuspiciousActivityReportId);

        var approvalId = (await ApprovalsAsync())[0].Approval.ApprovalId;

        // Act
        var response = await _analystClient.PostAsJsonAsync($"/approvals/{approvalId}/approve", new ReviewApprovalRequestBodyDto());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        (await StatusAsync(report.SuspiciousActivityReportId))
            .Should().Be((int)SuspiciousActivityReportStatus.PendingApproval);
    }

    [Fact]
    public async Task RejectSubmission_SendsTheReportBackToDraftRatherThanEndingIt()
    {
        // Arrange
        await ArrangeAsync();
        await SetPolicyAsync(requireFilingApproval: true);
        var report = await OpenReportAsync(await SeedCaseAsync());
        await SubmitAsync(report.SuspiciousActivityReportId);

        var approvalId = (await ApprovalsAsync())[0].Approval.ApprovalId;

        // Act
        var response = await _officerClient.PostAsJsonAsync(
            $"/approvals/{approvalId}/reject", new ReviewApprovalRequestBodyDto { Note = "The narrative omits the source of funds." });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        (await StatusAsync(report.SuspiciousActivityReportId))
            .Should().Be((int)SuspiciousActivityReportStatus.Draft);
    }

    [Fact]
    public async Task Submit_WhileAlreadyPendingApproval_IsRefused()
    {
        // Arrange
        await ArrangeAsync();
        await SetPolicyAsync(requireFilingApproval: true);
        var report = await OpenReportAsync(await SeedCaseAsync());
        await SubmitAsync(report.SuspiciousActivityReportId);

        // Act
        var response = await _analystClient.PostAsJsonAsync(
            $"/suspicious-activity-reports/{report.SuspiciousActivityReportId}/submit", new { });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task File_RecordsWhoSentItAndWhatTheUnitCallsIt()
    {
        // Arrange
        await ArrangeAsync();
        var report = await OpenReportAsync(await SeedCaseAsync());
        await SubmitAsync(report.SuspiciousActivityReportId);

        // Act
        var response = await _officerClient.PostAsJsonAsync(
            $"/suspicious-activity-reports/{report.SuspiciousActivityReportId}/file",
            new FileSuspiciousActivityReportRequestBodyDto { FilingReference = "FIU-2026-0042" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var filed = await response.Content.ReadDataFromJsonAsync<SuspiciousActivityReportResourceDto>();
        filed!.SuspiciousActivityReport.Status.Should().Be(nameof(SuspiciousActivityReportStatus.Filed));
        filed.SuspiciousActivityReport.FilingReference.Should().Be("FIU-2026-0042");
        filed.SuspiciousActivityReport.FiledByUserId.Should().Be(_officer.Id);
        filed.SuspiciousActivityReport.FiledAt.Should().NotBeNull();
    }

    [Fact]
    public async Task File_WhileStillADraft_IsRefused()
    {
        // Arrange
        await ArrangeAsync();
        var report = await OpenReportAsync(await SeedCaseAsync());

        // Act
        var response = await _officerClient.PostAsJsonAsync(
            $"/suspicious-activity-reports/{report.SuspiciousActivityReportId}/file",
            new FileSuspiciousActivityReportRequestBodyDto { FilingReference = "FIU-2026-0042" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task File_ByAnAnalyst_IsForbidden()
    {
        // Arrange
        await ArrangeAsync(analystIsManager: false);
        var report = await OpenReportAsync(await SeedCaseAsync());
        await SubmitAsync(report.SuspiciousActivityReportId);

        // Act
        var response = await _analystClient.PostAsJsonAsync(
            $"/suspicious-activity-reports/{report.SuspiciousActivityReportId}/file",
            new FileSuspiciousActivityReportRequestBodyDto { FilingReference = "FIU-2026-0042" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Acknowledge_KeepsTheFilingReferenceTheSubmissionWroteDown()
    {
        // Arrange
        await ArrangeAsync();
        var report = await OpenReportAsync(await SeedCaseAsync());
        await SubmitAsync(report.SuspiciousActivityReportId);
        await FileAsync(report.SuspiciousActivityReportId, "FIU-2026-0042");

        // Act
        var response = await _officerClient.PostAsJsonAsync(
            $"/suspicious-activity-reports/{report.SuspiciousActivityReportId}/acknowledge",
            new AcknowledgeSuspiciousActivityReportRequestBodyDto { AcknowledgementReference = "ACK-991" });

        // Assert
        var acknowledged = await response.Content.ReadDataFromJsonAsync<SuspiciousActivityReportResourceDto>();

        acknowledged!.SuspiciousActivityReport.Status.Should().Be(nameof(SuspiciousActivityReportStatus.Acknowledged));
        acknowledged.SuspiciousActivityReport.AcknowledgementReference.Should().Be("ACK-991");
        acknowledged.SuspiciousActivityReport.FilingReference.Should().Be("FIU-2026-0042");
        acknowledged.SuspiciousActivityReport.AcknowledgedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Decline_RecordsThatTheCaseWasConsideredAndNotFiled()
    {
        // Arrange
        await ArrangeAsync();
        var report = await OpenReportAsync(await SeedCaseAsync());

        // Act
        var response = await _officerClient.PostAsJsonAsync(
            $"/suspicious-activity-reports/{report.SuspiciousActivityReportId}/decline",
            new DeclineSuspiciousActivityReportRequestBodyDto { Reason = "Customer produced an invoice for the payment." });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var declined = await response.Content.ReadDataFromJsonAsync<SuspiciousActivityReportResourceDto>();
        declined!.SuspiciousActivityReport.Status.Should().Be(nameof(SuspiciousActivityReportStatus.NotFiled));
        declined.SuspiciousActivityReport.NotFiledReason.Should().Be("Customer produced an invoice for the payment.");
        declined.SuspiciousActivityReport.DecidedByUserId.Should().Be(_officer.Id);
    }

    [Fact]
    public async Task Decline_StampsWhenTheDecisionWasMade()
    {
        // Arrange
        await ArrangeAsync();
        var report = await OpenReportAsync(await SeedCaseAsync());
        report.DecidedAt.Should().BeNull();

        // Act
        var response = await _officerClient.PostAsJsonAsync(
            $"/suspicious-activity-reports/{report.SuspiciousActivityReportId}/decline",
            new DeclineSuspiciousActivityReportRequestBodyDto { Reason = "Invoice produced." });

        // Assert
        var declined = await response.Content.ReadDataFromJsonAsync<SuspiciousActivityReportResourceDto>();

        declined!.SuspiciousActivityReport.DecidedAt.Should().NotBeNull();
        declined.SuspiciousActivityReport.DecidedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task FileReport_LeavesTheDecidedStampUnset()
    {
        // Arrange
        await ArrangeAsync();
        var report = await OpenReportAsync(await SeedCaseAsync());
        await SubmitAsync(report.SuspiciousActivityReportId);

        // Act
        await FileAsync(report.SuspiciousActivityReportId, "FIU-2026-0001");

        // Assert
        var resource = await _officerClient.GetFromJsonAsync<ResponseDataDto<SuspiciousActivityReportResourceDto>>(
            $"/suspicious-activity-reports/{report.SuspiciousActivityReportId}");

        resource!.Data.SuspiciousActivityReport.DecidedAt.Should().BeNull();
        resource.Data.SuspiciousActivityReport.FiledAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Decline_WritesTheReasonOntoTheCaseAsANote()
    {
        // Arrange
        await ArrangeAsync();
        var caseId = await SeedCaseAsync();
        var report = await OpenReportAsync(caseId);

        // Act
        await _officerClient.PostAsJsonAsync(
            $"/suspicious-activity-reports/{report.SuspiciousActivityReportId}/decline",
            new DeclineSuspiciousActivityReportRequestBodyDto { Reason = "Invoice produced." });

        // Assert
        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var notes = await context.CaseNotes.Where(x => x.CaseId == caseId).ToListAsync();

        notes.Should().ContainSingle();
        notes[0].Message.Should().Be("Invoice produced.");
    }

    [Fact]
    public async Task Decline_WithNoReason_IsRefused()
    {
        // Arrange
        await ArrangeAsync();
        var report = await OpenReportAsync(await SeedCaseAsync());

        // Act
        var response = await _officerClient.PostAsJsonAsync(
            $"/suspicious-activity-reports/{report.SuspiciousActivityReportId}/decline",
            new DeclineSuspiciousActivityReportRequestBodyDto { Reason = string.Empty });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task OpenReport_OnACaseAlreadyRecordedAsNotFiled_IsRefused()
    {
        // Arrange
        await ArrangeAsync();
        var caseId = await SeedCaseAsync();
        var report = await OpenReportAsync(caseId);

        await _officerClient.PostAsJsonAsync(
            $"/suspicious-activity-reports/{report.SuspiciousActivityReportId}/decline",
            new DeclineSuspiciousActivityReportRequestBodyDto { Reason = "Invoice produced." });

        // Act
        var response = await _analystClient.PostAsJsonAsync(
            $"/cases/{caseId}/suspicious-activity-report", new OpenSuspiciousActivityReportRequestBodyDto());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.Content.ReadAsStringAsync()).Should().Contain("considered and recorded as not filed");
    }

    [Fact]
    public async Task ListReports_FilteredByOverdue_ReturnsOnlyTheOnesStillOwedAndPastTheirDeadline()
    {
        // Arrange
        await ArrangeAsync();
        var overdueReport = await OpenReportAsync(await SeedCaseAsync(createdAt: DateTimeOffset.UtcNow.AddDays(-60)));
        await OpenReportAsync(await SeedCaseAsync());

        // Act
        var response = await _officerClient.GetFromJsonAsync<PagedResponseDto<SuspiciousActivityReportResourceDto>>(
            "/suspicious-activity-reports?isOverdue=true");

        // Assert
        response!.Data.Should().ContainSingle();
        response.Data[0].SuspiciousActivityReport.SuspiciousActivityReportId
            .Should().Be(overdueReport.SuspiciousActivityReportId);
        response.Data[0].SuspiciousActivityReport.IsOverdue.Should().BeTrue();
    }

    [Fact]
    public async Task ListReports_FilteredByOverdue_ExcludesOnesThatHaveAlreadyBeenFiled()
    {
        // Arrange
        await ArrangeAsync();
        var report = await OpenReportAsync(await SeedCaseAsync(createdAt: DateTimeOffset.UtcNow.AddDays(-60)));
        await SubmitAsync(report.SuspiciousActivityReportId);
        await FileAsync(report.SuspiciousActivityReportId, "FIU-2026-0001");

        // Act
        var response = await _officerClient.GetFromJsonAsync<PagedResponseDto<SuspiciousActivityReportResourceDto>>(
            "/suspicious-activity-reports?isOverdue=true");

        // Assert
        response!.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task ListReports_OrdersBySoonestDeadlineFirst()
    {
        // Arrange
        await ArrangeAsync();
        var later = await OpenReportAsync(await SeedCaseAsync());
        var sooner = await OpenReportAsync(await SeedCaseAsync(createdAt: DateTimeOffset.UtcNow.AddDays(-20)));

        // Act
        var response = await _officerClient.GetFromJsonAsync<PagedResponseDto<SuspiciousActivityReportResourceDto>>(
            "/suspicious-activity-reports");

        // Assert
        response!.Data.Should().HaveCount(2);
        response.Data[0].SuspiciousActivityReport.SuspiciousActivityReportId.Should().Be(sooner.SuspiciousActivityReportId);
        response.Data[1].SuspiciousActivityReport.SuspiciousActivityReportId.Should().Be(later.SuspiciousActivityReportId);
    }

    [Fact]
    public async Task ListReports_PutsWorkStillOwedAheadOfSettledFilings()
    {
        // Arrange
        await ArrangeAsync();
        var settled = await OpenReportAsync(await SeedCaseAsync(createdAt: DateTimeOffset.UtcNow.AddDays(-60)));
        await _officerClient.PostAsJsonAsync(
            $"/suspicious-activity-reports/{settled.SuspiciousActivityReportId}/decline",
            new DeclineSuspiciousActivityReportRequestBodyDto { Reason = "Invoice produced." });

        var stillOwed = await OpenReportAsync(await SeedCaseAsync());

        // Act
        var response = await _officerClient.GetFromJsonAsync<PagedResponseDto<SuspiciousActivityReportResourceDto>>(
            "/suspicious-activity-reports");

        // Assert
        response!.Data.Should().HaveCount(2);

        response.Data[0].SuspiciousActivityReport.SuspiciousActivityReportId.Should().Be(stillOwed.SuspiciousActivityReportId);
        response.Data[1].SuspiciousActivityReport.SuspiciousActivityReportId.Should().Be(settled.SuspiciousActivityReportId);
    }

    [Fact]
    public async Task ListReports_OrdersSettledFilingsNewestFirst()
    {
        // Arrange
        await ArrangeAsync();
        var older = await OpenReportAsync(await SeedCaseAsync(createdAt: DateTimeOffset.UtcNow.AddDays(-60)));
        var newer = await OpenReportAsync(await SeedCaseAsync(createdAt: DateTimeOffset.UtcNow.AddDays(-10)));

        foreach (var report in new[] { older, newer })
        {
            await _officerClient.PostAsJsonAsync(
                $"/suspicious-activity-reports/{report.SuspiciousActivityReportId}/decline",
                new DeclineSuspiciousActivityReportRequestBodyDto { Reason = "Invoice produced." });
        }

        // Act
        var response = await _officerClient.GetFromJsonAsync<PagedResponseDto<SuspiciousActivityReportResourceDto>>(
            "/suspicious-activity-reports");

        // Assert
        response!.Data.Should().HaveCount(2);
        response.Data[0].SuspiciousActivityReport.SuspiciousActivityReportId.Should().Be(newer.SuspiciousActivityReportId);
        response.Data[1].SuspiciousActivityReport.SuspiciousActivityReportId.Should().Be(older.SuspiciousActivityReportId);
    }

    [Fact]
    public async Task GetCaseReport_ReturnsTheFilingOpenedOnThatCase()
    {
        // Arrange
        await ArrangeAsync();
        var caseId = await SeedCaseAsync();
        var report = await OpenReportAsync(caseId);

        // Act
        var response = await _analystClient.GetAsync($"/cases/{caseId}/suspicious-activity-report");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var resource = await response.Content.ReadDataFromJsonAsync<SuspiciousActivityReportResourceDto>();
        resource!.SuspiciousActivityReport.SuspiciousActivityReportId.Should().Be(report.SuspiciousActivityReportId);
        resource.SuspiciousActivityReport.CaseId.Should().Be(caseId);
    }

    [Fact]
    public async Task GetCaseReport_WhenNobodyHasOpenedOne_IsNotFound()
    {
        // Arrange
        await ArrangeAsync();
        var caseId = await SeedCaseAsync();

        // Act
        var response = await _analystClient.GetAsync($"/cases/{caseId}/suspicious-activity-report");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetCaseReport_ForACaseInAnotherOrganization_IsNotFound()
    {
        // Arrange
        await ArrangeAsync();

        // Act
        var response = await _analystClient.GetAsync($"/cases/{Guid.NewGuid()}/suspicious-activity-report");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Export_ProducesGoAmlXmlCarryingTheNarrativeAndTheReportingEntity()
    {
        // Arrange
        await ArrangeAsync();
        await SetPolicyAsync(reportingEntityId: "RE-01234");
        var report = await OpenReportAsync(await SeedCaseAsync());

        await _analystClient.PatchAsJsonAsync(
            $"/suspicious-activity-reports/{report.SuspiciousActivityReportId}",
            new UpdateSuspiciousActivityReportRequestBodyDto { Narrative = "Structuring across three days." });

        // Act
        var response = await _officerClient.GetAsync($"/suspicious-activity-reports/{report.SuspiciousActivityReportId}/export");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var document = XDocument.Parse(Encoding.UTF8.GetString(await response.Content.ReadAsByteArrayAsync()));

        document.Root!.Name.LocalName.Should().Be("report");
        document.Root.Element("rentity_id")!.Value.Should().Be("RE-01234");
        document.Root.Element("report_code")!.Value.Should().Be("STR");
        document.Root.Element("reason")!.Value.Should().Be("Structuring across three days.");
    }

    [Fact]
    public async Task Export_WithNoReportingEntityDeclared_IsRefused()
    {
        // Arrange
        await ArrangeAsync();
        var report = await OpenReportAsync(await SeedCaseAsync());

        // Act
        var response = await _officerClient.GetAsync($"/suspicious-activity-reports/{report.SuspiciousActivityReportId}/export");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GetHistory_RecordsEveryStatusMove()
    {
        // Arrange
        await ArrangeAsync();
        var report = await OpenReportAsync(await SeedCaseAsync());
        await SubmitAsync(report.SuspiciousActivityReportId);
        await FileAsync(report.SuspiciousActivityReportId, "FIU-2026-0007");

        // Act
        var response = await _officerClient.GetFromJsonAsync<PagedResponseDto<AuditLogResourceDto>>(
            $"/suspicious-activity-reports/{report.SuspiciousActivityReportId}/history?pageSize=50");

        // Assert
        var changedFields = response!.Data.SelectMany(x => x.AuditLog.Changes.Select(y => y.Field)).ToList();

        changedFields.Should().Contain("Status");
        changedFields.Should().Contain(nameof(SuspiciousActivityReportResponseDto.FilingReference));
    }

    [Fact]
    public async Task GetPolicy_WithNothingStated_IsNotFound()
    {
        // Arrange
        await ArrangeAsync();

        // Act
        var response = await _officerClient.GetAsync($"/organizations/{OrganizationId}/suspicious-activity-report-policy");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetPolicy_ReportsTheEffectiveDeadlineIncludingTheHouseDefault()
    {
        // Arrange
        await ArrangeAsync();
        await SetPolicyAsync(reportingEntityId: "RE-01234");

        // Act
        var response = await _officerClient.GetFromJsonAsync<ResponseDataDto<SuspiciousActivityReportPolicyResponseDto>>(
            $"/organizations/{OrganizationId}/suspicious-activity-report-policy");

        // Assert
        response!.Data.FilingDeadlineDays.Should().BeNull();
        response.Data.EffectiveFilingDeadlineDays.Should().Be(30);
    }

    [Fact]
    public async Task SetPolicy_StatedWithoutADeadline_DropsThePreviouslyDeclaredOne()
    {
        // Arrange
        await ArrangeAsync();
        await SetPolicyAsync(filingDeadlineDays: 5);

        // Act
        await SetPolicyAsync();

        // Assert
        var response = await _officerClient.GetFromJsonAsync<ResponseDataDto<SuspiciousActivityReportPolicyResponseDto>>(
            $"/organizations/{OrganizationId}/suspicious-activity-report-policy");

        response!.Data.FilingDeadlineDays.Should().BeNull();
        response.Data.EffectiveFilingDeadlineDays.Should().Be(30);
    }

    [Fact]
    public async Task SetPolicy_WithAnUnreadableAmountPath_IsRefused()
    {
        // Arrange
        await ArrangeAsync();

        // Act
        var response = await _officerClient.PutAsJsonAsync(
            $"/organizations/{OrganizationId}/suspicious-activity-report-policy",
            new SetOrganizationSuspiciousActivityReportPolicyRequestBodyDto
            {
                RequireFilingApproval = false,
                AmountPath = "lines[*].amount"
            });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetReport_CarriesTheFilingGateToCallersWhoCannotReadThePolicy()
    {
        // Arrange
        await ArrangeAsync(analystIsManager: false);
        await SetPolicyAsync(requireFilingApproval: true);
        var report = await OpenReportAsync(await SeedCaseAsync());

        // Act
        var policyResponse = await _analystClient.GetAsync($"/organizations/{OrganizationId}/suspicious-activity-report-policy");
        var resource = await _analystClient.GetFromJsonAsync<ResponseDataDto<SuspiciousActivityReportResourceDto>>(
            $"/suspicious-activity-reports/{report.SuspiciousActivityReportId}");

        // Assert
        policyResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        resource!.Data.RequireFilingApproval.Should().BeTrue();
    }

    [Fact]
    public async Task GetReport_WithNothingStated_ReportsTheFilingGateAsOff()
    {
        // Arrange
        await ArrangeAsync();
        var report = await OpenReportAsync(await SeedCaseAsync());

        // Act
        var resource = await _officerClient.GetFromJsonAsync<ResponseDataDto<SuspiciousActivityReportResourceDto>>(
            $"/suspicious-activity-reports/{report.SuspiciousActivityReportId}");

        // Assert
        resource!.Data.RequireFilingApproval.Should().BeFalse();
    }

    private async Task<SuspiciousActivityReportResponseDto> OpenReportAsync(Guid caseId)
    {
        var response = await _analystClient.PostAsJsonAsync(
            $"/cases/{caseId}/suspicious-activity-report", new OpenSuspiciousActivityReportRequestBodyDto());

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var resource = await response.Content.ReadDataFromJsonAsync<SuspiciousActivityReportResourceDto>();

        return resource!.SuspiciousActivityReport;
    }

    private async Task SubmitAsync(Guid suspiciousActivityReportId)
    {
        var response = await _analystClient.PostAsJsonAsync(
            $"/suspicious-activity-reports/{suspiciousActivityReportId}/submit", new { });

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Accepted);
    }

    private async Task FileAsync(Guid suspiciousActivityReportId, string filingReference)
    {
        var response = await _officerClient.PostAsJsonAsync(
            $"/suspicious-activity-reports/{suspiciousActivityReportId}/file",
            new FileSuspiciousActivityReportRequestBodyDto { FilingReference = filingReference });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task SetPolicyAsync(
        string? reportingEntityId = null,
        int? filingDeadlineDays = null,
        bool requireFilingApproval = false)
    {
        var response = await _officerClient.PutAsJsonAsync(
            $"/organizations/{OrganizationId}/suspicious-activity-report-policy",
            new SetOrganizationSuspiciousActivityReportPolicyRequestBodyDto
            {
                ReportingEntityId = reportingEntityId,
                FilingDeadlineDays = filingDeadlineDays,
                RequireFilingApproval = requireFilingApproval
            });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    private async Task<ApprovalResourceDto[]> ApprovalsAsync()
    {
        var response = await _officerClient.GetFromJsonAsync<PagedResponseDto<ApprovalResourceDto>>("/approvals?pageSize=50");

        return response!.Data;
    }

    private async Task<int> StatusAsync(Guid suspiciousActivityReportId)
    {
        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var entity = await context.SuspiciousActivityReports
            .FirstAsync(x => x.SuspiciousActivityReportId == suspiciousActivityReportId);

        return entity.StatusId;
    }

    private async Task<Guid> SeedCaseAsync(DateTimeOffset? createdAt = null)
    {
        var openedAt = createdAt ?? DateTimeOffset.UtcNow;

        var transactionEntity = FakeTransactionEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            Payload = Convert.ToBase64String(Encoding.UTF8.GetBytes("""{"amount":24500,"currency":"GBP"}""")),
            IsDeleted = false
        };

        await DatabaseHelper.CreateTransactions(testFixture.Services, [transactionEntity]);

        var caseEntity = FakeCaseEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            TransactionId = transactionEntity.TransactionId,
            Transaction = null!,
            StatusId = (int)CaseStatus.Approved,
            SeverityId = (int)RuleSeverity.High,
            DispositionId = (int)CaseDisposition.TruePositive,
            CaseReasonCodeId = null,
            QueueId = null,
            AssignedToUserId = _analyst.Id,
            AssignedToUser = null!,
            AssignedAt = openedAt,
            CreatedAt = openedAt,
            UpdatedAt = openedAt,
            DueAt = openedAt.AddHours(4),
            ResolvedAt = openedAt,
            IsDeleted = false
        };

        await DatabaseHelper.CreateCases(testFixture.Services, [caseEntity]);

        return caseEntity.CaseId;
    }

    private async Task ArrangeAsync(bool analystIsManager = true)
    {
        List<UserRole> analystRoles = analystIsManager
            ? [UserRole.User, UserRole.Manager, UserRole.Admin]
            : [UserRole.User];

        _analyst = await AuthenticationHelper.CreateUserAsync(
            testFixture.Services, AnalystEmail, Password, analystRoles, OrganizationId);

        _officer = await AuthenticationHelper.CreateUserAsync(
            testFixture.Services, OfficerEmail, Password, [UserRole.User, UserRole.Manager, UserRole.Admin], OrganizationId);

        await AuthenticationHelper.SetTokenAsync(_analystClient, AnalystEmail, Password);
        await AuthenticationHelper.SetTokenAsync(_officerClient, OfficerEmail, Password);
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
