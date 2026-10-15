using AutoFixture;
using BehaviouralTests.TestHelpers;
using Core.Enums;
using Core.Interfaces.Services;
using FluentAssertions;
using Infrastructure;
using Infrastructure.Configurations;
using Infrastructure.Entities;
using Microsoft.Extensions.DependencyInjection;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

namespace BehaviouralTests.Tests.Jobs;

public class ExpireHeldDecisionJobTests(TestFixture testFixture) : IClassFixture<TestFixture>, IAsyncLifetime
{
    private readonly Fixture _fixture = AutoFixtureHelper.Create();

    private static readonly Guid OrganizationId = new("4d6389e9-f5d1-4325-a004-915865e3adec");

    [Theory]
    [InlineData(HoldExpiryVerdict.Release, DecisionStatus.Released)]
    [InlineData(HoldExpiryVerdict.Block, DecisionStatus.Blocked)]
    [InlineData(HoldExpiryVerdict.Expire, DecisionStatus.Expired)]
    public async Task ExpireDueAsync_GivenADueHoldWithACase_AppliesTheVerdictAndExpiresTheCase(
        HoldExpiryVerdict holdExpiryVerdict,
        DecisionStatus expectedStatus)
    {
        // Arrange
        var transactionEntity = FakeTransactionEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            ExternalReference = "pay_expire_me",
            IsDeleted = false
        };

        await DatabaseHelper.CreateTransactions(testFixture.Services, [transactionEntity]);

