using System.Net;
using System.Net.Http.Json;
using AutoFixture;
using BehaviouralTests.TestHelpers;
using Core.Constants;
using Core.Enums;
using Core.ValueObjects;
using FluentAssertions;
using Infrastructure.Configurations;
using Infrastructure.Entities;
using Infrastructure.Mappers;
using Presentation.Constants;
using Presentation.Models.BackTests;
using Presentation.Models.Common;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Users;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.BackTests;

namespace BehaviouralTests.Tests.Controllers;

public class BackTestControllerTests(TestFixture testFixture) : IClassFixture<TestFixture>, IAsyncLifetime
{
    private readonly Fixture _fixture = AutoFixtureHelper.Create();
    private readonly HttpClient _httpClient = testFixture.CreateClient();

    private const string Email = "username@mail.com";
    private const string Password = "P@$$w0rd";
    private static readonly Guid OrganizationId = new("4d6389e9-f5d1-4325-a004-915865e3adec");
    private static readonly Guid DisabledOrganizationId = new("03a9b48e-4af2-40bd-a180-cac58bd930f3");

    [Fact]
    public async Task CreateBackTest_NotAuthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _httpClient.PostAsync($"/rules/{_fixture.Create<Guid>()}/backTests", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task CreateBackTest_NotManager_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.PostAsync($"/rules/{_fixture.Create<Guid>()}/backTests", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task CreateBackTest_DoesNotBelongToOrganization_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], SystemConstants.DefaultOrganization);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.PostAsync($"/rules/{_fixture.Create<Guid>()}/backTests", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task CreateBackTest_BelongsToDisabledOrganization_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], DisabledOrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.PostAsync($"/rules/{_fixture.Create<Guid>()}/backTests", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task CreateBackTest_RuleNotFound_ShouldReturnNotFound()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.PostAsync($"/rules/{_fixture.Create<Guid>()}/backTests", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task CreateBackTest_GivenValidRequest_ShouldReturnCreated()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        var ruleSetEntity = FakeRuleSetEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            IsEnabled = true,
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

        // Act
        var response = await _httpClient.PostAsync($"/rules/{ruleSetEntity.Rules.First().RuleId}/backTests", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);

        var backTest = await response.Content.ReadDataFromJsonAsync<BackTestResourceDto>();
        backTest.Should().NotBeNull();
        backTest!.BackTest.BackTestId.Should().NotBeEmpty();
        backTest.BackTest.Status.Should().Be(nameof(BackTestStatus.Pending));
        backTest.BackTest.SampleSize.Should().Be(100);
    }

    [Fact]
    public async Task CreateBackTest_GivenSampleSize_ShouldPersistSampleSize()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        var ruleSetEntity = FakeRuleSetEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            IsEnabled = true,
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

        // Act
        var response = await _httpClient.PostAsJsonAsync(
            $"/rules/{ruleSetEntity.Rules.First().RuleId}/backTests",
            FakeCreateBackTestRequestBodyDto.CreateValid(_fixture) with
            {
                SampleSize = 25
            });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var backTest = await response.Content.ReadDataFromJsonAsync<BackTestResourceDto>();
        backTest.Should().NotBeNull();
        backTest!.BackTest.SampleSize.Should().Be(25);
    }

    [Fact]
    public async Task CreateBackTest_GivenSampleSizeOutOfRange_ShouldReturnBadRequest()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        var ruleSetEntity = FakeRuleSetEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            IsEnabled = true,
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

