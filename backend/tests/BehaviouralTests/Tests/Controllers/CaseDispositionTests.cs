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
using Presentation.Models.Cases;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

namespace BehaviouralTests.Tests.Controllers;

public class CaseDispositionTests(TestFixture testFixture) : IClassFixture<TestFixture>, IAsyncLifetime
{
    private readonly Fixture _fixture = AutoFixtureHelper.Create();
    private readonly HttpClient _httpClient = testFixture.CreateClient();

    private const string Email = "disposition@mail.com";
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

    [Fact]
    public async Task ResolveCase_GivenADisposition_RecordsItApartFromTheStatus()
    {
        // Arrange
        var caseId = await CreateAssignedCaseAsync();

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/cases/{caseId}/resolve", new ResolveCaseRequestBodyDto
        {
            Status = nameof(CaseStatus.Approved),
            Disposition = nameof(CaseDisposition.TruePositive),
            Note = "Funds moved on within minutes, referred to the reporting team"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var caseEntity = await GetCaseAsync(caseId);

        caseEntity.StatusId.Should().Be((int)CaseStatus.Approved);
        caseEntity.DispositionId.Should().Be((int)CaseDisposition.TruePositive);
    }

    [Theory]
    [InlineData(nameof(CaseDisposition.TruePositive))]
    [InlineData(nameof(CaseDisposition.FalsePositive))]
    [InlineData(nameof(CaseDisposition.TrueNegative))]
    [InlineData(nameof(CaseDisposition.FalseNegative))]
    public async Task ResolveCase_GivenAnyDeclaredDisposition_RecordsIt(string disposition)
    {
        // Arrange
        var caseId = await CreateAssignedCaseAsync();

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/cases/{caseId}/resolve", new ResolveCaseRequestBodyDto
        {
            Status = nameof(CaseStatus.Approved),
            Disposition = disposition,
            Note = "Reviewed by the desk"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var caseEntity = await GetCaseAsync(caseId);

        caseEntity.DispositionId.Should().Be((int)Enum.Parse<CaseDisposition>(disposition));
    }

    [Fact]
    public async Task ResolveCase_MissWithoutANote_IsRefused()
    {
        // Arrange
        var caseId = await CreateAssignedCaseAsync();

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/cases/{caseId}/resolve", new ResolveCaseRequestBodyDto
        {
            Status = nameof(CaseStatus.Approved),
            Disposition = nameof(CaseDisposition.FalseNegative)
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ResolveCase_WithoutADisposition_IsRefused()
    {
        // Arrange
        var caseId = await CreateAssignedCaseAsync();

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/cases/{caseId}/resolve", new
        {
            status = nameof(CaseStatus.Approved)
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ResolveCase_RejectingWithoutANote_IsRefused()
    {
        // Arrange
        var caseId = await CreateAssignedCaseAsync();

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/cases/{caseId}/resolve", new ResolveCaseRequestBodyDto
        {
            Status = nameof(CaseStatus.Rejected),
            Disposition = nameof(CaseDisposition.FalsePositive)
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ResolveCase_TruePositiveWithoutANote_IsRefused()
    {
        // Arrange
        var caseId = await CreateAssignedCaseAsync();

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/cases/{caseId}/resolve", new ResolveCaseRequestBodyDto
        {
            Status = nameof(CaseStatus.Approved),
            Disposition = nameof(CaseDisposition.TruePositive)
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ResolveCase_GivenANote_WritesItAgainstTheCase()
    {
        // Arrange
        var caseId = await CreateAssignedCaseAsync();

        // Act
        await _httpClient.PostAsJsonAsync($"/cases/{caseId}/resolve", new ResolveCaseRequestBodyDto
        {
            Status = nameof(CaseStatus.Rejected),
            Disposition = nameof(CaseDisposition.TruePositive),
            Note = "Confirmed mule account"
        });

        // Assert
        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var caseNotes = await context.CaseNotes.Where(x => x.CaseId == caseId).ToListAsync();

        caseNotes.Should().ContainSingle(x => x.Message == "Confirmed mule account" && x.CreatedByUserId == _userId);
    }

    [Fact]
    public async Task ResolveCase_WhereTheOrganizationDeclaredATaxonomy_DemandsAReasonCode()
    {
        // Arrange
        await CreateReasonCodeAsync("MULE_ACCOUNT", disposition: null, isEnabled: true);

        var caseId = await CreateAssignedCaseAsync();

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/cases/{caseId}/resolve", new ResolveCaseRequestBodyDto
        {
            Status = nameof(CaseStatus.Approved),
            Disposition = nameof(CaseDisposition.FalsePositive)
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ResolveCase_GivenAReasonCodeBoundToTheOtherDisposition_IsRefused()
    {
        // Arrange
        var reasonCodeId = await CreateReasonCodeAsync("MULE_ACCOUNT", CaseDisposition.TruePositive, isEnabled: true);

        var caseId = await CreateAssignedCaseAsync();

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/cases/{caseId}/resolve", new ResolveCaseRequestBodyDto
        {
            Status = nameof(CaseStatus.Approved),
            Disposition = nameof(CaseDisposition.FalsePositive),
            ReasonCodeId = reasonCodeId
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ResolveCase_GivenARetiredReasonCode_IsRefused()
    {
        // Arrange
        var reasonCodeId = await CreateReasonCodeAsync("LEGACY_CODE", CaseDisposition.FalsePositive, isEnabled: false);
        await CreateReasonCodeAsync("CUSTOMER_CONFIRMED", CaseDisposition.FalsePositive, isEnabled: true);

        var caseId = await CreateAssignedCaseAsync();

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/cases/{caseId}/resolve", new ResolveCaseRequestBodyDto
        {
            Status = nameof(CaseStatus.Approved),
            Disposition = nameof(CaseDisposition.FalsePositive),
            ReasonCodeId = reasonCodeId
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ResolveCase_GivenAnApplicableReasonCode_RecordsItAgainstTheCase()
    {
        // Arrange
        var reasonCodeId = await CreateReasonCodeAsync("CUSTOMER_CONFIRMED", CaseDisposition.FalsePositive, isEnabled: true);

        var caseId = await CreateAssignedCaseAsync();

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/cases/{caseId}/resolve", new ResolveCaseRequestBodyDto
        {
            Status = nameof(CaseStatus.Approved),
            Disposition = nameof(CaseDisposition.FalsePositive),
            ReasonCodeId = reasonCodeId
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var caseEntity = await GetCaseAsync(caseId);

        caseEntity.CaseReasonCodeId.Should().Be(reasonCodeId);
    }

    [Fact]
    public async Task ResolveCase_GivenACodeApplyingToEitherDisposition_IsAccepted()
    {
        // Arrange
        var reasonCodeId = await CreateReasonCodeAsync("CUSTOMER_CONFIRMED", disposition: null, isEnabled: true);

        var caseId = await CreateAssignedCaseAsync();

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/cases/{caseId}/resolve", new ResolveCaseRequestBodyDto
        {
            Status = nameof(CaseStatus.Approved),
            Disposition = nameof(CaseDisposition.TruePositive),
            ReasonCodeId = reasonCodeId,
            Note = "Customer confirmed the transfer was coerced"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ResolveCase_WhereEveryEnabledCodeIsBoundToTheOtherDisposition_IsAccepted()
    {
        // Arrange
        await CreateReasonCodeAsync("CONFIRMED_FRAUD", CaseDisposition.TruePositive, isEnabled: true);

        var caseId = await CreateAssignedCaseAsync();

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/cases/{caseId}/resolve", new ResolveCaseRequestBodyDto
        {
            Status = nameof(CaseStatus.Approved),
            Disposition = nameof(CaseDisposition.FalsePositive)
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var caseEntity = await GetCaseAsync(caseId);

        caseEntity.DispositionId.Should().Be((int)CaseDisposition.FalsePositive);
        caseEntity.CaseReasonCodeId.Should().BeNull();
    }

    private async Task<Guid> CreateAssignedCaseAsync()
    {
        var caseEntity = FakeCaseEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            AssignedToUserId = _userId,
            AssignedToUser = null!,
            StatusId = (int)CaseStatus.Assigned,
            DispositionId = null,
            CaseReasonCodeId = null,
            ResolvedAt = null,
            IsDeleted = false
        };

        await DatabaseHelper.CreateCases(testFixture.Services, [caseEntity]);

        return caseEntity.CaseId;
    }

    private async Task<Guid> CreateReasonCodeAsync(string code, CaseDisposition? disposition, bool isEnabled)
    {
        var caseReasonCodeEntity = new CaseReasonCodeEntity
        {
            ModeId = (int)Mode.Live,
            CaseReasonCodeId = Guid.NewGuid(),
            OrganizationId = OrganizationId,
            Code = code,
            Description = _fixture.Create<string>(),
            DispositionId = disposition.HasValue ? (int)disposition.Value : null,
            IsEnabled = isEnabled,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            IsDeleted = false,
            DeletedAt = null
        };

        await DatabaseHelper.CreateCaseReasonCodes(testFixture.Services, [caseReasonCodeEntity]);

        return caseReasonCodeEntity.CaseReasonCodeId;
    }

    private async Task<CaseEntity> GetCaseAsync(Guid caseId)
    {
        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        return await context.Cases.FirstAsync(x => x.CaseId == caseId);
    }
}
