using System.Net;
using System.Net.Http.Json;
using AutoFixture;
using BehaviouralTests.TestHelpers;
using Core.Constants;
using Core.Enums;
using Core.Interfaces.Services;
using Core.Models.ApiClients;
using Core.Models.Rules;
using Core.ValueObjects;
using FluentAssertions;
using Infrastructure;
using Infrastructure.Configurations;
using Infrastructure.Entities;
using Infrastructure.Mappers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Constants;
using Presentation.Models.Common;
using Presentation.Models.Rules;
using UnitTests.TestHelpers.FakeObjects.Core.Models.ApiClients;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Users;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.Rules;

namespace BehaviouralTests.Tests.Controllers;

public class RuleSetRuleControllerTests(TestFixture testFixture) : IClassFixture<TestFixture>, IAsyncLifetime
{
    private readonly Fixture _fixture = AutoFixtureHelper.Create();
    private readonly HttpClient _httpClient = testFixture.CreateClient();

    private const string Email = "username@mail.com";
    private const string Password = "P@$$w0rd";
    private static readonly Guid OrganizationId = new("4d6389e9-f5d1-4325-a004-915865e3adec");
    private static readonly Guid DisabledOrganizationId = new("03a9b48e-4af2-40bd-a180-cac58bd930f3");

