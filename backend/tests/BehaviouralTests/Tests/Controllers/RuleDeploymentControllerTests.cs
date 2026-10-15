using System.Net;
using System.Net.Http.Json;
using AutoFixture;
using BehaviouralTests.TestHelpers;
using Core.Constants;
using Core.Enums;
using Core.Jobs;
using FluentAssertions;
using Infrastructure;
using Infrastructure.Configurations;
using Infrastructure.Entities;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Presentation.Models.Common;
using Presentation.Models.Rules;
using Quartz;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.Rules;

namespace BehaviouralTests.Tests.Controllers;

public class RuleDeploymentControllerTests(TestFixture testFixture) : IClassFixture<TestFixture>, IAsyncLifetime
{
    private readonly Fixture _fixture = AutoFixtureHelper.Create();
    private readonly HttpClient _httpClient = testFixture.CreateClient();

    private const string Email = "username@mail.com";
    private const string Password = "P@$$w0rd";
    private static readonly Guid OrganizationId = new("4d6389e9-f5d1-4325-a004-915865e3adec");

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

        var createRequest = FakeCreateRuleRequestDto.CreateValid(_fixture) with
        {
            RuleSetId = ruleSetEntity.RuleSetId
        };
        var createResponse = await _httpClient.PostAsJsonAsync($"/rule-sets/{createRequest.RuleSetId}/rules", createRequest.Body);
        var createdRule = await createResponse.Content.ReadDataFromJsonAsync<RuleResourceDto>();

