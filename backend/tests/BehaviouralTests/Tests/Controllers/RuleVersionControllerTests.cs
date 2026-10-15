using System.Net;
using System.Net.Http.Json;
using AutoFixture;
using BehaviouralTests.TestHelpers;
using Core.Constants;
using Core.Enums;
using FluentAssertions;
using Infrastructure;
using Infrastructure.Configurations;
using Infrastructure.Entities;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Constants;
using Presentation.Models.Common;
using Presentation.Models.Rules;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.Rules;

namespace BehaviouralTests.Tests.Controllers;

public class RuleVersionControllerTests(TestFixture testFixture) : IClassFixture<TestFixture>, IAsyncLifetime
{
    private readonly Fixture _fixture = AutoFixtureHelper.Create();
    private readonly HttpClient _httpClient = testFixture.CreateClient();

    private const string Email = "username@mail.com";
    private const string Password = "P@$$w0rd";
    private static readonly Guid OrganizationId = new("4d6389e9-f5d1-4325-a004-915865e3adec");
    private static readonly Guid DisabledOrganizationId = new("03a9b48e-4af2-40bd-a180-cac58bd930f3");

    [Fact]
    public async Task CreateRuleVersion_NotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var versionRequest = FakeCreateRuleVersionRequestDto.CreateValid(_fixture);

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/rules/{versionRequest.RuleId}/versions", versionRequest.Body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task CreateRuleVersion_NotManager_ShouldReturnForbidden()
    {
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var versionRequest = FakeCreateRuleVersionRequestDto.CreateValid(_fixture);

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/rules/{versionRequest.RuleId}/versions", versionRequest.Body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task CreateRuleVersion_DoesNotBelongToOrganization_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], SystemConstants.DefaultOrganization);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var versionRequest = FakeCreateRuleVersionRequestDto.CreateValid(_fixture);

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/rules/{versionRequest.RuleId}/versions", versionRequest.Body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task CreateRuleVersion_BelongsToDisabledOrganization_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], DisabledOrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var versionRequest = FakeCreateRuleVersionRequestDto.CreateValid(_fixture);

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/rules/{versionRequest.RuleId}/versions", versionRequest.Body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task CreateRuleVersion_GivenInvalidRequest_ShouldReturnBadRequest()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var ruleSetEntity = FakeRuleSetEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            IsDeleted = false,
            Rules =
            [
                FakeRuleEntity.CreateValid(_fixture) with
                {
                    OrganizationId = OrganizationId,
                    Organization = null!,
                    IsEnabled = true,
                    IsDeleted = false
                }
            ]
        };

        await DatabaseHelper.CreateRuleSets(testFixture.Services, [ruleSetEntity]);

        var versionRequest = FakeCreateRuleVersionRequestDto.CreateInvalid(_fixture) with
        {
            RuleId = ruleSetEntity.Rules.First().RuleId,
            OrganizationId = OrganizationId
        };

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/rules/{versionRequest.RuleId}/versions", versionRequest.Body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task CreateRuleVersion_GivenValidRequest_ShouldReturnCreated()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var ruleSetEntity = FakeRuleSetEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            IsDeleted = false,
            Rules =
            [
                FakeRuleEntity.CreateValid(_fixture) with
                {
                    OrganizationId = OrganizationId,
                    Organization = null!,
                    IsEnabled = true,
                    IsDeleted = false
                }
            ]
        };

        await DatabaseHelper.CreateRuleSets(testFixture.Services, [ruleSetEntity]);

        var versionRequest = FakeCreateRuleVersionRequestDto.CreateValid(_fixture) with
        {
            RuleId = ruleSetEntity.Rules.First().RuleId,
            OrganizationId = OrganizationId
        };

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/rules/{versionRequest.RuleId}/versions", versionRequest.Body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);

        var seededRule = ruleSetEntity.Rules.First();

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var newVersion = context.RuleVersions
            .Where(x => x.RuleId == versionRequest.RuleId)
            .OrderByDescending(x => x.VersionNumber)
            .FirstOrDefault();
        newVersion.Should().NotBeNull();
        newVersion!.RuleBody.Should().NotBeNullOrEmpty();
        newVersion.VersionNumber.Should().Be(seededRule.CurrentVersionNumber + 1);

        var ruleEntity = context.Rules.FirstOrDefault(x => x.RuleId == versionRequest.RuleId);
        ruleEntity.Should().NotBeNull();
        ruleEntity!.CurrentRuleVersionId.Should().Be(seededRule.CurrentRuleVersionId);
        ruleEntity.CurrentVersionNumber.Should().Be(seededRule.CurrentVersionNumber);
    }

    [Fact]
    public async Task ListRuleVersions_ReturnsAllVersions()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var ruleId = await CreateRuleWithSecondVersionAsync();

        // Act
        var response = await _httpClient.GetAsync($"/rules/{ruleId}/versions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var versions = await response.Content.ReadFromJsonAsync<PagedResponseDto<RuleVersionResponseDto>>();
        versions!.Data.Should().HaveCount(2);
        versions.Data.Should().Contain(x => x.VersionNumber == 1);
        versions.Data.Should().Contain(x => x.VersionNumber == 2);
    }

    [Fact]
    public async Task GetRuleVersion_ReturnsRequestedVersion()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var ruleId = await CreateRuleWithSecondVersionAsync();

        // Act
        var response = await _httpClient.GetAsync($"/rules/{ruleId}/versions/2");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var version = await response.Content.ReadDataFromJsonAsync<RuleVersionResponseDto>();
        version!.RuleId.Should().Be(ruleId);
        version.VersionNumber.Should().Be(2);
    }

    private async Task<Guid> CreateRuleWithSecondVersionAsync()
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

        var versionRequest = FakeCreateRuleVersionRequestDto.CreateValid(_fixture);
        await _httpClient.PostAsJsonAsync($"/rules/{createdRule!.Rule.RuleId}/versions", versionRequest.Body);

        return createdRule.Rule.RuleId;
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
            },
            FakeOrganizationEntity.CreateValid(_fixture) with
            {
                OrganizationId = DisabledOrganizationId,
                StatusId = (int)OrganizationStatus.Disabled,
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
