using System.Net;
using System.Net.Http.Json;
using System.Text;
using AutoFixture;
using BehaviouralTests.TestHelpers;
using Core.Enums;
using Core.Helpers;
using Core.Interfaces.Repositories;
using Core.Jobs;
using Core.Models.AuditLogs;
using Core.Models.Cases;
using Core.ValueObjects;
using FluentAssertions;
using Infrastructure;
using Infrastructure.Configurations;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Presentation.Models.AuditLogs;
using Presentation.Models.CaseNotes;
using Presentation.Models.Cases;
using Presentation.Models.Common;
using Quartz;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

namespace BehaviouralTests.Tests.Controllers;

public class AuditTrailTests(TestFixture testFixture) : IClassFixture<TestFixture>, IAsyncLifetime
{
    private readonly Fixture _fixture = AutoFixtureHelper.Create();
    private readonly HttpClient _analystClient = testFixture.CreateClient();
    private readonly HttpClient _managerClient = testFixture.CreateClient();

    private const string AnalystEmail = "audit-analyst@mail.com";
    private const string ManagerEmail = "audit-manager@mail.com";
    private const string Password = "P@$$w0rd";
    private static readonly Guid OrganizationId = new("2f8c5a11-6bd4-4f7e-9c3a-84d1e0b7f265");

    private ApplicationUserEntity _analyst = null!;
    private ApplicationUserEntity _manager = null!;

    [Fact]
    public async Task WritingAudited_ChainsEachEntryToTheOneBeforeIt()
    {
        // Arrange
        await ArrangeAsync();
        var caseId = await SeedCaseAsync();

        // Act
        await AddNoteAsync(caseId, "First look at this one.");
        await AddNoteAsync(caseId, "Escalating to the desk lead.");

        // Assert
        var entries = await ReadChainAsync();

        entries.Should().HaveCountGreaterThan(1);
        ((long)entries[0].Sequence).Should().Be(1);
        entries[0].PreviousHash.Should().BeNull();

        for (var i = 1; i < entries.Count; i++)
        {
            ((long)entries[i].Sequence).Should().Be(entries[i - 1].Sequence + 1);
            entries[i].PreviousHash.Should().Be(entries[i - 1].Hash);
        }
    }

    [Fact]
    public async Task WritingAudited_ProducesADigestTheApplicationCanRecompute()
    {
        // Arrange
        await ArrangeAsync();
        var caseId = await SeedCaseAsync();
        await AddNoteAsync(caseId, "A note worth hashing.");

        // Act
        var entries = await ReadChainAsync();

        // Assert
        foreach (var entry in entries)
        {
            AuditChainHasher.Compute(entry).Should().Be(entry.Hash);
        }
    }

    [Fact]
    public async Task WritingAudited_KeepsAChainPerOrganization()
    {
        // Arrange
        await ArrangeAsync();
        var otherOrganizationId = Guid.NewGuid();

        await DatabaseHelper.CreateOrganizations(testFixture.Services,
        [
            FakeOrganizationEntity.CreateValid(_fixture) with
            {
                OrganizationId = otherOrganizationId,
                StatusId = (int)OrganizationStatus.Enabled,
                IsDeleted = false
            }
        ]);

        var caseId = await SeedCaseAsync();

        // Act
        await AddNoteAsync(caseId, "One desk's work.");
        await SeedCaseAsync(otherOrganizationId);

        // Assert
        var ours = await ReadChainAsync();
        var theirs = await ReadChainAsync(otherOrganizationId);

        ((long)ours[0].Sequence).Should().Be(1);
        ((long)theirs[0].Sequence).Should().Be(1);
        theirs.Should().OnlyContain(x => x.OrganizationId == new OrganizationId(otherOrganizationId));
    }

    [Fact]
    public async Task EditingAnEntryDirectly_IsRefusedByTheDatabase()
    {
        // Arrange
        await ArrangeAsync();
        var caseId = await SeedCaseAsync();
        await AddNoteAsync(caseId, "Something worth denying later.");

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        // Act
        var edit = async () => await context.Database.ExecuteSqlRawAsync(
            """UPDATE "Service"."AuditLogs" SET "Snapshot" = jsonb_build_object() WHERE "Sequence" = 1;""");

        // Assert
        await edit.Should().ThrowAsync<Exception>().WithMessage("*append only*");
    }