        var versionBody = FakeCreateRuleVersionRequestDto.CreateValid(_fixture).Body;
        var versionResponse = await _httpClient.PostAsJsonAsync($"/rules/{createdRule!.Rule.RuleId}/versions", versionBody);
        versionResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        return createdRule;
    }

    private async Task ExecuteRuleDeploymentJobAsync(Guid ruleDeploymentId)
    {
        using var scope = testFixture.Services.CreateScope();
        var job = ActivatorUtilities.CreateInstance<RuleDeploymentJob>(scope.ServiceProvider);

        var mockJobDetail = new Mock<IJobDetail>();
        mockJobDetail.SetupGet(x => x.Key).Returns(new JobKey(ruleDeploymentId.ToString(), QuartzConstants.RuleDeploymentGroup));

        var mockContext = new Mock<IJobExecutionContext>();
        mockContext.SetupGet(x => x.JobDetail).Returns(mockJobDetail.Object);
        mockContext.SetupGet(x => x.CancellationToken).Returns(CancellationToken.None);

        await job.Execute(mockContext.Object);
    }

    [Fact]
    public async Task DeployRule_AllAtOnce_UpdatesLiveRule()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var rule = await CreateRuleWithSecondVersionAsync();

        // Act
        var response = await _httpClient.PostAsJsonAsync(
            $"/rules/{rule.Rule.RuleId}/deployments",
            new { versionNumber = 2, strategy = nameof(DeploymentStrategy.AllAtOnce) });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var deployment = await response.Content.ReadDataFromJsonAsync<RuleDeploymentResourceDto>();
        deployment!.RuleDeployment.Status.Should().Be(nameof(RuleDeploymentStatus.Completed));
        deployment.RuleDeployment.Strategy.Should().Be(nameof(DeploymentStrategy.AllAtOnce));
        deployment.RuleDeployment.CurrentPercentage.Should().Be(100);

        var getResponse = await _httpClient.GetAsync($"/rules/{rule.Rule.RuleId}");
        var updatedRule = await getResponse.Content.ReadDataFromJsonAsync<RuleResourceDto>();

        updatedRule!.Rule.FriendlyName.Should().Be(rule.Rule.FriendlyName);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
        var ruleEntity = context.Rules.First(x => x.RuleId == rule.Rule.RuleId);
        ruleEntity.CurrentVersionNumber.Should().Be(2);
    }

    [Fact]
    public async Task DeployRule_Gradual_CreatesInProgressDeploymentAndLeavesLiveRuleUnchanged()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var rule = await CreateRuleWithSecondVersionAsync();

        // Act
        var response = await _httpClient.PostAsJsonAsync(
            $"/rules/{rule.Rule.RuleId}/deployments",
            new { versionNumber = 2, strategy = nameof(DeploymentStrategy.Gradual), stepPercentage = 20, stepIntervalSeconds = 3600 });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var deployment = await response.Content.ReadDataFromJsonAsync<RuleDeploymentResourceDto>();
        deployment!.RuleDeployment.Status.Should().Be(nameof(RuleDeploymentStatus.InProgress));
        deployment.RuleDeployment.Strategy.Should().Be(nameof(DeploymentStrategy.Gradual));
        deployment.RuleDeployment.CurrentPercentage.Should().Be(20);

        var getResponse = await _httpClient.GetAsync($"/rules/{rule.Rule.RuleId}");
        var liveRule = await getResponse.Content.ReadDataFromJsonAsync<RuleResourceDto>();
        liveRule!.Rule.FriendlyName.Should().Be(rule.Rule.FriendlyName);
    }

    [Fact]
    public async Task DeployRule_GradualStepsToHundred_CompletesDeploymentAndUpdatesLiveRule()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var rule = await CreateRuleWithSecondVersionAsync();

        var deployResponse = await _httpClient.PostAsJsonAsync(
            $"/rules/{rule.Rule.RuleId}/deployments",
            new { versionNumber = 2, strategy = nameof(DeploymentStrategy.Gradual), stepPercentage = 20, stepIntervalSeconds = 3600 });
        var deployment = await deployResponse.Content.ReadDataFromJsonAsync<RuleDeploymentResourceDto>();

        // Act
        for (var step = 0; step < 4; step++)
        {
            await ExecuteRuleDeploymentJobAsync(deployment!.RuleDeployment.RuleDeploymentId);
        }

        // Assert
        var listResponse = await _httpClient.GetAsync($"/rules/{rule.Rule.RuleId}/deployments");
        var deployments = await listResponse.Content.ReadFromJsonAsync<PagedResponseDto<RuleDeploymentResourceDto>>();
        deployments!.Data.Should().Contain(x =>
            x.RuleDeployment.RuleDeploymentId == deployment!.RuleDeployment.RuleDeploymentId &&
            x.RuleDeployment.Status == nameof(RuleDeploymentStatus.Completed) &&
            x.RuleDeployment.CurrentPercentage == 100);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
        var ruleEntity = context.Rules.First(x => x.RuleId == rule.Rule.RuleId);
        ruleEntity.CurrentVersionNumber.Should().Be(2);
        ruleEntity.HasActiveDeployment.Should().BeFalse();
    }

    [Fact]
    public async Task DeployRule_WhenDeploymentInProgress_ReturnsConflict()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var rule = await CreateRuleWithSecondVersionAsync();

        await _httpClient.PostAsJsonAsync(
            $"/rules/{rule.Rule.RuleId}/deployments",
            new { versionNumber = 2, strategy = nameof(DeploymentStrategy.Gradual), stepPercentage = 20, stepIntervalSeconds = 3600 });

        // Act
        var response = await _httpClient.PostAsJsonAsync(
            $"/rules/{rule.Rule.RuleId}/deployments",
            new { versionNumber = 2, strategy = nameof(DeploymentStrategy.AllAtOnce) });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CancelDeployment_InProgressGradual_MarksCancelled()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var rule = await CreateRuleWithSecondVersionAsync();

        var deployResponse = await _httpClient.PostAsJsonAsync(
            $"/rules/{rule.Rule.RuleId}/deployments",
            new { versionNumber = 2, strategy = nameof(DeploymentStrategy.Gradual), stepPercentage = 20, stepIntervalSeconds = 3600 });
        var deployment = await deployResponse.Content.ReadDataFromJsonAsync<RuleDeploymentResourceDto>();

        // Act
        var cancelResponse = await _httpClient.PostAsync($"/rules/{rule.Rule.RuleId}/deployments/{deployment!.RuleDeployment.RuleDeploymentId}/cancel", null);

        // Assert
        cancelResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var listResponse = await _httpClient.GetAsync($"/rules/{rule.Rule.RuleId}/deployments");
        var deployments = await listResponse.Content.ReadFromJsonAsync<PagedResponseDto<RuleDeploymentResourceDto>>();
        deployments!.Data.Should().Contain(x =>
            x.RuleDeployment.RuleDeploymentId == deployment.RuleDeployment.RuleDeploymentId &&
            x.RuleDeployment.Status == nameof(RuleDeploymentStatus.Cancelled));
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