        // Act
        var response = await _httpClient.PostAsJsonAsync(
            $"/rules/{ruleSetEntity.Rules.First().RuleId}/backTests",
            FakeCreateBackTestRequestBodyDto.CreateValid(_fixture) with
            {
                SampleSize = 125
            });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ListBackTests_NotAuthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _httpClient.GetAsync("/backTests");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task ListBackTests_DoesNotBelongToOrganization_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], SystemConstants.DefaultOrganization);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.GetAsync("/backTests");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task ListBackTests_BelongsToDisabledOrganization_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], DisabledOrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.GetAsync("/backTests");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task ListBackTests_GivenValidRequest_ShouldReturnOk()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.GetAsync("/backTests");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);

        var result = await response.Content.ReadFromJsonAsync<PagedResponseDto<BackTestResourceDto>>();
        result.Should().NotBeNull();
        result!.Metadata.Should().NotBeNull();
    }

    [Fact]
    public async Task GetBackTest_NotAuthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _httpClient.GetAsync($"/backTests/{_fixture.Create<Guid>()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task GetBackTest_DoesNotBelongToOrganization_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], SystemConstants.DefaultOrganization);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.GetAsync($"/backTests/{_fixture.Create<Guid>()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task GetBackTest_BelongsToDisabledOrganization_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], DisabledOrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.GetAsync($"/backTests/{_fixture.Create<Guid>()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task GetBackTest_NotFound_ShouldReturnNotFound()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.GetAsync($"/backTests/{_fixture.Create<Guid>()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task GetBackTest_GivenValidRequest_ShouldReturnOk()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        var appUserEntity = (FakeApplicationUser.CreateValid(_fixture) with
        {
            OrganizationId = new OrganizationId(OrganizationId),
            IsDeleted = new IsDeleted(false)
        }).ToEntity();
        await DatabaseHelper.CreateApplicationUsers(testFixture.Services, [appUserEntity]);

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

        var backTestEntity = new BackTestEntity
        {
            ModeId = (int)Mode.Live,
            BackTestId = _fixture.Create<Guid>(),
            OrganizationId = OrganizationId,
            RuleId = ruleSetEntity.Rules.First().RuleId,
            RuleVersionId = ruleSetEntity.Rules.First().CurrentRuleVersionId,
            StatusId = (int)BackTestStatus.Completed,
            SampleSize = 100,
            TotalEvaluations = 100,
            ProcessedCount = 100,
            CreatedByUserId = appUserEntity.Id,
            CompletedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        await DatabaseHelper.CreateBackTests(testFixture.Services, [backTestEntity]);

        // Act
        var response = await _httpClient.GetAsync($"/backTests/{backTestEntity.BackTestId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);

        var result = await response.Content.ReadDataFromJsonAsync<BackTestResourceDto>();
        result.Should().NotBeNull();
        result!.BackTest.BackTestId.Should().Be(backTestEntity.BackTestId);
        result.BackTest.SampleSize.Should().Be(100);
        result.BackTest.TotalEvaluations.Should().Be(100);
    }

    [Fact]
    public async Task ListBackTestResults_NotAuthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _httpClient.GetAsync($"/backTests/{_fixture.Create<Guid>()}/results");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task ListBackTestResults_DoesNotBelongToOrganization_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], SystemConstants.DefaultOrganization);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.GetAsync($"/backTests/{_fixture.Create<Guid>()}/results");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task ListBackTestResults_BelongsToDisabledOrganization_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], DisabledOrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.GetAsync($"/backTests/{_fixture.Create<Guid>()}/results");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task ListBackTestResults_NotFound_ShouldReturnNotFound()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.GetAsync($"/backTests/{_fixture.Create<Guid>()}/results");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task ListBackTestResults_GivenValidRequest_ShouldReturnOk()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        var appUserEntity = (FakeApplicationUser.CreateValid(_fixture) with
        {
            OrganizationId = new OrganizationId(OrganizationId),
            IsDeleted = new IsDeleted(false)
        }).ToEntity();
        await DatabaseHelper.CreateApplicationUsers(testFixture.Services, [appUserEntity]);

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

        var backTestEntity = new BackTestEntity
        {
            ModeId = (int)Mode.Live,
            BackTestId = _fixture.Create<Guid>(),
            OrganizationId = OrganizationId,
            RuleId = ruleSetEntity.Rules.First().RuleId,
            RuleVersionId = ruleSetEntity.Rules.First().CurrentRuleVersionId,
            StatusId = (int)BackTestStatus.Completed,
            SampleSize = 100,
            TotalEvaluations = 50,
            ProcessedCount = 50,
            CreatedByUserId = appUserEntity.Id,
            CompletedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        await DatabaseHelper.CreateBackTests(testFixture.Services, [backTestEntity]);

        // Act
        var response = await _httpClient.GetAsync($"/backTests/{backTestEntity.BackTestId}/results");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);

        var result = await response.Content.ReadFromJsonAsync<PagedResponseDto<BackTestResultResponseDto>>();
        result.Should().NotBeNull();
        result!.Metadata.Should().NotBeNull();
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