    [Fact]
    public async Task DeletingAnEntryDirectly_IsRefusedByTheDatabase()
    {
        // Arrange
        await ArrangeAsync();
        var caseId = await SeedCaseAsync();
        await AddNoteAsync(caseId, "Something worth removing later.");

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        // Act
        var delete = async () => await context.Database.ExecuteSqlRawAsync(
            """DELETE FROM "Service"."AuditLogs" WHERE "Sequence" = 1;""");

        // Assert
        await delete.Should().ThrowAsync<Exception>().WithMessage("*append only*");
    }

    [Fact]
    public async Task EditingAnAnchorDirectly_IsRefusedByTheDatabase()
    {
        // Arrange
        await ArrangeAsync();
        var caseId = await SeedCaseAsync();
        await AddNoteAsync(caseId, "A note to anchor over.");
        await ExecuteAnchorJobAsync();

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        // Act
        var edit = async () => await context.Database.ExecuteSqlRawAsync(
            """UPDATE "Service"."AuditAnchors" SET "Sequence" = 1;""");

        // Assert
        await edit.Should().ThrowAsync<Exception>().WithMessage("*append only*");
    }

    [Fact]
    public async Task VerifyChain_ReportsAnUntouchedChainAsIntact()
    {
        // Arrange
        await ArrangeAsync();
        var caseId = await SeedCaseAsync();
        await AddNoteAsync(caseId, "Nothing has been done to this.");

        // Act
        var response = await _managerClient.GetAsync("/audit-logs/verification");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var verification = await response.Content.ReadDataFromJsonAsync<AuditChainVerificationResponseDto>();

        verification!.IsIntact.Should().BeTrue();
        verification.EntryCount.Should().BeGreaterThan(0);
        verification.FirstBrokenAuditLogId.Should().BeNull();
    }

    [Fact]
    public async Task VerifyChain_FindsAnEntryThatWasEditedBehindTheGuard()
    {
        // Arrange
        await ArrangeAsync();
        var caseId = await SeedCaseAsync();
        await AddNoteAsync(caseId, "The note somebody would rather nobody read.");

        var tampered = await TamperWithEntryAsync();

        // Act
        var response = await _managerClient.GetAsync("/audit-logs/verification");

        // Assert
        var verification = await response.Content.ReadDataFromJsonAsync<AuditChainVerificationResponseDto>();

        verification!.IsIntact.Should().BeFalse();
        verification.FirstBrokenAuditLogId.Should().Be(tampered);
    }

    [Fact]
    public async Task VerifyChain_FindsAnAnchorTheChainNoLongerAgreesWith()
    {
        // Arrange
        await ArrangeAsync();
        var caseId = await SeedCaseAsync();
        await AddNoteAsync(caseId, "Anchored while it still said this.");
        await ExecuteAnchorJobAsync();

        await RewriteWholeChainAsync();

        // Act
        var response = await _managerClient.GetAsync("/audit-logs/verification");

        // Assert
        var verification = await response.Content.ReadDataFromJsonAsync<AuditChainVerificationResponseDto>();

        verification!.IsIntact.Should().BeFalse();
        verification.FirstBrokenAuditAnchorId.Should().NotBeNull();
    }

