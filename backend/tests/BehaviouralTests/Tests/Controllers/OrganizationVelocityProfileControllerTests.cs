using System.Net;
using System.Net.Http.Json;
using AutoFixture;
using BehaviouralTests.TestHelpers;
using Core.Enums;
using FluentAssertions;
using Infrastructure;
using Infrastructure.Configurations;
using Infrastructure.Entities;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Constants;
using Presentation.Models.Common;
using Presentation.Models.TransactionSchemas;
using Presentation.Models.VelocityProfiles;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

namespace BehaviouralTests.Tests.Controllers;

public class OrganizationVelocityProfileControllerTests(TestFixture testFixture) : IClassFixture<TestFixture>, IAsyncLifetime
{
    private readonly Fixture _fixture = AutoFixtureHelper.Create();
    private readonly HttpClient _httpClient = testFixture.CreateClient();

    private const string Email = "username@mail.com";
    private const string Password = "P@$$w0rd";
    private static readonly Guid OrganizationId = new("6f2a1c30-1b8d-4d34-9d0e-2fdd3a6bd101");

    [Fact]
    public async Task SetVelocityProfile_NotAuthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/velocity-profile", ValidBody());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task SetVelocityProfile_NotAdmin_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticateAsync(UserRole.User);

        // Act
        var response = await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/velocity-profile", ValidBody());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SetVelocityProfile_GivenValidRequest_ShouldReturnOkAndPersistTheProfile()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Admin);

        // Act
        var response = await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/velocity-profile", ValidBody());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var resource = await response.Content.ReadDataFromJsonAsync<VelocityProfileResourceDto>();
        resource.Should().NotBeNull();
        resource!.VelocityProfile.Metrics.Should().HaveCount(2);
        resource.VelocityProfile.Thresholds.Should().ContainSingle();

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
        context.VelocityProfiles.Should().ContainSingle(x => x.OrganizationId == OrganizationId);
    }

    [Fact]
    public async Task SetVelocityProfile_GivenTheSameMetricTwice_ShouldReturnBadRequest()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Admin);
        var body = new SetVelocityProfileRequestBodyDto
        {
            Metrics =
            [
                new VelocityMetricDto { Name = "amount", Path = "amount" },
                new VelocityMetricDto { Name = "Amount", Path = "total" }
            ]
        };

        // Act
        var response = await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/velocity-profile", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SetVelocityProfile_GivenAMetricPathCarryingAWildcard_ShouldReturnBadRequest()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Admin);
        var body = new SetVelocityProfileRequestBodyDto
        {
            Metrics = [new VelocityMetricDto { Name = "beneficiaryIban", Path = "parties[*].iban" }]
        };

        // Act
        var response = await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/velocity-profile", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SetVelocityProfile_GivenAMetricTheSchemaDoesNotDeclare_ShouldReturnBadRequest()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Admin);
        await SetSchemaAsync();

        var body = new SetVelocityProfileRequestBodyDto
        {
            Metrics = [new VelocityMetricDto { Name = "fee", Path = "fee" }]
        };

        // Act
        var response = await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/velocity-profile", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SetVelocityProfile_GivenANewMetric_ShouldQueueABackfill()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Admin);

        // Act
        await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/velocity-profile", ValidBody());
        var response = await _httpClient.GetAsync($"/organizations/{OrganizationId}/velocity-profile/backfills");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var page = await response.Content.ReadFromJsonAsync<PagedResponseDto<VelocityBackfillResourceDto>>();
        page!.Data.Should().ContainSingle();
        page.Data[0].VelocityBackfill.Metrics.Should().BeEquivalentTo("amount", "beneficiaryAccount");
    }

    [Fact]
    public async Task SetVelocityProfile_GivenNoNewMetric_ShouldNotQueueASecondBackfill()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Admin);
        await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/velocity-profile", ValidBody());

        // Act
        await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/velocity-profile", ValidBody());
        var response = await _httpClient.GetAsync($"/organizations/{OrganizationId}/velocity-profile/backfills");

        // Assert
        var page = await response.Content.ReadFromJsonAsync<PagedResponseDto<VelocityBackfillResourceDto>>();
        page!.Data.Should().ContainSingle();
    }

    [Fact]
    public async Task GetVelocityProfile_GivenNoProfile_ShouldReturnNotFound()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Admin);

        // Act
        var response = await _httpClient.GetAsync($"/organizations/{OrganizationId}/velocity-profile");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteVelocityProfile_GivenNoRuleReadsTheStore_ShouldReturnNoContent()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Admin);
        await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/velocity-profile", ValidBody());

        // Act
        var response = await _httpClient.DeleteAsync($"/organizations/{OrganizationId}/velocity-profile");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
        context.VelocityProfiles.Should().ContainSingle(x => x.OrganizationId == OrganizationId && x.IsDeleted);
    }

    [Fact]
    public async Task SetVelocityProfile_AfterAWithdrawal_ShouldQueueASecondBackfill()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Admin);
        await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/velocity-profile", ValidBody());
        await _httpClient.DeleteAsync($"/organizations/{OrganizationId}/velocity-profile");

        // Act
        await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/velocity-profile", ValidBody());
        var response = await _httpClient.GetAsync($"/organizations/{OrganizationId}/velocity-profile/backfills");

        // Assert
        var page = await response.Content.ReadFromJsonAsync<PagedResponseDto<VelocityBackfillResourceDto>>();
        page!.Data.Should().HaveCount(2);
    }

    [Fact]
    public async Task ListVelocityProfileUsages_GivenNoRuleReadsTheStore_ShouldReturnAnEmptyList()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Admin);
        await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/velocity-profile", ValidBody());

        // Act
        var response = await _httpClient.GetAsync($"/organizations/{OrganizationId}/velocity-profile/usages");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var usages = await response.Content.ReadDataFromJsonAsync<VelocityProfileUsageResponseDto[]>();
        usages.Should().BeEmpty();
    }

    private async Task SetSchemaAsync()
    {
        var body = new SetTransactionSchemaRequestBodyDto
        {
            Fields =
            [
                new SchemaFieldDto { Name = "amount", Type = nameof(SchemaFieldType.Number), IsRequired = true },
                new SchemaFieldDto { Name = "beneficiaryAccount", Type = nameof(SchemaFieldType.String), IsRequired = false }
            ]
        };

        var response = await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/transaction-schema", body);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static SetVelocityProfileRequestBodyDto ValidBody()
    {
        return new SetVelocityProfileRequestBodyDto
        {
            Metrics =
            [
                new VelocityMetricDto { Name = "amount", Path = "amount" },
                new VelocityMetricDto { Name = "beneficiaryAccount", Path = "beneficiaryAccount" }
            ],
            Thresholds = [new VelocityThresholdDto { Name = "reporting", Value = 10000m }]
        };
    }

    private async Task AuthenticateAsync(params UserRole[] roles)
    {
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, .. roles], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
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
