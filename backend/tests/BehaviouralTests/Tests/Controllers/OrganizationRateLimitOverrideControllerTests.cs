using System.Net;
using System.Net.Http.Json;
using AutoFixture;
using BehaviouralTests.TestHelpers;
using Core.Enums;
using FluentAssertions;
using Infrastructure;
using Infrastructure.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Models.Common;
using Presentation.Models.RateLimitOverrides;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

namespace BehaviouralTests.Tests.Controllers;

public class OrganizationRateLimitOverrideControllerTests(TestFixture testFixture) : IClassFixture<TestFixture>, IAsyncLifetime
{
    private readonly Fixture _fixture = AutoFixtureHelper.Create();
    private readonly HttpClient _httpClient = testFixture.CreateClient();

    private const string AdminEmail = "platform@mail.com";
    private const string ManagerEmail = "desk@mail.com";
    private const string Password = "P@$$w0rd";
    private static readonly Guid OrganizationId = new("2f1c7c3e-9a54-4b21-9d10-6c5b1f2a8e44");

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

        await AuthenticationHelper.CreateUserAsync(testFixture.Services, AdminEmail, Password, [UserRole.Admin], OrganizationId);
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, ManagerEmail, Password, [UserRole.Manager], OrganizationId);

        await AuthenticationHelper.SetTokenAsync(_httpClient, AdminEmail, Password);
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task CreateRateLimitOverride_GivenAValidRequest_ReturnsCreated()
    {
        // Act
        var response = await _httpClient.PostAsJsonAsync($"/organizations/{OrganizationId}/rate-limit-overrides", Body());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await response.Content.ReadFromJsonAsync<ResponseDataDto<RateLimitOverrideResponseDto>>();

        created.Should().NotBeNull();
        created!.Data.PermitLimit.Should().Be(10);
        created.Data.Pool.Should().Be(nameof(RateLimitPool.Evaluation));
        created.Data.Reason.Should().Be("Incident 4021 - retry loop from a single client");
    }

    [Fact]
    public async Task CreateRateLimitOverride_IsWrittenToTheAuditTrail()
    {
        // Act
        var response = await _httpClient.PostAsJsonAsync($"/organizations/{OrganizationId}/rate-limit-overrides", Body());
        var created = await response.Content.ReadFromJsonAsync<ResponseDataDto<RateLimitOverrideResponseDto>>();

        // Assert
        using var assertScope = testFixture.Services.CreateScope();
        var assertContext = assertScope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var auditLogs = await assertContext.AuditLogs
            .Where(x => x.EntityTypeId == (int)AuditableEntityType.RateLimitOverride &&
                        x.EntityId == created!.Data.RateLimitOverrideId &&
                        x.OperationId == (int)AuditOperation.Created)
            .CountAsync();

        auditLogs.Should().Be(1);
    }

    [Fact]
    public async Task CreateRateLimitOverride_WithoutAReason_IsRefused()
    {
        // Act
        var response = await _httpClient.PostAsJsonAsync(
            $"/organizations/{OrganizationId}/rate-limit-overrides", Body() with { Reason = "  " });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateRateLimitOverride_ExpiringInThePast_IsRefused()
    {
        // Act
        var response = await _httpClient.PostAsJsonAsync(
            $"/organizations/{OrganizationId}/rate-limit-overrides",
            Body() with { ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1) });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateRateLimitOverride_RunningLongerThanTheMaximum_IsRefused()
    {
        // Act
        var response = await _httpClient.PostAsJsonAsync(
            $"/organizations/{OrganizationId}/rate-limit-overrides",
            Body() with { ExpiresAt = DateTimeOffset.UtcNow.AddDays(365) });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateRateLimitOverride_AsSomebodyWithoutThePlatformPermission_IsRefused()
    {
        // Arrange
        await AuthenticationHelper.SetTokenAsync(_httpClient, ManagerEmail, Password);

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/organizations/{OrganizationId}/rate-limit-overrides", Body());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ListRateLimitOverrides_ByDefault_ExcludesExpiredOnes()
    {
        // Arrange
        var overrideId = await CreateOverrideAsync();
        await ExpireAsync(overrideId);

        // Act
        var response = await _httpClient.GetAsync($"/organizations/{OrganizationId}/rate-limit-overrides");

        // Assert
        var listed = await response.Content.ReadFromJsonAsync<PagedResponseDto<RateLimitOverrideResponseDto>>();

        listed.Should().NotBeNull();
        listed!.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task ListRateLimitOverrides_AskingForExpired_ReturnsThem()
    {
        // Arrange
        var overrideId = await CreateOverrideAsync();
        await ExpireAsync(overrideId);

        // Act
        var response = await _httpClient.GetAsync($"/organizations/{OrganizationId}/rate-limit-overrides?includeExpired=true");

        // Assert
        var listed = await response.Content.ReadFromJsonAsync<PagedResponseDto<RateLimitOverrideResponseDto>>();

        listed.Should().NotBeNull();
        listed!.Data.Should().ContainSingle(x => x.RateLimitOverrideId == overrideId);
    }

    [Fact]
    public async Task DeleteRateLimitOverride_LiftingAClamp_IsAuditedToo()
    {
        // Arrange
        var overrideId = await CreateOverrideAsync();

        // Act
        var response = await _httpClient.DeleteAsync($"/organizations/{OrganizationId}/rate-limit-overrides/{overrideId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var assertScope = testFixture.Services.CreateScope();
        var assertContext = assertScope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var lifted = await assertContext.RateLimitOverrides.FirstAsync(x => x.RateLimitOverrideId == overrideId);
        lifted.IsDeleted.Should().BeTrue();

        var auditLogs = await assertContext.AuditLogs
            .Where(x => x.EntityTypeId == (int)AuditableEntityType.RateLimitOverride &&
                        x.EntityId == overrideId &&
                        x.OperationId == (int)AuditOperation.Deleted)
            .CountAsync();

        auditLogs.Should().Be(1);
    }

    [Fact]
    public async Task DeleteRateLimitOverride_BelongingToAnotherOrganisation_ReturnsNotFound()
    {
        // Arrange
        var overrideId = await CreateOverrideAsync();

        // Act
        var response = await _httpClient.DeleteAsync(
            $"/organizations/{OrganizationConfigurations.DefaultOrganization.OrganizationId}/rate-limit-overrides/{overrideId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static CreateRateLimitOverrideRequestBodyDto Body()
    {
        return new CreateRateLimitOverrideRequestBodyDto
        {
            Pool = nameof(RateLimitPool.Evaluation),
            PermitLimit = 10,
            TokensPerPeriod = 10,
            ReplenishmentPeriodSeconds = 60,
            Reason = "Incident 4021 - retry loop from a single client",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(2)
        };
    }

    private async Task<Guid> CreateOverrideAsync()
    {
        var response = await _httpClient.PostAsJsonAsync($"/organizations/{OrganizationId}/rate-limit-overrides", Body());
        var created = await response.Content.ReadFromJsonAsync<ResponseDataDto<RateLimitOverrideResponseDto>>();

        return created!.Data.RateLimitOverrideId;
    }

    private async Task ExpireAsync(Guid rateLimitOverrideId)
    {
        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var entity = await context.RateLimitOverrides.FirstAsync(x => x.RateLimitOverrideId == rateLimitOverrideId);
        var expired = entity with { ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1) };

        context.RateLimitOverrides.Update(expired);
        await context.SaveChangesAsync();
    }
}