        var caseEntity = FakeCaseEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            TransactionId = transactionEntity.TransactionId,
            AssignedToUserId = null,
            AssignedToUser = null!,
            StatusId = (int)CaseStatus.Pending,
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
            StatusId = (int)DecisionStatus.Held,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1),
            HoldExpiryVerdictId = (int)holdExpiryVerdict
        };

        await DatabaseHelper.CreateDecisions(testFixture.Services, [decisionEntity]);

        using var scope = testFixture.Services.CreateScope();
        var decisionService = scope.ServiceProvider.GetRequiredService<IDecisionService>();

        // Act
        var expired = await decisionService.ExpireDueAsync(CancellationToken.None);

        // Assert
        ((int)expired).Should().Be(1);

        using var assertScope = testFixture.Services.CreateScope();
        var context = assertScope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var updatedDecisionEntity = context.Decisions.FirstOrDefault(x => x.DecisionId == decisionEntity.DecisionId);
        updatedDecisionEntity.Should().NotBeNull();
        updatedDecisionEntity!.StatusId.Should().Be((int)expectedStatus);
        updatedDecisionEntity.ResolvedAt.Should().NotBeNull();

        var updatedCaseEntity = context.Cases.FirstOrDefault(x => x.CaseId == caseEntity.CaseId);
        updatedCaseEntity.Should().NotBeNull();
        updatedCaseEntity!.StatusId.Should().Be((int)CaseStatus.Expired);
        updatedCaseEntity.ResolvedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ExpireDueAsync_GivenANeverExpiringHold_LeavesItHeld()
    {
        // Arrange
        var transactionEntity = FakeTransactionEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            ExternalReference = "pay_hold_forever",
            IsDeleted = false
        };

        await DatabaseHelper.CreateTransactions(testFixture.Services, [transactionEntity]);

        var decisionEntity = FakeDecisionEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            TransactionId = transactionEntity.TransactionId,
            Transaction = null!,
            StatusId = (int)DecisionStatus.Held,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1),
            HoldExpiryVerdictId = (int)HoldExpiryVerdict.Never
        };

        await DatabaseHelper.CreateDecisions(testFixture.Services, [decisionEntity]);

        using var scope = testFixture.Services.CreateScope();
        var decisionService = scope.ServiceProvider.GetRequiredService<IDecisionService>();

        // Act
        var expired = await decisionService.ExpireDueAsync(CancellationToken.None);

        // Assert
        ((int)expired).Should().Be(0);

        using var assertScope = testFixture.Services.CreateScope();
        var context = assertScope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var updatedDecisionEntity = context.Decisions.FirstOrDefault(x => x.DecisionId == decisionEntity.DecisionId);
        updatedDecisionEntity.Should().NotBeNull();
        updatedDecisionEntity!.StatusId.Should().Be((int)DecisionStatus.Held);
    }

    [Fact]
    public async Task ExpireDueAsync_GivenACaseStillHoldingAnotherPayment_LeavesTheCaseOpen()
    {
        // Arrange
        var dueTransactionEntity = FakeTransactionEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            ExternalReference = "pay_due",
            IsDeleted = false
        };

        var outstandingTransactionEntity = FakeTransactionEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            ExternalReference = "pay_outstanding",
            IsDeleted = false
        };

        await DatabaseHelper.CreateTransactions(testFixture.Services, [dueTransactionEntity, outstandingTransactionEntity]);

        var caseEntity = FakeCaseEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            TransactionId = dueTransactionEntity.TransactionId,
            AssignedToUserId = null,
            AssignedToUser = null!,
            StatusId = (int)CaseStatus.Pending,
            ResolvedAt = null,
            IsDeleted = false
        };

        await DatabaseHelper.CreateCases(testFixture.Services, [caseEntity]);

        var dueDecisionEntity = FakeDecisionEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            TransactionId = dueTransactionEntity.TransactionId,
            Transaction = null!,
            CaseId = caseEntity.CaseId,
            StatusId = (int)DecisionStatus.Held,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1),
            HoldExpiryVerdictId = (int)HoldExpiryVerdict.Release
        };

        var outstandingDecisionEntity = FakeDecisionEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            TransactionId = outstandingTransactionEntity.TransactionId,
            Transaction = null!,
            CaseId = caseEntity.CaseId,
            StatusId = (int)DecisionStatus.Held,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
            HoldExpiryVerdictId = (int)HoldExpiryVerdict.Release
        };

        await DatabaseHelper.CreateDecisions(testFixture.Services, [dueDecisionEntity, outstandingDecisionEntity]);

        using var scope = testFixture.Services.CreateScope();
        var decisionService = scope.ServiceProvider.GetRequiredService<IDecisionService>();

        // Act
        var expired = await decisionService.ExpireDueAsync(CancellationToken.None);

        // Assert
        ((int)expired).Should().Be(1);

        using var assertScope = testFixture.Services.CreateScope();
        var context = assertScope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var updatedDueDecisionEntity = context.Decisions.First(x => x.DecisionId == dueDecisionEntity.DecisionId);
        updatedDueDecisionEntity.StatusId.Should().Be((int)DecisionStatus.Released);

        var untouchedDecisionEntity = context.Decisions.First(x => x.DecisionId == outstandingDecisionEntity.DecisionId);
        untouchedDecisionEntity.StatusId.Should().Be((int)DecisionStatus.Held);

        var updatedCaseEntity = context.Cases.First(x => x.CaseId == caseEntity.CaseId);
        updatedCaseEntity.StatusId.Should().Be((int)CaseStatus.Pending);
        updatedCaseEntity.ResolvedAt.Should().BeNull();
    }

    [Fact]
    public async Task ExpireDueAsync_GivenEveryPaymentUnderTheCaseSettled_ExpiresIt()
    {
        // Arrange
        var settledTransactionEntity = FakeTransactionEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            ExternalReference = "pay_settled",
            IsDeleted = false
        };

        var dueTransactionEntity = FakeTransactionEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            ExternalReference = "pay_last",
            IsDeleted = false
        };

        await DatabaseHelper.CreateTransactions(testFixture.Services, [settledTransactionEntity, dueTransactionEntity]);

        var caseEntity = FakeCaseEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            TransactionId = dueTransactionEntity.TransactionId,
            AssignedToUserId = null,
            AssignedToUser = null!,
            StatusId = (int)CaseStatus.Pending,
            ResolvedAt = null,
            IsDeleted = false
        };

        await DatabaseHelper.CreateCases(testFixture.Services, [caseEntity]);

        var settledDecisionEntity = FakeDecisionEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            TransactionId = settledTransactionEntity.TransactionId,
            Transaction = null!,
            CaseId = caseEntity.CaseId,
            StatusId = (int)DecisionStatus.Released,
            ExpiresAt = null,
            HoldExpiryVerdictId = null
        };

        var dueDecisionEntity = FakeDecisionEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            TransactionId = dueTransactionEntity.TransactionId,
            Transaction = null!,
            CaseId = caseEntity.CaseId,
            StatusId = (int)DecisionStatus.Held,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1),
            HoldExpiryVerdictId = (int)HoldExpiryVerdict.Expire
        };

        await DatabaseHelper.CreateDecisions(testFixture.Services, [settledDecisionEntity, dueDecisionEntity]);

        using var scope = testFixture.Services.CreateScope();
        var decisionService = scope.ServiceProvider.GetRequiredService<IDecisionService>();

        // Act
        await decisionService.ExpireDueAsync(CancellationToken.None);

        // Assert
        using var assertScope = testFixture.Services.CreateScope();
        var context = assertScope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var updatedCaseEntity = context.Cases.First(x => x.CaseId == caseEntity.CaseId);
        updatedCaseEntity.StatusId.Should().Be((int)CaseStatus.Expired);
        updatedCaseEntity.ResolvedAt.Should().NotBeNull();
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
