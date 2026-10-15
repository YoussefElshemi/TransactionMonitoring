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
using Presentation.Models.AuditLogs;
using Presentation.Models.Cases;
using Presentation.Models.Common;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

namespace BehaviouralTests.Tests.Controllers;

public class CaseDeskStateTests(TestFixture testFixture) : IClassFixture<TestFixture>, IAsyncLifetime
{
    private readonly Fixture _fixture = AutoFixtureHelper.Create();
    private readonly HttpClient _httpClient = testFixture.CreateClient();

    private const string Email = "desk@mail.com";
    private const string Password = "P@$$w0rd";
    private static readonly Guid OrganizationId = new("4d6389e9-f5d1-4325-a004-915865e3adec");

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

    [Theory]
    [InlineData(CaseStatus.InReview)]
    [InlineData(CaseStatus.Escalated)]
    [InlineData(CaseStatus.PendingInformation)]
    [InlineData(CaseStatus.OnHold)]
    public async Task ChangeCaseStatus_FromAssigned_MovesTheCase(CaseStatus target)
    {
        // Arrange
        var caseId = await CreateCaseAsync(CaseStatus.Assigned);

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/cases/{caseId}/status", new ChangeCaseStatusRequestBodyDto
        {
            Status = target.ToString()
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var caseEntity = await GetCaseAsync(caseId);

        caseEntity.StatusId.Should().Be((int)target);
    }

    [Fact]
    public async Task ChangeCaseStatus_ToATerminalStatus_IsRefused()
    {
        // Arrange
        var caseId = await CreateCaseAsync(CaseStatus.InReview);

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/cases/{caseId}/status", new ChangeCaseStatusRequestBodyDto
        {
            Status = nameof(CaseStatus.Approved)
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ChangeCaseStatus_FromPending_IsRefused()
    {
        // Arrange
        var caseId = await CreateCaseAsync(CaseStatus.Pending);

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/cases/{caseId}/status", new ChangeCaseStatusRequestBodyDto
        {
            Status = nameof(CaseStatus.InReview)
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ChangeCaseStatus_GivenANote_WritesItAgainstTheCase()
    {
        // Arrange
        var caseId = await CreateCaseAsync(CaseStatus.Assigned);

        // Act
        await _httpClient.PostAsJsonAsync($"/cases/{caseId}/status", new ChangeCaseStatusRequestBodyDto
        {
            Status = nameof(CaseStatus.PendingInformation),
            Note = "Waiting on the customer's source of funds"
        });

        // Assert
        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var caseNotes = await context.CaseNotes.Where(x => x.CaseId == caseId).ToListAsync();

        caseNotes.Should().ContainSingle(x => x.Message == "Waiting on the customer's source of funds");
    }

    [Fact]
    public async Task ResolveCase_FromADeskState_IsAllowed()
    {
        // Arrange
        var caseId = await CreateCaseAsync(CaseStatus.Escalated);

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/cases/{caseId}/resolve", new ResolveCaseRequestBodyDto
        {
            Status = nameof(CaseStatus.Approved),
            Disposition = nameof(CaseDisposition.FalsePositive)
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ReopenCase_ClearsTheDispositionAndPutsTheWorkBackInReview()
    {
        // Arrange
        var caseId = await CreateCaseAsync(CaseStatus.Assigned);

        await _httpClient.PostAsJsonAsync($"/cases/{caseId}/resolve", new ResolveCaseRequestBodyDto
        {
            Status = nameof(CaseStatus.Approved),
            Disposition = nameof(CaseDisposition.FalsePositive)
        });

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/cases/{caseId}/reopen", new ReopenCaseRequestBodyDto
        {
            Reason = "New information from the receiving bank"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var caseEntity = await GetCaseAsync(caseId);

        caseEntity.StatusId.Should().Be((int)CaseStatus.InReview);
        caseEntity.DispositionId.Should().BeNull();
        caseEntity.ResolvedAt.Should().BeNull();
        caseEntity.ReopenedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ReopenCase_PreservesTheOriginalDispositionInHistory()
    {
        // Arrange
        var caseId = await CreateCaseAsync(CaseStatus.Assigned);

        await _httpClient.PostAsJsonAsync($"/cases/{caseId}/resolve", new ResolveCaseRequestBodyDto
        {
            Status = nameof(CaseStatus.Approved),
            Disposition = nameof(CaseDisposition.TruePositive),
            Note = "Confirmed mule account"
        });

        // Act
        await _httpClient.PostAsJsonAsync($"/cases/{caseId}/reopen", new ReopenCaseRequestBodyDto
        {
            Reason = "New information from the receiving bank"
        });

        // Assert
        var history = await _httpClient.GetFromJsonAsync<PagedResponseDto<AuditLogResourceDto>>($"/cases/{caseId}/history");

        history.Should().NotBeNull();

        var dispositionChanges = history!.Data
            .SelectMany(x => x.AuditLog.Changes)
            .Where(x => x.Field == "Disposition")
            .ToList();

        dispositionChanges.Should().Contain(x => x.NewValue != null && x.NewValue.GetValue<string>() == nameof(CaseDisposition.TruePositive));
        dispositionChanges.Should().Contain(x => x.NewValue == null && x.OldValue != null && x.OldValue.GetValue<string>() == nameof(CaseDisposition.TruePositive));
    }

    [Fact]
    public async Task ReopenCase_WithoutAReason_IsRefused()
    {
        // Arrange
        var caseId = await CreateCaseAsync(CaseStatus.Assigned);

        await _httpClient.PostAsJsonAsync($"/cases/{caseId}/resolve", new ResolveCaseRequestBodyDto
        {
            Status = nameof(CaseStatus.Approved),
            Disposition = nameof(CaseDisposition.FalsePositive)
        });

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/cases/{caseId}/reopen", new { reason = string.Empty });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReopenCase_GivenAnOpenCase_IsRefused()
    {
        // Arrange
        var caseId = await CreateCaseAsync(CaseStatus.InReview);

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/cases/{caseId}/reopen", new ReopenCaseRequestBodyDto
        {
            Reason = "New information from the receiving bank"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReopenCase_GivenAnExpiredCase_IsAllowed()
    {
        // Arrange
        var caseId = await CreateCaseAsync(CaseStatus.Expired);

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/cases/{caseId}/reopen", new ReopenCaseRequestBodyDto
        {
            Reason = "Reviewed after the fact"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ReopenCase_GivenAnExpiredCaseNobodyWasHolding_ReturnsItToTheQueue()
    {
        // Arrange
        var caseId = await CreateCaseAsync(CaseStatus.Expired, assigned: false);

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/cases/{caseId}/reopen", new ReopenCaseRequestBodyDto
        {
            Reason = "Reviewed after the fact"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var caseEntity = await GetCaseAsync(caseId);

        caseEntity.StatusId.Should().Be((int)CaseStatus.Pending);
        caseEntity.ReopenedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ReopenCase_GivenAnExpiredCaseNobodyWasHolding_LeavesItAssignable()
    {
        // Arrange
        var caseId = await CreateCaseAsync(CaseStatus.Expired, assigned: false);

        await _httpClient.PostAsJsonAsync($"/cases/{caseId}/reopen", new ReopenCaseRequestBodyDto
        {
            Reason = "Reviewed after the fact"
        });

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/cases/{caseId}/assign", new { userId = _userId });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var caseEntity = await GetCaseAsync(caseId);

        caseEntity.StatusId.Should().Be((int)CaseStatus.Assigned);
        caseEntity.AssignedToUserId.Should().Be(_userId);
    }

    [Fact]
    public async Task ReopenCase_GivenACaseSomebodyWasHolding_PutsItBackInReview()
    {
        // Arrange
        var caseId = await CreateCaseAsync(CaseStatus.Assigned);

        await _httpClient.PostAsJsonAsync($"/cases/{caseId}/resolve", new ResolveCaseRequestBodyDto
        {
            Status = nameof(CaseStatus.Approved),
            Disposition = nameof(CaseDisposition.FalsePositive)
        });

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/cases/{caseId}/reopen", new ReopenCaseRequestBodyDto
        {
            Reason = "New information from the receiving bank"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var caseEntity = await GetCaseAsync(caseId);

        caseEntity.StatusId.Should().Be((int)CaseStatus.InReview);
        caseEntity.AssignedToUserId.Should().Be(_userId);
    }

    private async Task<Guid> CreateCaseAsync(CaseStatus caseStatus, bool assigned = true)
    {
        var caseEntity = FakeCaseEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            AssignedToUserId = assigned ? _userId : null,
            AssignedToUser = null!,
            AssignedAt = assigned ? DateTimeOffset.UtcNow : null,
            StatusId = (int)caseStatus,
            DispositionId = null,
            CaseReasonCodeId = null,
            ResolvedAt = null,
            IsDeleted = false
        };

        await DatabaseHelper.CreateCases(testFixture.Services, [caseEntity]);

        return caseEntity.CaseId;
    }

    private async Task<CaseEntity> GetCaseAsync(Guid caseId)
    {
        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        return await context.Cases.FirstAsync(x => x.CaseId == caseId);
    }
}
