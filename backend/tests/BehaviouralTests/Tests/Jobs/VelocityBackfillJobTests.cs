using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using AutoFixture;
using BehaviouralTests.TestHelpers;
using Core.Enums;
using Core.Jobs;
using Core.Models.VelocityProfiles;
using Core.ValueObjects;
using FluentAssertions;
using Infrastructure;
using Infrastructure.Configurations;
using Infrastructure.Entities;
using Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Presentation.Models.PartyMappings;
using Quartz;
using UnitTests.TestHelpers.FakeObjects.Core.Models.ApiClients;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Users;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

namespace BehaviouralTests.Tests.Jobs;

public class VelocityBackfillJobTests(TestFixture testFixture) : IClassFixture<TestFixture>, IAsyncLifetime
{
    private readonly Fixture _fixture = AutoFixtureHelper.Create();
    private readonly HttpClient _httpClient = testFixture.CreateClient();
    private readonly HttpClient _ingestClient = testFixture.CreateClient();

    private const string Email = "username@mail.com";
    private const string Password = "P@$$w0rd";
    private static readonly Guid OrganizationId = new("0c37cd18-6d1b-4a4c-8c2c-9a4b3f7a55f1");

    [Fact]
    public async Task Execute_GivenPaymentsTakenBeforeTheMetricWasDeclared_WritesTheObservationsTheyWouldHaveWritten()
    {
        // Arrange
        await ArrangeAsync();

        await EvaluateAsync(4000);
        await EvaluateAsync(6000);

        await AssertNoObservationsAsync();

        await SetProfileAsync();

        // Act
        await RunBackfillAsync();

        // Assert
        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        context.VelocityObservations.Count(x => x.OrganizationId == OrganizationId).Should().Be(8);
    }

    [Fact]
    public async Task Execute_ShouldRecordEachObservationAgainstThePaymentsOwnClock()
    {
        // Arrange
        await ArrangeAsync();
        await EvaluateAsync(4000);
        await SetProfileAsync();

        // Act
        await RunBackfillAsync();

        // Assert
        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var transaction = await context.Transactions.SingleAsync(x => x.OrganizationId == OrganizationId);
        var observations = context.VelocityObservations.Where(x => x.TransactionId == transaction.TransactionId).ToList();

        observations.Should().NotBeEmpty();
        observations.Should().OnlyContain(x => x.ObservedAt == transaction.CreatedAt);
    }