    [Fact]
    public async Task CreateRule_NotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var createRequest = FakeCreateRuleRequestDto.CreateValid(_fixture);

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/rule-sets/{_fixture.Create<Guid>()}/rules", createRequest.Body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task CreateRule_NotManager_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var createRequest = FakeCreateRuleRequestDto.CreateValid(_fixture);

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/rule-sets/{createRequest.RuleSetId}/rules", createRequest.Body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task CreateRule_DoesNotBelongToOrganization_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], SystemConstants.DefaultOrganization);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var createRequest = FakeCreateRuleRequestDto.CreateValid(_fixture);

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/rule-sets/{createRequest.RuleSetId}/rules", createRequest.Body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task CreateRule_BelongsToDisabledOrganization_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], DisabledOrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var ruleSetEntity = FakeRuleSetEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            IsDeleted = false
        };

        await DatabaseHelper.CreateRuleSets(testFixture.Services, [ruleSetEntity]);
        var createRequest = FakeCreateRuleRequestDto.CreateValid(_fixture) with
        {
            RuleSetId = ruleSetEntity.RuleSetId
        };

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/rule-sets/{createRequest.RuleSetId}/rules", createRequest.Body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task CreateRule_GivenInvalidRequest_ShouldReturnBadRequest()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var ruleSetEntity = FakeRuleSetEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            IsEnabled = true,
            IsDeleted = false
        };

        await DatabaseHelper.CreateRuleSets(testFixture.Services, [ruleSetEntity]);

        var createRequest = FakeCreateRuleRequestDto.CreateInvalid(_fixture) with
        {
            RuleSetId = ruleSetEntity.RuleSetId
        };

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/rule-sets/{createRequest.RuleSetId}/rules", createRequest.Body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task CreateRule_GivenValidRequest_ShouldReturnCreated()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var ruleSetEntity = FakeRuleSetEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            IsEnabled = true,
            IsDeleted = false
        };

        await DatabaseHelper.CreateRuleSets(testFixture.Services, [ruleSetEntity]);
        var createRequest = FakeCreateRuleRequestDto.CreateValid(_fixture) with
        {
            RuleSetId = ruleSetEntity.RuleSetId
        };

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/rule-sets/{createRequest.RuleSetId}/rules", createRequest.Body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);

        var createdRuleSet = await response.Content.ReadDataFromJsonAsync<RuleResourceDto>();
        createdRuleSet.Should().NotBeNull();
        createdRuleSet!.Rule.FriendlyName.Should().Be(createRequest.Body.FriendlyName);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
        var ruleEntity = context.Rules.FirstOrDefault(x => x.RuleSetId == createdRuleSet.Rule.RuleSetId);
        ruleEntity.Should().NotBeNull();
    }

    [Fact]
    public async Task ListRules_NotAuthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _httpClient.GetAsync($"/rule-sets/{_fixture.Create<Guid>()}/rules");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task ListRules_DoesNotBelongToOrganization_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], SystemConstants.DefaultOrganization);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.GetAsync($"/rule-sets/{_fixture.Create<Guid>()}/rules");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task ListRules_BelongsToDisabledOrganization_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], DisabledOrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.GetAsync($"/rule-sets/{_fixture.Create<Guid>()}/rules");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task ListRules_GivenInvalidRequest_ShouldReturnBadRequest()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var ruleSetEntity = FakeRuleSetEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            IsDeleted = false,
            Rules = Enumerable.Range(0, 50).Select(
                _ => FakeRuleEntity.CreateValid(_fixture) with
                {
                    OrganizationId = OrganizationId,
                    Organization = null!,
                    IsEnabled = true,
                    IsDeleted = false
                }).ToList()
        };

        await DatabaseHelper.CreateRuleSets(testFixture.Services, [ruleSetEntity]);

        // Act
        var response = await _httpClient.GetAsync($"/rule-sets/{ruleSetEntity.RuleSetId}/rules?PageSize=200");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task ListRules_GivenValidRequest_ShouldReturnOk()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var ruleSetEntity = FakeRuleSetEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            IsDeleted = false,
            Rules = Enumerable.Range(0, 50).Select(
                _ => FakeRuleEntity.CreateValid(_fixture) with
                {
                    OrganizationId = OrganizationId,
                    Organization = null!,
                    IsEnabled = true,
                    IsDeleted = false
                }).ToList()
        };

        await DatabaseHelper.CreateRuleSets(testFixture.Services, [ruleSetEntity]);

        // Act
        var response = await _httpClient.GetAsync($"/rule-sets/{ruleSetEntity.RuleSetId}/rules");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);

        var ruleSets = await response.Content.ReadFromJsonAsync<PagedResponseDto<RuleResourceDto>>();
        ruleSets.Should().NotBeNull();
        ruleSets!.Metadata.Should().NotBeNull();
        ruleSets.Metadata.PageCount.Should().Be(5);
        ruleSets.Metadata.PageNumber.Should().Be(1);
        ruleSets.Metadata.PageSize.Should().Be(10);
        ruleSets.Metadata.TotalCount.Should().Be(50);
        ruleSets.Metadata.HasMore.Should().BeTrue();
    }

    [Fact]
    public async Task GetRule_NotAuthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _httpClient.GetAsync($"/rules/{_fixture.Create<Guid>()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task GetRule_DoesNotBelongToOrganization_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], SystemConstants.DefaultOrganization);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.GetAsync($"/rules/{_fixture.Create<Guid>()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task GetRule_BelongsToDisabledOrganization_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], DisabledOrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.GetAsync($"/rules/{_fixture.Create<Guid>()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task GetRule_GivenValidRequest_ShouldReturnOk()
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

        // Act
        var response = await _httpClient.GetAsync($"/rules/{ruleSetEntity.Rules.First().RuleId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);

        var rule = await response.Content.ReadDataFromJsonAsync<RuleResourceDto>();
        rule.Should().NotBeNull();
        rule!.Rule.RuleSetId.Should().Be(ruleSetEntity.RuleSetId);
        rule.Rule.RuleId.Should().Be(ruleSetEntity.Rules.First().RuleId);
        rule.Rule.FriendlyName.Should().Be(ruleSetEntity.Rules.First().FriendlyName);
    }

    [Fact]
    public async Task DeleteRule_NotAuthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _httpClient.DeleteAsync($"/rules/{_fixture.Create<Guid>()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task DeleteRule_NotManager_ShouldReturnForbidden()
    {
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.DeleteAsync($"/rules/{_fixture.Create<Guid>()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task DeleteRule_DoesNotBelongToOrganization_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], SystemConstants.DefaultOrganization);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.DeleteAsync($"/rules/{_fixture.Create<Guid>()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task DeleteRule_BelongsToDisabledOrganization_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], DisabledOrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.DeleteAsync($"/rules/{_fixture.Create<Guid>()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task DeleteRule_GivenValidRequest_ShouldReturnNoContent()
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

        // Act
        var response = await _httpClient.DeleteAsync($"/rules/{ruleSetEntity.Rules.First().RuleId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
        var updatedRuleEntity = context.Rules.FirstOrDefault(x => x.RuleSetId == ruleSetEntity.RuleSetId);
        updatedRuleEntity.Should().NotBeNull();
        updatedRuleEntity!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task RecoverRule_NotAuthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _httpClient.PostAsync($"/rules/{_fixture.Create<Guid>()}/recover", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task RecoverRule_NotManager_ShouldReturnForbidden()
    {
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.PostAsync($"/rules/{_fixture.Create<Guid>()}/recover", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task RecoverRule_DoesNotBelongToOrganization_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], SystemConstants.DefaultOrganization);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.PostAsync($"/rules/{_fixture.Create<Guid>()}/recover", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task RecoverRule_BelongsToDisabledOrganization_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], DisabledOrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.PostAsync($"/rules/{_fixture.Create<Guid>()}/recover", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task RecoverRule_GivenValidRequest_ShouldReturnNoContent()
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
                    IsDeleted = true
                }
            ]
        };

        await DatabaseHelper.CreateRuleSets(testFixture.Services, [ruleSetEntity]);

        // Act
        var response = await _httpClient.PostAsync($"/rules/{ruleSetEntity.Rules.First().RuleId}/recover", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
        var updatedRuleSetEntity = context.RuleSets.FirstOrDefault(x => x.RuleSetId == ruleSetEntity.RuleSetId);
        updatedRuleSetEntity.Should().NotBeNull();
        updatedRuleSetEntity!.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task EvaluateRule_NotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var evaluateRequest = FakeEvaluateRuleRequestDto.CreateValid(_fixture);

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/rules/{evaluateRequest.RuleId}/evaluate", evaluateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task EvaluateRule_AuthenticatedAsUser_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var evaluateRequest = FakeEvaluateRuleRequestDto.CreateValid(_fixture);

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/rules/{evaluateRequest.RuleId}/evaluate", evaluateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task EvaluateRule_DoesNotBelongToOrganization_ShouldReturnForbidden()
    {
        // Arrange
        var applicationUserEntity = (FakeApplicationUser.CreateValid(_fixture) with
        {
            OrganizationId = new OrganizationId(SystemConstants.DefaultOrganization),
            IsDeleted = new IsDeleted(false)
        }).ToEntity();

        await DatabaseHelper.CreateApplicationUsers(testFixture.Services, [applicationUserEntity]);
        var createApiClientRequest = FakeCreateApiClientRequest.CreateValid(_fixture) with
        {
            OrganizationId = new OrganizationId(SystemConstants.DefaultOrganization),
            UserId = new ApplicationUserId(applicationUserEntity.Id)
        };

        var ruleSetEntity = FakeRuleSetEntity.CreateValid(_fixture) with
        {
            OrganizationId = SystemConstants.DefaultOrganization,
            Organization = null!,
            IsEnabled = true,
            IsDeleted = false
        };

        await DatabaseHelper.CreateRuleSets(testFixture.Services, [ruleSetEntity]);

        var createdApiClient = await AuthenticationHelper.CreateApiClientAsync(testFixture.Services, createApiClientRequest);
        await AuthenticationHelper.SetClientCredentialsTokenAsync(_httpClient, createdApiClient.ApiClient.ClientId, createdApiClient.ClientSecret);
        var evaluateRequest = FakeEvaluateRuleRequestDto.CreateValid(_fixture);

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/rules/{evaluateRequest.RuleId}/evaluate", evaluateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task EvaluateRule_BelongsToDisabledOrganization_ShouldReturnForbidden()
    {
        // Arrange
        var applicationUserEntity = (FakeApplicationUser.CreateValid(_fixture) with
        {
            OrganizationId = new OrganizationId(DisabledOrganizationId),
            IsDeleted = new IsDeleted(false)
        }).ToEntity();

        await DatabaseHelper.CreateApplicationUsers(testFixture.Services, [applicationUserEntity]);
        var createApiClientRequest = FakeCreateApiClientRequest.CreateValid(_fixture) with
        {
            OrganizationId = new OrganizationId(DisabledOrganizationId),
            UserId = new ApplicationUserId(applicationUserEntity.Id)
        };

        var ruleSetEntity = FakeRuleSetEntity.CreateValid(_fixture) with
        {
            OrganizationId = DisabledOrganizationId,
            Organization = null!,
            IsEnabled = true,
            IsDeleted = false
        };

        await DatabaseHelper.CreateRuleSets(testFixture.Services, [ruleSetEntity]);

        var createdApiClient = await AuthenticationHelper.CreateApiClientAsync(testFixture.Services, createApiClientRequest);
        await AuthenticationHelper.SetClientCredentialsTokenAsync(_httpClient, createdApiClient.ApiClient.ClientId, createdApiClient.ClientSecret);
        var evaluateRequest = FakeEvaluateRuleRequestDto.CreateValid(_fixture);

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/rules/{evaluateRequest.RuleId}/evaluate", evaluateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task EvaluateRule_GivenInvalidRequest_ShouldReturnBadRequest()
    {
        // Arrange
        var applicationUserEntity = (FakeApplicationUser.CreateValid(_fixture) with
        {
            OrganizationId = new OrganizationId(OrganizationId),
            IsDeleted = new IsDeleted(false)
        }).ToEntity();

        await DatabaseHelper.CreateApplicationUsers(testFixture.Services, [applicationUserEntity]);
        var createApiClientRequest = FakeCreateApiClientRequest.CreateValid(_fixture) with
        {
            OrganizationId = new OrganizationId(OrganizationId),
            UserId = new ApplicationUserId(applicationUserEntity.Id)
        };

        var ruleSetEntity = FakeRuleSetEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            IsEnabled = false,
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

        var createdApiClient = await AuthenticationHelper.CreateApiClientAsync(testFixture.Services, createApiClientRequest);
        await AuthenticationHelper.SetClientCredentialsTokenAsync(_httpClient, createdApiClient.ApiClient.ClientId, createdApiClient.ClientSecret);
        var evaluateRequest = FakeEvaluateRuleRequestDto.CreateValid(_fixture) with
        {
            RuleId = ruleSetEntity.Rules.First().RuleId
        };

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/rules/{evaluateRequest.RuleId}/evaluate", evaluateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task EvaluateRule_GivenValidRequest_ShouldReturnOk()
    {
        // Arrange
        var applicationUserEntity = (FakeApplicationUser.CreateValid(_fixture) with
        {
            OrganizationId = new OrganizationId(OrganizationId),
            IsDeleted = new IsDeleted(false)
        }).ToEntity();

        await DatabaseHelper.CreateApplicationUsers(testFixture.Services, [applicationUserEntity]);
        var createApiClientRequest = FakeCreateApiClientRequest.CreateValid(_fixture) with
        {
            OrganizationId = new OrganizationId(OrganizationId),
            UserId = new ApplicationUserId(applicationUserEntity.Id)
        };

        var ruleEntity = FakeRuleEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            IsEnabled = true,
            IsDeleted = false
        };

        var ruleVersionEntity = FakeRuleVersionEntity.CreateValid(_fixture) with
        {
            RuleVersionId = ruleEntity.CurrentRuleVersionId,
            RuleId = ruleEntity.RuleId
        };

        var ruleSetEntity = FakeRuleSetEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            IsEnabled = true,
            IsDeleted = false,
            Rules =
            [
                ruleEntity with
                {
                    RuleVersions = [ruleVersionEntity]
                }
            ]
        };

        await DatabaseHelper.CreateRuleSets(testFixture.Services, [ruleSetEntity]);

        var createdApiClient = await AuthenticationHelper.CreateApiClientAsync(testFixture.Services, createApiClientRequest);
        await AuthenticationHelper.SetClientCredentialsTokenAsync(_httpClient, createdApiClient.ApiClient.ClientId, createdApiClient.ClientSecret);
        var evaluateRequest = FakeEvaluateRuleRequestDto.CreateValid(_fixture) with
        {
            RuleId = ruleSetEntity.Rules.First().RuleId,
            CreateCase = true
        };

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/rules/{evaluateRequest.RuleId}/evaluate", evaluateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
        var usageEntity = await context.Usages.CountAsync();
        usageEntity.Should().Be(1);
    }

    [Fact]
    public async Task EvaluateRule_TokenDoesNotCarryTheRulesEvaluateScope_ShouldReturnForbidden()
    {
        // Arrange
        var applicationUserEntity = (FakeApplicationUser.CreateValid(_fixture) with
        {
            OrganizationId = new OrganizationId(OrganizationId),
            IsDeleted = new IsDeleted(false)
        }).ToEntity();

        await DatabaseHelper.CreateApplicationUsers(testFixture.Services, [applicationUserEntity]);
        var createApiClientRequest = FakeCreateApiClientRequest.CreateValid(_fixture) with
        {
            OrganizationId = new OrganizationId(OrganizationId),
            UserId = new ApplicationUserId(applicationUserEntity.Id),
            Scopes = [ApiScope.RuleSetsEvaluate]
        };

        var createdApiClient = await AuthenticationHelper.CreateApiClientAsync(testFixture.Services, createApiClientRequest);
        await AuthenticationHelper.SetClientCredentialsTokenAsync(_httpClient, createdApiClient.ApiClient.ClientId, createdApiClient.ClientSecret);
        var evaluateRequest = FakeEvaluateRuleRequestDto.CreateValid(_fixture);

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/rules/{evaluateRequest.RuleId}/evaluate", evaluateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task EvaluateRule_ClientDisabledAfterTheTokenWasIssued_ShouldReturnUnauthorized()
    {
        // Arrange
        var applicationUserEntity = (FakeApplicationUser.CreateValid(_fixture) with
        {
            OrganizationId = new OrganizationId(OrganizationId),
            IsDeleted = new IsDeleted(false)
        }).ToEntity();

        await DatabaseHelper.CreateApplicationUsers(testFixture.Services, [applicationUserEntity]);
        var createApiClientRequest = FakeCreateApiClientRequest.CreateValid(_fixture) with
        {
            OrganizationId = new OrganizationId(OrganizationId),
            UserId = new ApplicationUserId(applicationUserEntity.Id)
        };

        var createdApiClient = await AuthenticationHelper.CreateApiClientAsync(testFixture.Services, createApiClientRequest);
        await AuthenticationHelper.SetClientCredentialsTokenAsync(_httpClient, createdApiClient.ApiClient.ClientId, createdApiClient.ClientSecret);

        using (var scope = testFixture.Services.CreateScope())
        {
            var apiClientService = scope.ServiceProvider.GetRequiredService<IApiClientService>();

            await apiClientService.UpdateAsync(new UpdateApiClientRequest
            {
                ApiClientId = createdApiClient.ApiClient.ApiClientId,
                FriendlyName = null,
                IsEnabled = new IsEnabled(false),
                Scopes = null,
                ApiVersion = null
            }, CancellationToken.None);
        }

        var evaluateRequest = FakeEvaluateRuleRequestDto.CreateValid(_fixture);

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/rules/{evaluateRequest.RuleId}/evaluate", evaluateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task EvaluateRule_ClientDeletedAfterTheTokenWasIssued_ShouldReturnUnauthorized()
    {
        // Arrange
        var applicationUserEntity = (FakeApplicationUser.CreateValid(_fixture) with
        {
            OrganizationId = new OrganizationId(OrganizationId),
            IsDeleted = new IsDeleted(false)
        }).ToEntity();

        await DatabaseHelper.CreateApplicationUsers(testFixture.Services, [applicationUserEntity]);
        var createApiClientRequest = FakeCreateApiClientRequest.CreateValid(_fixture) with
        {
            OrganizationId = new OrganizationId(OrganizationId),
            UserId = new ApplicationUserId(applicationUserEntity.Id)
        };

        var createdApiClient = await AuthenticationHelper.CreateApiClientAsync(testFixture.Services, createApiClientRequest);
        await AuthenticationHelper.SetClientCredentialsTokenAsync(_httpClient, createdApiClient.ApiClient.ClientId, createdApiClient.ClientSecret);

        using (var scope = testFixture.Services.CreateScope())
        {
            var apiClientService = scope.ServiceProvider.GetRequiredService<IApiClientService>();
            await apiClientService.DeleteAsync(createdApiClient.ApiClient.ApiClientId, CancellationToken.None);
        }

        var evaluateRequest = FakeEvaluateRuleRequestDto.CreateValid(_fixture);

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/rules/{evaluateRequest.RuleId}/evaluate", evaluateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task EvaluateRule_RepeatedWithTheSameIdempotencyKey_ShouldReplayTheOriginalResponse()
    {
        // Arrange
        var applicationUserEntity = (FakeApplicationUser.CreateValid(_fixture) with
        {
            OrganizationId = new OrganizationId(OrganizationId),
            IsDeleted = new IsDeleted(false)
        }).ToEntity();

        await DatabaseHelper.CreateApplicationUsers(testFixture.Services, [applicationUserEntity]);

        var createApiClientRequest = FakeCreateApiClientRequest.CreateValid(_fixture) with
        {
            OrganizationId = new OrganizationId(OrganizationId),
            UserId = new ApplicationUserId(applicationUserEntity.Id)
        };

        var ruleEntity = FakeRuleEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            IsEnabled = true,
            IsDeleted = false,
            HasActiveDeployment = false
        };

        var ruleVersionEntity = FakeRuleVersionEntity.CreateValid(_fixture) with
        {
            RuleVersionId = ruleEntity.CurrentRuleVersionId,
            RuleId = ruleEntity.RuleId,
            RuleBody = ruleEntity.RuleBody
        };

        var ruleSetEntity = FakeRuleSetEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            IsEnabled = true,
            IsDeleted = false,
            Rules = [ruleEntity with { RuleVersions = [ruleVersionEntity] }]
        };

        await DatabaseHelper.CreateRuleSets(testFixture.Services, [ruleSetEntity]);

        var createdApiClient = await AuthenticationHelper.CreateApiClientAsync(testFixture.Services, createApiClientRequest);
        await AuthenticationHelper.SetClientCredentialsTokenAsync(_httpClient, createdApiClient.ApiClient.ClientId, createdApiClient.ClientSecret);

        var evaluateRequest = FakeEvaluateRuleRequestDto.CreateValid(_fixture) with
        {
            RuleId = ruleSetEntity.Rules.First().RuleId
        };

        _httpClient.DefaultRequestHeaders.Add(HeaderNames.IdempotencyKey, Guid.NewGuid().ToString());

        // Act
        var first = await _httpClient.PostAsJsonAsync($"/rules/{evaluateRequest.RuleId}/evaluate", evaluateRequest.Payload);
        var second = await _httpClient.PostAsJsonAsync($"/rules/{evaluateRequest.RuleId}/evaluate", evaluateRequest.Payload);

        // Assert
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        first.Headers.Should().NotContainKey(HeaderNames.IdempotentReplay);

        second.StatusCode.Should().Be(HttpStatusCode.OK);
        second.Headers.Should().ContainKey(HeaderNames.IdempotentReplay);
        (await second.Content.ReadAsStringAsync()).Should().Be(await first.Content.ReadAsStringAsync());

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
        (await context.Usages.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task EvaluateRule_ReusingAnIdempotencyKeyOnADifferentRule_ShouldReturnConflict()
    {
        // Arrange
        var applicationUserEntity = (FakeApplicationUser.CreateValid(_fixture) with
        {
            OrganizationId = new OrganizationId(OrganizationId),
            IsDeleted = new IsDeleted(false)
        }).ToEntity();

        await DatabaseHelper.CreateApplicationUsers(testFixture.Services, [applicationUserEntity]);

        var createApiClientRequest = FakeCreateApiClientRequest.CreateValid(_fixture) with
        {
            OrganizationId = new OrganizationId(OrganizationId),
            UserId = new ApplicationUserId(applicationUserEntity.Id)
        };

        var ruleEntities = Enumerable.Range(0, 2).Select(_ =>
        {
            var ruleEntity = FakeRuleEntity.CreateValid(_fixture) with
            {
                OrganizationId = OrganizationId,
                Organization = null!,
                IsEnabled = true,
                IsDeleted = false,
                HasActiveDeployment = false
            };

            return ruleEntity with
            {
                RuleVersions =
                [
                    FakeRuleVersionEntity.CreateValid(_fixture) with
                    {
                        RuleVersionId = ruleEntity.CurrentRuleVersionId,
                        RuleId = ruleEntity.RuleId,
                        RuleBody = ruleEntity.RuleBody
                    }
                ]
            };
        }).ToList();

        var ruleSetEntity = FakeRuleSetEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            IsEnabled = true,
            IsDeleted = false,
            Rules = ruleEntities
        };

        await DatabaseHelper.CreateRuleSets(testFixture.Services, [ruleSetEntity]);

        var createdApiClient = await AuthenticationHelper.CreateApiClientAsync(testFixture.Services, createApiClientRequest);
        await AuthenticationHelper.SetClientCredentialsTokenAsync(_httpClient, createdApiClient.ApiClient.ClientId, createdApiClient.ClientSecret);

        var payload = FakeEvaluateRuleRequestDto.CreateValid(_fixture).Payload;

        _httpClient.DefaultRequestHeaders.Add(HeaderNames.IdempotencyKey, Guid.NewGuid().ToString());

        // Act
        var first = await _httpClient.PostAsJsonAsync($"/rules/{ruleEntities[0].RuleId}/evaluate", payload);
        var second = await _httpClient.PostAsJsonAsync($"/rules/{ruleEntities[1].RuleId}/evaluate", payload);

        // Assert
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var problem = await second.Content.ReadFromJsonAsync<ProblemDetails>();
        problem!.Title.Should().Be($"{HeaderNames.IdempotencyKey} already used");
    }

    [Fact]
    public async Task EvaluateRule_WithoutTheRulesEvaluateScope_ShouldReturnForbidden()
    {
        // Arrange
        var applicationUserEntity = (FakeApplicationUser.CreateValid(_fixture) with
        {
            OrganizationId = new OrganizationId(OrganizationId),
            IsDeleted = new IsDeleted(false)
        }).ToEntity();

        await DatabaseHelper.CreateApplicationUsers(testFixture.Services, [applicationUserEntity]);

        var createApiClientRequest = FakeCreateApiClientRequest.CreateValid(_fixture) with
        {
            OrganizationId = new OrganizationId(OrganizationId),
            UserId = new ApplicationUserId(applicationUserEntity.Id),
            Scopes = [ApiScope.RuleSetsEvaluate]
        };

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

        var createdApiClient = await AuthenticationHelper.CreateApiClientAsync(testFixture.Services, createApiClientRequest);
        await AuthenticationHelper.SetClientCredentialsTokenAsync(_httpClient, createdApiClient.ApiClient.ClientId, createdApiClient.ClientSecret);

        var evaluateRequest = FakeEvaluateRuleRequestDto.CreateValid(_fixture) with
        {
            RuleId = ruleSetEntity.Rules.First().RuleId
        };

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/rules/{evaluateRequest.RuleId}/evaluate", evaluateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task EvaluateRule_RequestedByAnApiClient_ShouldAttributeAuditRowsToTheClient()
    {
        // Arrange
        var applicationUserEntity = (FakeApplicationUser.CreateValid(_fixture) with
        {
            OrganizationId = new OrganizationId(OrganizationId),
            IsDeleted = new IsDeleted(false)
        }).ToEntity();

        await DatabaseHelper.CreateApplicationUsers(testFixture.Services, [applicationUserEntity]);

        var createApiClientRequest = FakeCreateApiClientRequest.CreateValid(_fixture) with
        {
            OrganizationId = new OrganizationId(OrganizationId),
            UserId = new ApplicationUserId(applicationUserEntity.Id)
        };

        var matchingRuleBody = new RuleBody
        {
            Condition = RuleCondition.EqualTo,
            Field = new RuleField("Key"),
            Value = new RuleValue("Value"),
            And = [],
            Or = [],
            Not = []
        };

        var ruleEntity = FakeRuleEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            RuleBody = matchingRuleBody.ToBase64String(),
            IsEnabled = true,
            IsDeleted = false,
            HasActiveDeployment = false
        };

        var ruleVersionEntity = FakeRuleVersionEntity.CreateValid(_fixture) with
        {
            RuleVersionId = ruleEntity.CurrentRuleVersionId,
            RuleId = ruleEntity.RuleId,
            RuleBody = ruleEntity.RuleBody
        };

        var ruleSetEntity = FakeRuleSetEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            IsEnabled = true,
            IsDeleted = false,
            Rules = [ruleEntity with { RuleVersions = [ruleVersionEntity] }]
        };

        await DatabaseHelper.CreateRuleSets(testFixture.Services, [ruleSetEntity]);

        var createdApiClient = await AuthenticationHelper.CreateApiClientAsync(testFixture.Services, createApiClientRequest);
        await AuthenticationHelper.SetClientCredentialsTokenAsync(_httpClient, createdApiClient.ApiClient.ClientId, createdApiClient.ClientSecret);

        var evaluateRequest = FakeEvaluateRuleRequestDto.CreateValid(_fixture) with
        {
            RuleId = ruleSetEntity.Rules.First().RuleId
        };

        _httpClient.DefaultRequestHeaders.Add(HeaderNames.CreateCase, "true");

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/rules/{evaluateRequest.RuleId}/evaluate", evaluateRequest.Payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var caseAuditLogs = await context.AuditLogs
            .Where(x => x.EntityTypeId == (int)AuditableEntityType.Case)
            .ToListAsync();

        caseAuditLogs.Should().NotBeEmpty();
        caseAuditLogs.Should().OnlyContain(x => x.ChangedByApiClientId == (Guid)createdApiClient.ApiClient.ApiClientId);
        caseAuditLogs.Should().OnlyContain(x => x.ChangedByUserId == null);
    }

    [Fact]
    public async Task TransferRule_NotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var transferRequest = FakeTransferRuleRequestDto.CreateValid(_fixture);

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/rules/{transferRequest.RuleId}/transfer", transferRequest.Body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task TransferRule_NotManager_ShouldReturnForbidden()
    {
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var transferRequest = FakeTransferRuleRequestDto.CreateValid(_fixture);

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/rules/{transferRequest.RuleId}/transfer", transferRequest.Body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task TransferRule_DoesNotBelongToOrganization_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], SystemConstants.DefaultOrganization);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var transferRequest = FakeTransferRuleRequestDto.CreateValid(_fixture);

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/rules/{transferRequest.RuleId}/transfer", transferRequest.Body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task TransferRule_BelongsToDisabledOrganization_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], DisabledOrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var transferRequest = FakeTransferRuleRequestDto.CreateValid(_fixture);

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/rules/{transferRequest.RuleId}/transfer", transferRequest.Body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task TransferRule_GivenInvalidRequest_ShouldReturnBadRequest()
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

        var targetRuleSetEntity = FakeRuleSetEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            IsDeleted = false,
            Rules = Enumerable.Range(0, 50).Select(
                _ => FakeRuleEntity.CreateValid(_fixture) with
                {
                    OrganizationId = OrganizationId,
                    Organization = null!,
                    IsEnabled = true,
                    IsDeleted = false
                }).ToList()
        };

        await DatabaseHelper.CreateRuleSets(testFixture.Services, [ruleSetEntity, targetRuleSetEntity]);

        var transferRequest = FakeTransferRuleRequestDto.CreateValid(_fixture) with
        {
            RuleId = ruleSetEntity.Rules.First().RuleId,
            OrganizationId = OrganizationId,
            Body = new TransferRuleRequestBodyDto
            {
                TargetRuleSetId = targetRuleSetEntity.RuleSetId
            }
        };

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/rules/{transferRequest.RuleId}/transfer", transferRequest.Body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task TransferRule_GivenValidRequest_ShouldReturnNoContent()
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

        var targetRuleSetEntity = FakeRuleSetEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            IsDeleted = false
        };

        await DatabaseHelper.CreateRuleSets(testFixture.Services, [ruleSetEntity, targetRuleSetEntity]);

        var transferRequest = FakeTransferRuleRequestDto.CreateValid(_fixture) with
        {
            RuleId = ruleSetEntity.Rules.First().RuleId,
            OrganizationId = OrganizationId,
            Body = new TransferRuleRequestBodyDto
            {
                TargetRuleSetId = targetRuleSetEntity.RuleSetId
            }
        };

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/rules/{transferRequest.RuleId}/transfer", transferRequest.Body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
        var transferredRuleEntity = context.Rules.FirstOrDefault(x => x.RuleId == transferRequest.RuleId);
        transferredRuleEntity.Should().NotBeNull();
        transferredRuleEntity!.RuleSetId.Should().Be(transferRequest.Body.TargetRuleSetId);
    }

    [Fact]
    public async Task CopyRule_NotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var copyRequest = FakeTransferRuleRequestDto.CreateValid(_fixture);

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/rules/{copyRequest.RuleId}/copy", copyRequest.Body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task CopyRule_NotManager_ShouldReturnForbidden()
    {
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var copyRequest = FakeTransferRuleRequestDto.CreateValid(_fixture);

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/rules/{copyRequest.RuleId}/copy", copyRequest.Body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task CopyRule_DoesNotBelongToOrganization_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], SystemConstants.DefaultOrganization);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var copyRequest = FakeTransferRuleRequestDto.CreateValid(_fixture);

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/rules/{copyRequest.RuleId}/copy", copyRequest.Body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task CopyRule_BelongsToDisabledOrganization_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], DisabledOrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var copyRequest = FakeTransferRuleRequestDto.CreateValid(_fixture);

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/rules/{copyRequest.RuleId}/copy", copyRequest.Body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task CopyRule_GivenInvalidRequest_ShouldReturnBadRequest()
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

        var targetRuleSetEntity = FakeRuleSetEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            IsDeleted = false,
            Rules = Enumerable.Range(0, 50).Select(
                _ => FakeRuleEntity.CreateValid(_fixture) with
                {
                    OrganizationId = OrganizationId,
                    Organization = null!,
                    IsEnabled = true,
                    IsDeleted = false
                }).ToList()
        };

        await DatabaseHelper.CreateRuleSets(testFixture.Services, [ruleSetEntity, targetRuleSetEntity]);

        var copyRequest = FakeTransferRuleRequestDto.CreateValid(_fixture) with
        {
            RuleId = ruleSetEntity.Rules.First().RuleId,
            OrganizationId = OrganizationId,
            Body = new TransferRuleRequestBodyDto
            {
                TargetRuleSetId = targetRuleSetEntity.RuleSetId
            }
        };

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/rules/{copyRequest.RuleId}/copy", copyRequest.Body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task CopyRule_GivenValidRequest_ShouldReturnCreated()
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

        var targetRuleSetEntity = FakeRuleSetEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            IsDeleted = false
        };

        await DatabaseHelper.CreateRuleSets(testFixture.Services, [ruleSetEntity, targetRuleSetEntity]);

        var copyRequest = FakeTransferRuleRequestDto.CreateValid(_fixture) with
        {
            RuleId = ruleSetEntity.Rules.First().RuleId,
            OrganizationId = OrganizationId,
            Body = new TransferRuleRequestBodyDto
            {
                TargetRuleSetId = targetRuleSetEntity.RuleSetId
            }
        };

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/rules/{copyRequest.RuleId}/copy", copyRequest.Body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
        var copiedRuleEntity = context.Rules.FirstOrDefault(x => x.RuleId != copyRequest.RuleId);
        copiedRuleEntity.Should().NotBeNull();
        copiedRuleEntity!.RuleSetId.Should().Be(copyRequest.Body.TargetRuleSetId);
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
