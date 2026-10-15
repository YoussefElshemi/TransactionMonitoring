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
using Presentation.Models.CaseReasonCodes;
using Presentation.Models.Common;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

namespace BehaviouralTests.Tests.Controllers;

public class OrganizationCaseReasonCodeControllerTests(TestFixture testFixture) : IClassFixture<TestFixture>, IAsyncLifetime
{
    private readonly Fixture _fixture = AutoFixtureHelper.Create();
    private readonly HttpClient _httpClient = testFixture.CreateClient();

    private const string Email = "taxonomy@mail.com";
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
    public async Task CreateCaseReasonCode_GivenAValidRequest_ReturnsCreated()
    {
        // Act
        var response = await _httpClient.PostAsJsonAsync($"/organizations/{OrganizationId}/reason-codes", new CreateCaseReasonCodeRequestBodyDto
        {
            Code = "MULE_ACCOUNT",
            Description = "Funds moved on within minutes of arriving",
            Disposition = nameof(CaseDisposition.TruePositive),
            IsEnabled = true
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await response.Content.ReadFromJsonAsync<ResponseDataDto<CaseReasonCodeResponseDto>>();

        created.Should().NotBeNull();
        created!.Data.Code.Should().Be("MULE_ACCOUNT");
        created.Data.Disposition.Should().Be(nameof(CaseDisposition.TruePositive));
    }

    [Fact]
    public async Task CreateCaseReasonCode_GivenACodeThatDiffersOnlyInCasing_IsRefused()
    {
        // Arrange
        await CreateReasonCodeAsync("MULE_ACCOUNT", CaseDisposition.TruePositive, isEnabled: true);

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/organizations/{OrganizationId}/reason-codes", new CreateCaseReasonCodeRequestBodyDto
        {
            Code = "mule_account",
            Description = _fixture.Create<string>(),
            Disposition = null,
            IsEnabled = true
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateCaseReasonCode_RetiringACodeInUse_IsAllowed()
    {
        // Arrange
        var reasonCodeId = await CreateReasonCodeAsync("MULE_ACCOUNT", CaseDisposition.TruePositive, isEnabled: true);
        await CreateClosedCaseAsync(reasonCodeId);

        // Act
        var response = await _httpClient.PatchAsJsonAsync($"/organizations/{OrganizationId}/reason-codes/{reasonCodeId}", new UpdateCaseReasonCodeRequestBodyDto
        {
            IsEnabled = false
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var caseReasonCodeEntity = await context.CaseReasonCodes.FirstAsync(x => x.CaseReasonCodeId == reasonCodeId);

        caseReasonCodeEntity.IsEnabled.Should().BeFalse();
        caseReasonCodeEntity.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteCaseReasonCode_GivenACodeACaseIsFiledUnder_IsRefused()
    {
        // Arrange
        var reasonCodeId = await CreateReasonCodeAsync("MULE_ACCOUNT", CaseDisposition.TruePositive, isEnabled: true);
        await CreateClosedCaseAsync(reasonCodeId);

        // Act
        var response = await _httpClient.DeleteAsync($"/organizations/{OrganizationId}/reason-codes/{reasonCodeId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteCaseReasonCode_GivenACodeNobodyHasUsed_IsAllowed()
    {
        // Arrange
        var reasonCodeId = await CreateReasonCodeAsync("TYPO_CODE", CaseDisposition.TruePositive, isEnabled: true);

        // Act
        var response = await _httpClient.DeleteAsync($"/organizations/{OrganizationId}/reason-codes/{reasonCodeId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task GetCaseReasonCodeUsages_ReportsWhatRetiringItWouldTouch()
    {
        // Arrange
        var reasonCodeId = await CreateReasonCodeAsync("MULE_ACCOUNT", CaseDisposition.TruePositive, isEnabled: true);
        await CreateClosedCaseAsync(reasonCodeId);
        await CreateClosedCaseAsync(reasonCodeId);

        // Act
        var response = await _httpClient.GetFromJsonAsync<ResponseDataDto<CaseReasonCodeUsageResponseDto>>(
            $"/organizations/{OrganizationId}/reason-codes/{reasonCodeId}/usages");

        // Assert
        response.Should().NotBeNull();
        response!.Data.Code.Should().Be("MULE_ACCOUNT");
        response.Data.CaseCount.Should().Be(2);
    }

    [Fact]
    public async Task ListCaseReasonCodes_FiltersByDisposition()
    {
        // Arrange
        await CreateReasonCodeAsync("MULE_ACCOUNT", CaseDisposition.TruePositive, isEnabled: true);
        await CreateReasonCodeAsync("KNOWN_CUSTOMER", CaseDisposition.FalsePositive, isEnabled: true);

        // Act
        var response = await _httpClient.GetFromJsonAsync<PagedResponseDto<CaseReasonCodeResourceDto>>(
            $"/organizations/{OrganizationId}/reason-codes?disposition={nameof(CaseDisposition.FalsePositive)}");

        // Assert
        response.Should().NotBeNull();
        response!.Data.Should().OnlyContain(x => x.CaseReasonCode.Disposition == nameof(CaseDisposition.FalsePositive));
        response.Data.Should().ContainSingle(x => x.CaseReasonCode.Code == "KNOWN_CUSTOMER");
    }

    [Fact]
    public async Task GetCaseReasonCodeHistory_AfterRetiring_ReturnsTheAuditTrail()
    {
        // Arrange
        var reasonCodeId = await CreateReasonCodeAsync("MULE_ACCOUNT", CaseDisposition.TruePositive, isEnabled: true);

        await _httpClient.PatchAsJsonAsync($"/organizations/{OrganizationId}/reason-codes/{reasonCodeId}", new UpdateCaseReasonCodeRequestBodyDto
        {
            IsEnabled = false
        });

        // Act
        var response = await _httpClient.GetAsync($"/organizations/{OrganizationId}/reason-codes/{reasonCodeId}/history");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateCaseReasonCode_ClearingTheDisposition_OffersItUnderBothAgain()
    {
        // Arrange
        var reasonCodeId = await CreateReasonCodeAsync("CUSTOMER_CONFIRMED", CaseDisposition.TruePositive, isEnabled: true);

        // Act
        var response = await _httpClient.PatchAsJsonAsync($"/organizations/{OrganizationId}/reason-codes/{reasonCodeId}", new UpdateCaseReasonCodeRequestBodyDto
        {
            ClearDisposition = true
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var caseReasonCodeEntity = await context.CaseReasonCodes.FirstAsync(x => x.CaseReasonCodeId == reasonCodeId);

        caseReasonCodeEntity.DispositionId.Should().BeNull();
    }

    [Fact]
    public async Task UpdateCaseReasonCode_WithoutTouchingTheDisposition_LeavesItAlone()
    {
        // Arrange
        var reasonCodeId = await CreateReasonCodeAsync("CUSTOMER_CONFIRMED", CaseDisposition.TruePositive, isEnabled: true);

        // Act
        var response = await _httpClient.PatchAsJsonAsync($"/organizations/{OrganizationId}/reason-codes/{reasonCodeId}", new UpdateCaseReasonCodeRequestBodyDto
        {
            IsEnabled = false
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var caseReasonCodeEntity = await context.CaseReasonCodes.FirstAsync(x => x.CaseReasonCodeId == reasonCodeId);

        caseReasonCodeEntity.DispositionId.Should().Be((int)CaseDisposition.TruePositive);
    }

    [Fact]
    public async Task UpdateCaseReasonCode_ClearingAndSettingTheDispositionTogether_IsRefused()
    {
        // Arrange
        var reasonCodeId = await CreateReasonCodeAsync("CUSTOMER_CONFIRMED", CaseDisposition.TruePositive, isEnabled: true);

        // Act
        var response = await _httpClient.PatchAsJsonAsync($"/organizations/{OrganizationId}/reason-codes/{reasonCodeId}", new UpdateCaseReasonCodeRequestBodyDto
        {
            Disposition = nameof(CaseDisposition.FalsePositive),
            ClearDisposition = true
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateCaseReasonCode_GivenACodeWhoseWildcardsWouldMatchAnother_IsAllowed()
    {
        // Arrange
        await CreateReasonCodeAsync("MULE_A", CaseDisposition.TruePositive, isEnabled: true);

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/organizations/{OrganizationId}/reason-codes", new CreateCaseReasonCodeRequestBodyDto
        {
            Code = "MULEXA",
            Description = "A different code entirely",
            Disposition = nameof(CaseDisposition.TruePositive),
            IsEnabled = true
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
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

    private async Task CreateClosedCaseAsync(Guid reasonCodeId)
    {
        var caseEntity = FakeCaseEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            AssignedToUserId = _userId,
            AssignedToUser = null!,
            StatusId = (int)CaseStatus.Approved,
            DispositionId = (int)CaseDisposition.TruePositive,
            CaseReasonCodeId = reasonCodeId,
            IsDeleted = false
        };

        await DatabaseHelper.CreateCases(testFixture.Services, [caseEntity]);
    }
}