    [Fact]
    public async Task Execute_ShouldMarkTheBackfillCompletedAndCountWhatItWalked()
    {
        // Arrange
        await ArrangeAsync();
        await EvaluateAsync(4000);
        await SetProfileAsync();

        // Act
        await RunBackfillAsync();

        // Assert
        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var backfill = await context.VelocityBackfills.SingleAsync(x => x.OrganizationId == OrganizationId);
        backfill.StatusId.Should().Be((int)VelocityBackfillStatus.Completed);
        backfill.TotalTransactions.Should().Be(1);
        backfill.ProcessedCount.Should().Be(1);
        backfill.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Execute_RunTwice_ShouldNotDoubleTheObservations()
    {
        // Arrange
        await ArrangeAsync();
        await EvaluateAsync(4000);
        await SetProfileAsync();

        var backfillId = await BackfillIdAsync();
        await RunBackfillAsync(backfillId);

        await ReopenAsync(backfillId);

        // Act
        await RunBackfillAsync(backfillId);

        // Assert
        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        context.VelocityObservations.Count(x => x.OrganizationId == OrganizationId).Should().Be(4);
    }

    private async Task AssertNoObservationsAsync()
    {
        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        (await context.VelocityObservations.ToListAsync()).Should().BeEmpty();
    }

    private async Task ReopenAsync(Guid backfillId)
    {
        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var backfill = await context.VelocityBackfills.SingleAsync(x => x.VelocityBackfillId == backfillId);
        backfill.StatusId = (int)VelocityBackfillStatus.Pending;
        backfill.ProcessedCount = 0;

        context.VelocityBackfills.Update(backfill);
        await context.SaveChangesAsync();
    }

    private async Task<Guid> BackfillIdAsync()
    {
        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var backfill = await context.VelocityBackfills.SingleAsync(x => x.OrganizationId == OrganizationId);

        return backfill.VelocityBackfillId;
    }

    private async Task RunBackfillAsync(Guid? velocityBackfillId = null)
    {
        var backfillId = velocityBackfillId ?? await BackfillIdAsync();

        using var scope = testFixture.Services.CreateScope();
        var job = ActivatorUtilities.CreateInstance<VelocityBackfillJob>(scope.ServiceProvider);

        await job.Execute(CreateContext(backfillId));
    }

    private static IJobExecutionContext CreateContext(Guid velocityBackfillId)
    {
        var mockJobDetail = new Mock<IJobDetail>();
        mockJobDetail.SetupGet(x => x.Key).Returns(new JobKey(velocityBackfillId.ToString(), "VelocityBackfills"));

        var mockContext = new Mock<IJobExecutionContext>();
        mockContext.SetupGet(x => x.JobDetail).Returns(mockJobDetail.Object);
        mockContext.SetupGet(x => x.CancellationToken).Returns(CancellationToken.None);

        return mockContext.Object;
    }

    private Task<HttpResponseMessage> EvaluateAsync(int amount)
    {
        return _ingestClient.PostAsJsonAsync("/rule-sets/evaluate", Payload(amount));
    }

    private static JsonObject Payload(int amount)
    {
        return JsonNode.Parse(
            $$"""
              {
                "amount": {{amount}},
                "beneficiaryAccount": "IBAN-1",
                "sender": { "accountId": "ACC-1", "name": "Ada" },
                "parties": [ { "iban": "IBAN-1", "name": "Grace" } ]
              }
              """)!.AsObject();
    }

    private async Task ArrangeAsync()
    {
        await AuthenticateAsync(UserRole.Manager, UserRole.Admin);
        await ArrangeIngestAsync();
        await SetMappingAsync();
    }

    private async Task SetMappingAsync()
    {
        var body = new SetPartyMappingRequestBodyDto
        {
            Mappings =
            [
                new PartyRoleMappingDto { Role = nameof(PartyRole.Remitter), Path = "sender", IdentifierPath = "accountId" },
                new PartyRoleMappingDto { Role = nameof(PartyRole.Beneficiary), Path = "parties[*]", IdentifierPath = "iban" }
            ]
        };

        var response = await _httpClient.PutAsJsonAsync("/organizations/my/party-mapping", body);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // Seeded directly: declaring through the API schedules the real backfill job alongside the one under test.
    private async Task SetProfileAsync()
    {
        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var user = await context.Users.FirstAsync(x => x.OrganizationId == OrganizationId);
        var now = DateTimeOffset.UtcNow;

        var metrics = new List<VelocityMetric>
        {
            new() { Name = new VelocityMetricName("amount"), Path = new VelocityMetricPath("amount") },
            new() { Name = new VelocityMetricName("beneficiaryAccount"), Path = new VelocityMetricPath("beneficiaryAccount") }
        };

        context.VelocityProfiles.Add(new VelocityProfileEntity
        {
            ModeId = (int)Mode.Live,
            VelocityProfileId = Guid.NewGuid(),
            OrganizationId = OrganizationId,
            Metrics = metrics.ToBase64String(),
            Thresholds = new List<VelocityThreshold>().ToBase64String(),
            CreatedByUserId = user.Id,
            CreatedAt = now,
            UpdatedAt = now,
            IsDeleted = false,
            DeletedAt = null
        });

        context.VelocityBackfills.Add(new VelocityBackfillEntity
        {
            ModeId = (int)Mode.Live,
            VelocityBackfillId = Guid.NewGuid(),
            OrganizationId = OrganizationId,
            Metrics = metrics.ConvertAll(x => x.Name).ToBase64String(),
            StatusId = (int)VelocityBackfillStatus.Pending,
            TotalTransactions = 0,
            ProcessedCount = 0,
            CompletedAt = null,
            CreatedAt = now,
            UpdatedAt = now
        });

        await context.SaveChangesAsync();
    }

    private async Task ArrangeIngestAsync()
    {
        var applicationUserEntity = (FakeApplicationUser.CreateValid(_fixture) with
        {
            OrganizationId = new OrganizationId(OrganizationId),
            IsDeleted = new IsDeleted(false)
        }).ToEntity();

        await DatabaseHelper.CreateApplicationUsers(testFixture.Services, [applicationUserEntity]);

        await DatabaseHelper.CreateRuleSets(testFixture.Services, [
            FakeRuleSetEntity.CreateValid(_fixture) with
            {
                OrganizationId = OrganizationId,
                Organization = null!,
                IsEnabled = true,
                IsDeleted = false,
                Rules = []
            }
        ]);

        var createApiClientRequest = FakeCreateApiClientRequest.CreateValid(_fixture) with
        {
            OrganizationId = new OrganizationId(OrganizationId),
            UserId = new ApplicationUserId(applicationUserEntity.Id)
        };

        var createdApiClient = await AuthenticationHelper.CreateApiClientAsync(testFixture.Services, createApiClientRequest);
        await AuthenticationHelper.SetClientCredentialsTokenAsync(_ingestClient, createdApiClient.ApiClient.ClientId, createdApiClient.ClientSecret);
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
