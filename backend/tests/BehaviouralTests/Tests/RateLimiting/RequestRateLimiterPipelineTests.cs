using System.Net;
using System.Net.Http.Json;
using AutoFixture;
using BehaviouralTests.TestHelpers;
using Core.Enums;
using Core.ValueObjects;
using FluentAssertions;
using Infrastructure.Configurations;
using Infrastructure.Mappers;
using Presentation.Constants;
using UnitTests.TestHelpers.FakeObjects.Core.Models.ApiClients;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Users;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.RuleSets;

namespace BehaviouralTests.Tests.RateLimiting;

public class RequestRateLimiterPipelineTests(RateLimitedTestFixture testFixture)
    : IClassFixture<RateLimitedTestFixture>, IAsyncLifetime
{
    private readonly Fixture _fixture = AutoFixtureHelper.Create();
    private readonly HttpClient _httpClient = testFixture.CreateClient();

    private static readonly Guid OrganizationId = new("4d6389e9-f5d1-4325-a004-915865e3adec");

    [Fact]
    public async Task Evaluate_AuthenticatedAsALiveApiClient_ChargesTheClientAndOrganisationLiveBuckets()
    {
        // Arrange
        await AuthenticateAsApiClientAsync(Mode.Live);
        var evaluateAllRequest = FakeEvaluateAllRuleSetRequestDto.CreateValid(_fixture);

        // Act
        var response = await _httpClient.PostAsJsonAsync("/rule-sets/evaluate", evaluateAllRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        testFixture.Recorder.Keys.Should().HaveCount(2);
        testFixture.Recorder.Keys.Should().OnlyContain(x => x.Contains($"{{org:{OrganizationId}}}"));
        testFixture.Recorder.Keys.Should().Contain(x => x.Contains("client:"));
        testFixture.Recorder.Keys.Should().OnlyContain(x => x.EndsWith($":{Mode.Live}:{RateLimitPool.Evaluation}"));
    }

    [Fact]
    public async Task Evaluate_AuthenticatedAsATestApiClient_ChargesTheSandboxBucketsInstead()
    {
        // Arrange
        await AuthenticateAsApiClientAsync(Mode.Test);
        var evaluateAllRequest = FakeEvaluateAllRuleSetRequestDto.CreateValid(_fixture);

        // Act
        var response = await _httpClient.PostAsJsonAsync("/rule-sets/evaluate", evaluateAllRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        testFixture.Recorder.Keys.Should().HaveCount(2);
        testFixture.Recorder.Keys.Should().OnlyContain(x => x.EndsWith($":{Mode.Test}:{RateLimitPool.Evaluation}"));
        testFixture.Recorder.Keys.Should().NotContain(x => x.Contains($":{Mode.Live}:"));
    }

    // Anonymous endpoint on purpose: authorization runs before the limiter on protected ones.
    [Fact]
    public async Task TokenExchange_Unauthenticated_ChargesTheAnonymousBucket()
    {
        // Arrange
        var form = new Dictionary<string, string>
        {
            ["grant_type"] = OAuthConstants.ClientCredentialsGrantType,
            ["client_id"] = _fixture.Create<string>(),
            ["client_secret"] = _fixture.Create<string>()
        };

        // Act
        var response = await AuthenticationHelper.PostTokenFormAsync(_httpClient, form);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        testFixture.Recorder.Keys.Should().ContainSingle();
        testFixture.Recorder.Keys.Should().OnlyContain(x => x.Contains("anon:"));
    }

    private async Task AuthenticateAsApiClientAsync(Mode mode)
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
                ModeId = (int)mode,
                OrganizationId = OrganizationId,
                Organization = null!,
                IsEnabled = true,
                IsDeleted = false
            }
        ]);

        var createdApiClient = await AuthenticationHelper.CreateApiClientAsync(testFixture.Services,
            FakeCreateApiClientRequest.CreateValid(_fixture) with
            {
                OrganizationId = new OrganizationId(OrganizationId),
                UserId = new ApplicationUserId(applicationUserEntity.Id)
            }, mode);

        await AuthenticationHelper.SetClientCredentialsTokenAsync(
            _httpClient, createdApiClient.ApiClient.ClientId, createdApiClient.ClientSecret);

        // The token exchange is itself rate limited, so clear the recorder after authenticating.
        testFixture.Recorder.Clear();
    }

    public async Task InitializeAsync()
    {
        await testFixture.ResetDatabaseAsync();
        testFixture.Recorder.Clear();

        await DatabaseHelper.CreateOrganizations(testFixture.Services, [
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