    [Fact]
    public async Task VerifyChain_IsRefusedToAnAnalyst()
    {
        // Arrange
        await ArrangeAsync(analystIsManager: false);

        // Act
        var response = await _analystClient.GetAsync("/audit-logs/verification");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AnchorJob_WitnessesEachChainOnceAndSaysNothingNewTheSecondTime()
    {
        // Arrange
        await ArrangeAsync();
        var caseId = await SeedCaseAsync();
        await AddNoteAsync(caseId, "Worth anchoring.");

        // Act
        await ExecuteAnchorJobAsync();
        var afterFirst = await ReadAnchorsAsync();

        await ExecuteAnchorJobAsync();
        var afterSecond = await ReadAnchorsAsync();

        // Assert
        afterFirst.Should().NotBeEmpty();
        afterSecond.Should().HaveCount(afterFirst.Count);
    }

    [Fact]
    public async Task AnchorJob_TakesAFreshAnchorOnceTheChainHasMovedOn()
    {
        // Arrange
        await ArrangeAsync();
        var caseId = await SeedCaseAsync();
        await AddNoteAsync(caseId, "First.");
        await ExecuteAnchorJobAsync();

        // Act
        await AddNoteAsync(caseId, "Second, after the anchor.");
        await ExecuteAnchorJobAsync();

        // Assert
        var anchors = await ReadAnchorsAsync();

        anchors.Where(x => x.OrganizationId == OrganizationId).Should().HaveCountGreaterThan(1);
    }

    [Fact]
    public async Task ExportAuditLogs_ReturnsACsvCarryingTheChainColumns()
    {
        // Arrange
        await ArrangeAsync();
        var caseId = await SeedCaseAsync();
        await AddNoteAsync(caseId, "A line the export has to carry.");

        // Act
        var response = await _managerClient.GetAsync("/audit-logs/export");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/csv");

        var content = await response.Content.ReadAsStringAsync();

        content.Should().Contain("Sequence,AuditLogId,CreatedAt");
        content.Should().Contain("PreviousHash,Hash");

        var entries = await ReadChainAsync();
        content.Should().Contain(entries[0].Hash.ToString());
    }

    [Fact]
    public async Task ExportAuditLogs_NarrowedToAnEntity_CarriesOnlyThatRecordsHistory()
    {
        // Arrange
        await ArrangeAsync();
        var caseId = await SeedCaseAsync();
        var otherCaseId = await SeedCaseAsync();

        await AddNoteAsync(caseId, "About the first case.");

        // Act
        var response = await _managerClient.GetAsync(
            $"/audit-logs/export?entityType={nameof(AuditableEntityType.Case)}&entityId={caseId}");

        // Assert
        var content = await response.Content.ReadAsStringAsync();

        content.Should().Contain(caseId.ToString());
        content.Should().NotContain(otherCaseId.ToString());
    }

    [Fact]
    public async Task ExportAuditLogs_WithAnEntityIdAndNoType_IsRefused()
    {
        // Arrange
        await ArrangeAsync();

        // Act
        var response = await _managerClient.GetAsync($"/audit-logs/export?entityId={Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ExportAuditLogs_IsRefusedToAnAnalyst()
    {
        // Arrange
        await ArrangeAsync(analystIsManager: false);

        // Act
        var response = await _analystClient.GetAsync("/audit-logs/export");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetEvidencePack_ReturnsAPdfForTheCase()
    {
        // Arrange
        await ArrangeAsync();
        var caseId = await SeedCaseAsync();
        await AddNoteAsync(caseId, "The note the pack has to carry.");

        // Act
        var response = await _analystClient.GetAsync($"/cases/{caseId}/evidence-pack");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");

        var content = await response.Content.ReadAsByteArrayAsync();

        content.Should().NotBeEmpty();
        Encoding.ASCII.GetString(content.Take(5).ToArray()).Should().Be("%PDF-");
    }

    [Fact]
    public async Task GetEvidencePack_CarriesTheHistoryOfAWithdrawnNote()
    {
        // Arrange
        await ArrangeAsync();
        var caseId = await SeedCaseAsync();
        var caseNoteId = await AddNoteAsync(caseId, "The note somebody thought better of.");

        var withdrawal = await _analystClient.DeleteAsync($"/cases/{caseId}/notes/{caseNoteId}");
        withdrawal.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Act
        var evidencePackSource = await ReadEvidencePackSourceAsync(caseId);

        // Assert
        evidencePackSource.Notes.Should().NotContain(x => x.CaseNoteId == caseNoteId);
        evidencePackSource.Timeline.Should().Contain(x =>
            x.EntityType == AuditableEntityType.CaseNote &&
            x.EntityId == caseNoteId &&
            x.Operation == AuditOperation.Deleted);
    }

    [Fact]
    public async Task GetEvidencePack_NamesTheMostRecentDecisionAsTheFinalOne()
    {
        // Arrange
        await ArrangeAsync();
        var caseId = await SeedCaseAsync();
        var transactionId = await ReadCaseTransactionIdAsync(caseId);
        var now = DateTimeOffset.UtcNow;

        var earlierTransaction = FakeTransactionEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            IsDeleted = false
        };

        await DatabaseHelper.CreateTransactions(testFixture.Services, [earlierTransaction]);

        var superseded = FakeDecisionEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            TransactionId = earlierTransaction.TransactionId,
            Transaction = null!,
            CaseId = caseId,
            StatusId = (int)DecisionStatus.Held,
            ResolvedByUserId = null,
            CreatedAt = now.AddHours(-2),
            UpdatedAt = now.AddHours(-2)
        };

        var latest = FakeDecisionEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            TransactionId = transactionId,
            Transaction = null!,
            CaseId = caseId,
            StatusId = (int)DecisionStatus.Blocked,
            ResolvedByUserId = _analyst.Id,
            ResolvedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };

        await DatabaseHelper.CreateDecisions(testFixture.Services, [latest, superseded]);

        // Act
        var evidencePackSource = await ReadEvidencePackSourceAsync(caseId);

        // Assert
        evidencePackSource.Decision.Should().NotBeNull();
        ((Guid)evidencePackSource.Decision!.DecisionId).Should().Be(latest.DecisionId);
    }

    [Fact]
    public async Task GetEvidencePack_IsOpenToTheAnalystWhoWorksTheCase()
    {
        // Arrange
        await ArrangeAsync(analystIsManager: false);
        var caseId = await SeedCaseAsync();

        // Act
        var response = await _analystClient.GetAsync($"/cases/{caseId}/evidence-pack");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetCaseHistory_CarriesEachEntrysPositionAndDigest()
    {
        // Arrange
        await ArrangeAsync();
        var caseId = await SeedCaseAsync();
        await AddNoteAsync(caseId, "So the case has history.");

        await _analystClient.PostAsJsonAsync($"/cases/{caseId}/status",
            new ChangeCaseStatusRequestBodyDto { Status = nameof(CaseStatus.InReview) });

        // Act
        var response = await _analystClient.GetAsync($"/cases/{caseId}/history");

        // Assert
        var history = await response.Content.ReadFromJsonAsync<PagedResponseDto<AuditLogResourceDto>>();

        history!.Data.Should().NotBeEmpty();
        history.Data.Should().OnlyContain(x => x.AuditLog.Sequence > 0);
        history.Data.Should().OnlyContain(x => x.AuditLog.Hash.Length == 64);
    }

    private async Task<List<AuditLogEntry>> ReadChainAsync(Guid? organizationId = null)
    {
        var forOrganization = organizationId ?? OrganizationId;

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var auditLogEntities = await context.AuditLogs
            .Where(x => x.OrganizationId == forOrganization)
            .OrderBy(x => x.Sequence)
            .ToListAsync();

        return auditLogEntities.ConvertAll(x => new AuditLogEntry
        {
            AuditLogId = new AuditLogId(x.AuditLogId),
            EntityType = (AuditableEntityType)x.EntityTypeId,
            EntityId = x.EntityId,
            OrganizationId = x.OrganizationId.HasValue ? new OrganizationId(x.OrganizationId.Value) : null,
            Operation = (AuditOperation)x.OperationId,
            Snapshot = new Snapshot(x.Snapshot),
            ChangedByUserId = x.ChangedByUserId.HasValue ? new ApplicationUserId(x.ChangedByUserId.Value) : null,
            ChangedByApiClientId = x.ChangedByApiClientId.HasValue ? new ApiClientId(x.ChangedByApiClientId.Value) : null,
            Sequence = new Sequence(x.Sequence),
            PreviousHash = x.PreviousHash is not null ? new Hash(x.PreviousHash) : null,
            Hash = new Hash(x.Hash),
            CreatedAt = new CreatedAt(x.CreatedAt)
        });
    }

    private async Task<List<AuditAnchorEntity>> ReadAnchorsAsync()
    {
        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        return await context.AuditAnchors.ToListAsync();
    }

    private async Task<Guid> TamperWithEntryAsync()
    {
        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var target = await context.AuditLogs
            .Where(x => x.OrganizationId == OrganizationId)
            .OrderByDescending(x => x.Sequence)
            .FirstAsync();

        await WithoutAppendOnlyGuardAsync(context,
            """UPDATE "Service"."AuditLogs" SET "Snapshot" = jsonb_build_object('Message', 'something else') WHERE "AuditLogId" = '@target';"""
                .Replace("@target", target.AuditLogId.ToString(), StringComparison.Ordinal));

        return target.AuditLogId;
    }

    private async Task RewriteWholeChainAsync()
    {
        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        await WithoutAppendOnlyGuardAsync(context,
            """
            UPDATE "Service"."AuditLogs"
            SET "Snapshot" = jsonb_build_object('Message', 'rewritten')
            WHERE "OrganizationId" = '@organization';

            WITH RECURSIVE "ordered" AS (
                SELECT a.*, row_number() OVER (ORDER BY a."Sequence") AS "Position"
                FROM "Service"."AuditLogs" a
                WHERE a."OrganizationId" = '@organization'
            ),
            "chained" AS (
                SELECT o."AuditLogId", o."Position", NULL::text AS "PreviousHash",
                       "Service"."ComputeAuditLogHash"(
                           o."AuditLogId", o."EntityTypeId", o."EntityId", o."OrganizationId", o."OperationId",
                           o."Snapshot", o."ChangedByUserId", o."ChangedByApiClientId", o."CreatedAt",
                           o."Position", NULL) AS "Hash"
                FROM "ordered" o WHERE o."Position" = 1
                UNION ALL
                SELECT o."AuditLogId", o."Position", c."Hash",
                       "Service"."ComputeAuditLogHash"(
                           o."AuditLogId", o."EntityTypeId", o."EntityId", o."OrganizationId", o."OperationId",
                           o."Snapshot", o."ChangedByUserId", o."ChangedByApiClientId", o."CreatedAt",
                           o."Position", c."Hash") AS "Hash"
                FROM "chained" c
                JOIN "ordered" o ON o."Position" = c."Position" + 1
            )
            UPDATE "Service"."AuditLogs" a
            SET "PreviousHash" = c."PreviousHash", "Hash" = c."Hash"
            FROM "chained" c
            WHERE a."AuditLogId" = c."AuditLogId";
            """.Replace("@organization", OrganizationId.ToString(), StringComparison.Ordinal));
    }

    private static async Task WithoutAppendOnlyGuardAsync(TransactionMonitoringDbContext context, string sql)
    {
        await context.Database.ExecuteSqlRawAsync(
            """ALTER TABLE "Service"."AuditLogs" DISABLE TRIGGER "AuditLogs_AppendOnly";""");

        try
        {
            await context.Database.ExecuteSqlRawAsync(sql);
        }
        finally
        {
            await context.Database.ExecuteSqlRawAsync(
                """ALTER TABLE "Service"."AuditLogs" ENABLE TRIGGER "AuditLogs_AppendOnly";""");
        }
    }

    private async Task ExecuteAnchorJobAsync()
    {
        using var scope = testFixture.Services.CreateScope();
        var job = ActivatorUtilities.CreateInstance<AnchorAuditChainJob>(scope.ServiceProvider);

        var mockContext = new Mock<IJobExecutionContext>();
        mockContext.SetupGet(x => x.CancellationToken).Returns(CancellationToken.None);

        await job.Execute(mockContext.Object);
    }

    private async Task<Guid> AddNoteAsync(Guid caseId, string message)
    {
        var response = await _analystClient.PostAsJsonAsync(
            $"/cases/{caseId}/notes", new CreateCaseNoteRequestBodyDto { Message = message });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var resource = await response.Content.ReadDataFromJsonAsync<CaseNoteResourceDto>();

        return resource!.CaseNote.CaseNoteId;
    }

    private async Task<EvidencePackSource> ReadEvidencePackSourceAsync(Guid caseId)
    {
        using var scope = testFixture.Services.CreateScope();
        var caseRepository = scope.ServiceProvider.GetRequiredService<ICaseRepository>();

        return await caseRepository.GetEvidencePackSourceAsync(new CaseId(caseId), CancellationToken.None);
    }

    private async Task<Guid> ReadCaseTransactionIdAsync(Guid caseId)
    {
        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var caseEntity = await context.Cases.FirstAsync(x => x.CaseId == caseId);

        return caseEntity.TransactionId!.Value;
    }

    private async Task<Guid> SeedCaseAsync(Guid? organizationId = null)
    {
        var forOrganization = organizationId ?? OrganizationId;
        var openedAt = DateTimeOffset.UtcNow;

        var transactionEntity = FakeTransactionEntity.CreateValid(_fixture) with
        {
            OrganizationId = forOrganization,
            Organization = null!,
            Payload = Convert.ToBase64String(Encoding.UTF8.GetBytes("""{"amount":24500,"currency":"GBP"}""")),
            IsDeleted = false
        };

        await DatabaseHelper.CreateTransactions(testFixture.Services, [transactionEntity]);

        var caseEntity = FakeCaseEntity.CreateValid(_fixture) with
        {
            OrganizationId = forOrganization,
            Organization = null!,
            TransactionId = transactionEntity.TransactionId,
            Transaction = null!,
            StatusId = (int)CaseStatus.Pending,
            SeverityId = (int)RuleSeverity.High,
            DispositionId = null,
            CaseReasonCodeId = null,
            QueueId = null,
            AssignedToUserId = null,
            AssignedToUser = null!,
            AssignedAt = null,
            CreatedAt = openedAt,
            UpdatedAt = openedAt,
            DueAt = openedAt.AddHours(4),
            ResolvedAt = null,
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

        _manager = await AuthenticationHelper.CreateUserAsync(
            testFixture.Services, ManagerEmail, Password, [UserRole.User, UserRole.Manager, UserRole.Admin], OrganizationId);

        await AuthenticationHelper.SetTokenAsync(_analystClient, AnalystEmail, Password);
        await AuthenticationHelper.SetTokenAsync(_managerClient, ManagerEmail, Password);
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
