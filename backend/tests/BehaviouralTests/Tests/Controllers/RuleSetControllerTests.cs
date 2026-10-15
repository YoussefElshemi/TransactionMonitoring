using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using AutoFixture;
using BehaviouralTests.TestHelpers;
using Core.Constants;
using Core.Enums;
using Core.Helpers;
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
using Presentation.Models.Cases;
using Presentation.Models.Common;
using Presentation.Models.Rules;
using Presentation.Models.RuleSets;
using UnitTests.TestHelpers.FakeObjects.Core.Models.ApiClients;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Users;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.RuleSets;

namespace BehaviouralTests.Tests.Controllers;

public class RuleSetControllerTests(TestFixture testFixture) : IClassFixture<TestFixture>, IAsyncLifetime
{
    private readonly Fixture _fixture = AutoFixtureHelper.Create();
    private readonly HttpClient _httpClient = testFixture.CreateClient();

    private const string Email = "username@mail.com";
    private const string Password = "P@$$w0rd";
    private static readonly Guid OrganizationId = new("4d6389e9-f5d1-4325-a004-915865e3adec");
    private static readonly Guid DisabledOrganizationId = new("03a9b48e-4af2-40bd-a180-cac58bd930f3");

    [Fact]
    public async Task EvaluateAllRuleSets_NotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var evaluateAllRequest = FakeEvaluateAllRuleSetRequestDto.CreateValid(_fixture);

        // Act
        var response = await _httpClient.PostAsJsonAsync("/rule-sets/evaluate", evaluateAllRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task EvaluateAllRuleSets_AuthenticatedAsUser_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var evaluateAllRequest = FakeEvaluateAllRuleSetRequestDto.CreateValid(_fixture);

        // Act
        var response = await _httpClient.PostAsJsonAsync("/rule-sets/evaluate", evaluateAllRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Theory]
    [InlineData(UserRole.Manager)]
    [InlineData(UserRole.Admin)]
    public async Task EvaluateAllRuleSets_AuthenticatedAsPrivilegedUser_ShouldReturnOk(UserRole role)
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [role], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        await DatabaseHelper.CreateRuleSets(testFixture.Services, [
            FakeRuleSetEntity.CreateValid(_fixture) with
            {
                OrganizationId = OrganizationId,
                Organization = null!,
                IsEnabled = true,
                IsDeleted = false
            }
        ]);

        var evaluateAllRequest = FakeEvaluateAllRuleSetRequestDto.CreateValid(_fixture);

        // Act
        var response = await _httpClient.PostAsJsonAsync("/rule-sets/evaluate", evaluateAllRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task EvaluateAllRuleSets_ApiClientTokenSentAlongsideACookieSession_ShouldReturnOk()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.Admin], OrganizationId);
        await AuthenticationHelper.SetCookieAsync(_httpClient, Email, Password);

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
                IsDeleted = false
            }
        ]);

        var createdApiClient = await AuthenticationHelper.CreateApiClientAsync(testFixture.Services,
            FakeCreateApiClientRequest.CreateValid(_fixture) with
            {
                OrganizationId = new OrganizationId(OrganizationId),
                UserId = new ApplicationUserId(applicationUserEntity.Id)
            });

        await AuthenticationHelper.SetClientCredentialsTokenAsync(_httpClient, createdApiClient.ApiClient.ClientId, createdApiClient.ClientSecret);
        var evaluateAllRequest = FakeEvaluateAllRuleSetRequestDto.CreateValid(_fixture);

        // Act
        var response = await _httpClient.PostAsJsonAsync("/rule-sets/evaluate", evaluateAllRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task EvaluateAllRuleSets_DoesNotBelongToOrganization_ShouldReturnForbidden()
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

        await DatabaseHelper.CreateRuleSets(testFixture.Services, Enumerable.Range(0, 50).Select(
            _ => FakeRuleSetEntity.CreateValid(_fixture) with
            {
                OrganizationId = OrganizationId,
                Organization = null!,
                IsEnabled = true,
                IsDeleted = false
            })
        );

        var createdApiClient = await AuthenticationHelper.CreateApiClientAsync(testFixture.Services, createApiClientRequest);
        await AuthenticationHelper.SetClientCredentialsTokenAsync(_httpClient, createdApiClient.ApiClient.ClientId, createdApiClient.ClientSecret);
        var evaluateAllRequest = FakeEvaluateAllRuleSetRequestDto.CreateValid(_fixture);

        // Act
        var response = await _httpClient.PostAsJsonAsync("/rule-sets/evaluate", evaluateAllRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task EvaluateAllRuleSets_BelongsToDisabledOrganization_ShouldReturnForbidden()
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

        await DatabaseHelper.CreateRuleSets(testFixture.Services, Enumerable.Range(0, 50).Select(
            _ => FakeRuleSetEntity.CreateValid(_fixture) with
            {
                OrganizationId = OrganizationId,
                Organization = null!,
                IsEnabled = true,
                IsDeleted = false
            })
        );

        var createdApiClient = await AuthenticationHelper.CreateApiClientAsync(testFixture.Services, createApiClientRequest);
        await AuthenticationHelper.SetClientCredentialsTokenAsync(_httpClient, createdApiClient.ApiClient.ClientId, createdApiClient.ClientSecret);
        var evaluateAllRequest = FakeEvaluateAllRuleSetRequestDto.CreateValid(_fixture);

        // Act
        var response = await _httpClient.PostAsJsonAsync("/rule-sets/evaluate", evaluateAllRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task EvaluateAllRuleSets_TokenDoesNotCarryTheRuleSetsEvaluateScope_ShouldReturnForbidden()
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
            Scopes = [ApiScope.RulesEvaluate]
        };

        var createdApiClient = await AuthenticationHelper.CreateApiClientAsync(testFixture.Services, createApiClientRequest);
        await AuthenticationHelper.SetClientCredentialsTokenAsync(_httpClient, createdApiClient.ApiClient.ClientId, createdApiClient.ClientSecret);
        var evaluateAllRequest = FakeEvaluateAllRuleSetRequestDto.CreateValid(_fixture);

        // Act
        var response = await _httpClient.PostAsJsonAsync("/rule-sets/evaluate", evaluateAllRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task EvaluateAllRuleSets_TokenNarrowedToTheOtherScope_ShouldReturnForbidden()
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

        await AuthenticationHelper.SetClientCredentialsTokenAsync(
            _httpClient, createdApiClient.ApiClient.ClientId, createdApiClient.ClientSecret,
            ApiScopeHelper.ToScopeString(ApiScope.RulesEvaluate));

        var evaluateAllRequest = FakeEvaluateAllRuleSetRequestDto.CreateValid(_fixture);

        // Act
        var response = await _httpClient.PostAsJsonAsync("/rule-sets/evaluate", evaluateAllRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task EvaluateAllRuleSets_GivenValidRequest_ShouldReturnOk()
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

        await DatabaseHelper.CreateRuleSets(testFixture.Services, Enumerable.Range(0, 50).Select(
            _ => FakeRuleSetEntity.CreateValid(_fixture) with
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
            })
        );

        var createdApiClient = await AuthenticationHelper.CreateApiClientAsync(testFixture.Services, createApiClientRequest);
        await AuthenticationHelper.SetClientCredentialsTokenAsync(_httpClient, createdApiClient.ApiClient.ClientId, createdApiClient.ClientSecret);
        var evaluateAllRequest = FakeEvaluateAllRuleSetRequestDto.CreateValid(_fixture) with
        {
            CreateCase = true
        };

        // Act
        var response = await _httpClient.PostAsJsonAsync("/rule-sets/evaluate", evaluateAllRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
        var usageEntity = await context.Usages.FirstOrDefaultAsync();
        usageEntity!.Count.Should().Be(50);
    }

    [Fact]
    public async Task EvaluateAllRuleSets_GivenATransactionReference_ShouldRecordItOnTheTransaction()
    {
        await ArrangeEvaluableRuleSetsAsync(ruleSetCount: 3);
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.TransactionReference, "pay_9f3c21");

        var evaluateAllRequest = FakeEvaluateAllRuleSetRequestDto.CreateValid(_fixture);

        var response = await _httpClient.PostAsJsonAsync("/rule-sets/evaluate", evaluateAllRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadDataFromJsonAsync<EvaluateAllRulesResourceDto>();
        body!.Result.TransactionReference.Should().Be("pay_9f3c21");
        body.Result.TransactionId.Should().NotBeEmpty();

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var transactions = await context.Transactions.ToListAsync();
        transactions.Should().ContainSingle();
        transactions[0].ExternalReference.Should().Be("pay_9f3c21");
        transactions[0].TransactionId.Should().Be(body.Result.TransactionId);

        var evaluations = await context.RuleEvaluations.ToListAsync();
        evaluations.Should().HaveCount(3);
        evaluations.Should().OnlyContain(x => x.TransactionId == transactions[0].TransactionId);
    }

    [Fact]
    public async Task EvaluateAllRuleSets_RepeatedWithTheSameTransactionReference_ShouldReuseTheTransaction()
    {
        await ArrangeEvaluableRuleSetsAsync(ruleSetCount: 3);
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.TransactionReference, "pay_9f3c21");

        var evaluateAllRequest = FakeEvaluateAllRuleSetRequestDto.CreateValid(_fixture);

        var firstResponse = await _httpClient.PostAsJsonAsync("/rule-sets/evaluate", evaluateAllRequest);
        var secondResponse = await _httpClient.PostAsJsonAsync("/rule-sets/evaluate", evaluateAllRequest);

        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var first = await firstResponse.Content.ReadDataFromJsonAsync<EvaluateAllRulesResourceDto>();
        var second = await secondResponse.Content.ReadDataFromJsonAsync<EvaluateAllRulesResourceDto>();
        second!.Result.TransactionId.Should().Be(first!.Result.TransactionId);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        (await context.Transactions.CountAsync()).Should().Be(1);

        var evaluations = await context.RuleEvaluations.ToListAsync();
        evaluations.Should().HaveCount(6);
        evaluations.Should().OnlyContain(x => x.TransactionId == first.Result.TransactionId);
    }

    [Fact]
    public async Task EvaluateAllRuleSets_RepeatedWithTheSameTransactionReference_ShouldKeepTheFirstDecision()
    {
        await ArrangeEvaluableRuleSetsAsync(ruleSetCount: 3);
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.TransactionReference, "pay_9f3c21");

        var evaluateAllRequest = FakeEvaluateAllRuleSetRequestDto.CreateValid(_fixture);

        var firstResponse = await _httpClient.PostAsJsonAsync("/rule-sets/evaluate", evaluateAllRequest);
        var secondResponse = await _httpClient.PostAsJsonAsync("/rule-sets/evaluate", evaluateAllRequest);

        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var first = await firstResponse.Content.ReadDataFromJsonAsync<EvaluateAllRulesResourceDto>();
        var second = await secondResponse.Content.ReadDataFromJsonAsync<EvaluateAllRulesResourceDto>();

        first!.Result.Decision.Should().NotBeNull();
        second!.Result.Decision.Should().NotBeNull();
        second.Result.Decision!.DecisionId.Should().Be(first.Result.Decision!.DecisionId);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var decisions = await context.Decisions.ToListAsync();
        decisions.Should().ContainSingle();
        decisions[0].DecisionId.Should().Be(first.Result.Decision.DecisionId);
        decisions[0].TransactionId.Should().Be(first.Result.TransactionId);
    }

    [Fact]
    public async Task EvaluateAllRuleSets_ReusingATransactionReferenceForADifferentPayload_ShouldReturnBadRequest()
    {
        await ArrangeEvaluableRuleSetsAsync(ruleSetCount: 3);
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.TransactionReference, "pay_9f3c21");

        var evaluateAllRequest = FakeEvaluateAllRuleSetRequestDto.CreateValid(_fixture);
        var differentPayloadRequest = evaluateAllRequest with
        {
            Payload = new JsonObject { ["Key"] = "A different payment altogether" }
        };

        var firstResponse = await _httpClient.PostAsJsonAsync("/rule-sets/evaluate", evaluateAllRequest);
        var secondResponse = await _httpClient.PostAsJsonAsync("/rule-sets/evaluate", differentPayloadRequest);

        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        secondResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        secondResponse.Headers.Should().ContainKey(HeaderNames.CorrelationId);

        var problemDetails = await secondResponse.Content.ReadFromJsonAsync<ProblemDetails>();
        problemDetails!.Title.Should().Be("Transaction reference does not match its payload");
        problemDetails.Detail.Should().Contain("different payload");

        problemDetails.Extensions["transactionReference"].Should().NotBeNull();
        problemDetails.Extensions["transactionReference"]!.ToString().Should().Be("pay_9f3c21");

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        (await context.Transactions.CountAsync()).Should().Be(1);
        (await context.RuleEvaluations.CountAsync()).Should().Be(3);
    }

    [Fact]
    public async Task EvaluateAllRuleSets_ReusingTheReferenceOfADeletedTransaction_ShouldCreateANewOne()
    {
        await ArrangeEvaluableRuleSetsAsync(ruleSetCount: 1);

        var deletedTransaction = FakeTransactionEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            FriendlyName = "TX0000009999",
            ExternalReference = "pay_9f3c21",
            IsDeleted = true
        };

        await DatabaseHelper.CreateTransactions(testFixture.Services, [deletedTransaction]);

        _httpClient.DefaultRequestHeaders.Add(HeaderNames.TransactionReference, "pay_9f3c21");

        var evaluateAllRequest = FakeEvaluateAllRuleSetRequestDto.CreateValid(_fixture);

        var response = await _httpClient.PostAsJsonAsync("/rule-sets/evaluate", evaluateAllRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadDataFromJsonAsync<EvaluateAllRulesResourceDto>();
        body!.Result.TransactionReference.Should().Be("pay_9f3c21");
        body.Result.TransactionId.Should().NotBe(deletedTransaction.TransactionId);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var live = await context.Transactions.Where(x => !x.IsDeleted).ToListAsync();
        live.Should().ContainSingle();
        live[0].TransactionId.Should().Be(body.Result.TransactionId);
    }

    [Fact]
    public async Task EvaluateAllRuleSets_WithoutATransactionReference_ShouldMintAFriendlyName()
    {
        await ArrangeEvaluableRuleSetsAsync(ruleSetCount: 3);
        var evaluateAllRequest = FakeEvaluateAllRuleSetRequestDto.CreateValid(_fixture);

        var response = await _httpClient.PostAsJsonAsync("/rule-sets/evaluate", evaluateAllRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadDataFromJsonAsync<EvaluateAllRulesResourceDto>();
        body!.Result.TransactionReference.Should().BeNull();
        body.Result.TransactionId.Should().NotBeEmpty();

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var transaction = await context.Transactions.SingleAsync();
        transaction.ExternalReference.Should().BeNull();
        transaction.FriendlyName.Should().MatchRegex("^TX[0-9]{10}$");
    }

    [Fact]
    public async Task EvaluateAllRuleSets_RepeatedWithSameIdempotencyKey_ShouldReplayWithoutReevaluating()
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

        await DatabaseHelper.CreateRuleSets(testFixture.Services, Enumerable.Range(0, 50).Select(
            _ => FakeRuleSetEntity.CreateValid(_fixture) with
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
            })
        );

        var createdApiClient = await AuthenticationHelper.CreateApiClientAsync(testFixture.Services, createApiClientRequest);
        await AuthenticationHelper.SetClientCredentialsTokenAsync(_httpClient, createdApiClient.ApiClient.ClientId, createdApiClient.ClientSecret);
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.IdempotencyKey, Guid.NewGuid().ToString());

        var evaluateAllRequest = FakeEvaluateAllRuleSetRequestDto.CreateValid(_fixture) with
        {
            CreateCase = true
        };

        // Act
        var firstResponse = await _httpClient.PostAsJsonAsync("/rule-sets/evaluate", evaluateAllRequest);
        var secondResponse = await _httpClient.PostAsJsonAsync("/rule-sets/evaluate", evaluateAllRequest);

        // Assert
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        secondResponse.Headers.Should().ContainKey(HeaderNames.IdempotentReplay);

        var firstBody = await firstResponse.Content.ReadAsStringAsync();
        var secondBody = await secondResponse.Content.ReadAsStringAsync();
        secondBody.Should().Be(firstBody);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var usageEntity = await context.Usages.FirstOrDefaultAsync();
        usageEntity!.Count.Should().Be(50);
        (await context.RuleEvaluations.CountAsync()).Should().Be(50);
    }

    [Fact]
    public async Task EvaluateAllRuleSets_SameIdempotencyKeyOnDifferentEndpoint_ShouldReturnConflict()
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
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.IdempotencyKey, Guid.NewGuid().ToString());

        var evaluateAllRequest = FakeEvaluateAllRuleSetRequestDto.CreateValid(_fixture) with
        {
            CreateCase = true
        };

        // Act
        var firstResponse = await _httpClient.PostAsJsonAsync("/rule-sets/evaluate", evaluateAllRequest);
        var secondResponse = await _httpClient.PostAsJsonAsync(
            $"/rule-sets/{ruleSetEntity.RuleSetId}/evaluate", evaluateAllRequest);

        // Assert
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task EvaluateAllRuleSets_InvalidIdempotencyKey_ShouldReturnBadRequest()
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
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.IdempotencyKey, "not-a-guid");

        var evaluateAllRequest = FakeEvaluateAllRuleSetRequestDto.CreateValid(_fixture) with
        {
            CreateCase = true
        };

        // Act
        var response = await _httpClient.PostAsJsonAsync("/rule-sets/evaluate", evaluateAllRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateRuleSet_NotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var createRequest = FakeCreateRuleSetRequestDto.CreateValid(_fixture);

        // Act
        var response = await _httpClient.PostAsJsonAsync("/rule-sets", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task CreateRuleSet_NotManager_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var createRequest = FakeCreateRuleSetRequestDto.CreateValid(_fixture);

        // Act
        var response = await _httpClient.PostAsJsonAsync("/rule-sets", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task CreateRuleSet_DoesNotBelongToOrganization_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], SystemConstants.DefaultOrganization);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var createRequest = FakeCreateRuleSetRequestDto.CreateValid(_fixture);

        // Act
        var response = await _httpClient.PostAsJsonAsync("/rule-sets", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task CreateRuleSet_BelongsToDisabledOrganization_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], DisabledOrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var createRequest = FakeCreateRuleSetRequestDto.CreateValid(_fixture);

        // Act
        var response = await _httpClient.PostAsJsonAsync("/rule-sets", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task CreateRuleSet_GivenInvalidRequest_ShouldReturnBadRequest()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var createRequest = FakeCreateRuleSetRequestDto.CreateInvalid(_fixture);

        // Act
        var response = await _httpClient.PostAsJsonAsync("/rule-sets", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task CreateRuleSet_GivenValidRequest_ShouldReturnCreated()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var createRequest = FakeCreateRuleSetRequestDto.CreateValid(_fixture);

        // Act
        var response = await _httpClient.PostAsJsonAsync("/rule-sets", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);

        var createdRuleSet = await response.Content.ReadDataFromJsonAsync<RuleSetResourceDto>();
        createdRuleSet.Should().NotBeNull();
        createdRuleSet!.RuleSet.FriendlyName.Should().Be(createRequest.FriendlyName);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
        var ruleSetEntity = context.RuleSets.FirstOrDefault(x => x.RuleSetId == createdRuleSet.RuleSet.RuleSetId);
        ruleSetEntity.Should().NotBeNull();
    }

    [Fact]
    public async Task ListRuleSet_NotAuthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _httpClient.GetAsync("/rule-sets");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task ListRuleSet_DoesNotBelongToOrganization_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], SystemConstants.DefaultOrganization);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.GetAsync("/rule-sets");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task ListRuleSet_BelongsToDisabledOrganization_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], DisabledOrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.GetAsync("/rule-sets");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task ListRuleSet_GivenInvalidRequest_ShouldReturnBadRequest()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        await DatabaseHelper.CreateRuleSets(testFixture.Services, Enumerable.Range(0, 50).Select(
            _ => FakeRuleSetEntity.CreateValid(_fixture) with
            {
                OrganizationId = OrganizationId,
                Organization = null!,
                IsDeleted = false
            })
        );

        // Act
        var response = await _httpClient.GetAsync("/rule-sets?PageSize=200");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task ListRuleSet_GivenValidRequest_ShouldReturnOk()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        await DatabaseHelper.CreateRuleSets(testFixture.Services, Enumerable.Range(0, 50).Select(
            _ => FakeRuleSetEntity.CreateValid(_fixture) with
            {
                OrganizationId = OrganizationId,
                Organization = null!,
                IsDeleted = false
            })
        );

        // Act
        var response = await _httpClient.GetAsync("/rule-sets");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);

        var ruleSets = await response.Content.ReadFromJsonAsync<PagedResponseDto<RuleSetResourceDto>>();
        ruleSets.Should().NotBeNull();
        ruleSets!.Metadata.Should().NotBeNull();
        ruleSets.Metadata.PageCount.Should().Be(5);
        ruleSets.Metadata.PageNumber.Should().Be(1);
        ruleSets.Metadata.PageSize.Should().Be(10);
        ruleSets.Metadata.TotalCount.Should().Be(50);
        ruleSets.Metadata.HasMore.Should().BeTrue();
    }

    [Fact]
    public async Task GetRuleSet_NotAuthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _httpClient.GetAsync($"/rule-sets/{_fixture.Create<Guid>()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task GetRuleSet_DoesNotBelongToOrganization_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], SystemConstants.DefaultOrganization);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.GetAsync($"/rule-sets/{_fixture.Create<Guid>()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task GetRuleSet_BelongsToDisabledOrganization_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], DisabledOrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.GetAsync($"/rule-sets/{_fixture.Create<Guid>()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task GetRuleSet_GivenValidRequest_ShouldReturnOk()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        var ruleSetEntity = FakeRuleSetEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            IsDeleted = false
        };

        await DatabaseHelper.CreateRuleSets(testFixture.Services, [ruleSetEntity]);

        // Act
        var response = await _httpClient.GetAsync($"/rule-sets/{ruleSetEntity.RuleSetId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);

        var ruleSet = await response.Content.ReadDataFromJsonAsync<RuleSetResourceDto>();
        ruleSet.Should().NotBeNull();
        ruleSet!.RuleSet.RuleSetId.Should().Be(ruleSetEntity.RuleSetId);
        ruleSet.RuleSet.FriendlyName.Should().Be(ruleSetEntity.FriendlyName);
    }

    [Fact]
    public async Task UpdateRuleSet_NotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var updateRequest = FakeUpdateRuleSetRequestDto.CreateValid(_fixture);

        // Act
        var response = await _httpClient.PatchAsJsonAsync($"/rule-sets/{updateRequest.RuleSetId}", updateRequest.Body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task UpdateRuleSet_NotManager_ShouldReturnForbidden()
    {
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var updateRequest = FakeUpdateRuleSetRequestDto.CreateValid(_fixture);

        // Act
        var response = await _httpClient.PatchAsJsonAsync($"/rule-sets/{updateRequest.RuleSetId}", updateRequest.Body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task UpdateRuleSet_DoesNotBelongToOrganization_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], SystemConstants.DefaultOrganization);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var updateRequest = FakeUpdateRuleSetRequestDto.CreateValid(_fixture);

        // Act
        var response = await _httpClient.PatchAsJsonAsync($"/rule-sets/{updateRequest.RuleSetId}", updateRequest.Body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task UpdateRuleSet_BelongsToDisabledOrganization_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], DisabledOrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var updateRequest = FakeUpdateRuleSetRequestDto.CreateValid(_fixture);

        // Act
        var response = await _httpClient.PatchAsJsonAsync($"/rule-sets/{updateRequest.RuleSetId}", updateRequest.Body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task UpdateRuleSet_GivenInvalidRequest_ShouldReturnBadRequest()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var ruleSetEntity = FakeRuleSetEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            IsDeleted = false
        };

        await DatabaseHelper.CreateRuleSets(testFixture.Services, [ruleSetEntity]);

        var updateRequest = FakeUpdateRuleSetRequestDto.CreateInvalid(_fixture) with
        {
            RuleSetId = ruleSetEntity.RuleSetId,
            OrganizationId = OrganizationId
        };

        // Act
        var response = await _httpClient.PatchAsJsonAsync($"/rule-sets/{updateRequest.RuleSetId}", updateRequest.Body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task UpdateRuleSet_GivenValidRequest_ShouldReturnNoContent()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var ruleSetEntity = FakeRuleSetEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            IsDeleted = false
        };

        await DatabaseHelper.CreateRuleSets(testFixture.Services, [ruleSetEntity]);

        var updateRequest = FakeUpdateRuleSetRequestDto.CreateValid(_fixture) with
        {
            RuleSetId = ruleSetEntity.RuleSetId,
            OrganizationId = OrganizationId
        };

        // Act
        var response = await _httpClient.PatchAsJsonAsync($"/rule-sets/{updateRequest.RuleSetId}", updateRequest.Body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
        var updatedRuleSetEntity = context.RuleSets.FirstOrDefault(x => x.RuleSetId == updateRequest.RuleSetId);
        updatedRuleSetEntity.Should().NotBeNull();
        updatedRuleSetEntity!.FriendlyName.Should().Be(updateRequest.Body.FriendlyName);
    }

    [Fact]
    public async Task DeleteRuleSet_NotAuthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _httpClient.DeleteAsync($"/rule-sets/{_fixture.Create<Guid>()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task DeleteRuleSet_NotManager_ShouldReturnForbidden()
    {
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.DeleteAsync($"/rule-sets/{_fixture.Create<Guid>()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task DeleteRuleSet_DoesNotBelongToOrganization_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], SystemConstants.DefaultOrganization);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.DeleteAsync($"/rule-sets/{_fixture.Create<Guid>()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task DeleteRuleSet_BelongsToDisabledOrganization_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], DisabledOrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.DeleteAsync($"/rule-sets/{_fixture.Create<Guid>()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task DeleteRuleSet_GivenValidRequest_ShouldReturnNoContent()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var ruleSetEntity = FakeRuleSetEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            IsDeleted = false
        };

        await DatabaseHelper.CreateRuleSets(testFixture.Services, [ruleSetEntity]);

        // Act
        var response = await _httpClient.DeleteAsync($"/rule-sets/{ruleSetEntity.RuleSetId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
        var updatedRuleSetEntity = context.RuleSets.FirstOrDefault(x => x.RuleSetId == ruleSetEntity.RuleSetId);
        updatedRuleSetEntity.Should().NotBeNull();
        updatedRuleSetEntity!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task RecoverRuleSet_NotAuthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _httpClient.PostAsync($"/rule-sets/{_fixture.Create<Guid>()}/recover", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task RecoverRuleSet_NotManager_ShouldReturnForbidden()
    {
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.PostAsync($"/rule-sets/{_fixture.Create<Guid>()}/recover", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task RecoverRuleSet_DoesNotBelongToOrganization_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], SystemConstants.DefaultOrganization);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.PostAsync($"/rule-sets/{_fixture.Create<Guid>()}/recover", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task RecoverRuleSet_BelongsToDisabledOrganization_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], DisabledOrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.PostAsync($"/rule-sets/{_fixture.Create<Guid>()}/recover", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task RecoverRuleSet_GivenValidRequest_ShouldReturnNoContent()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var ruleSetEntity = FakeRuleSetEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            IsDeleted = true
        };

        await DatabaseHelper.CreateRuleSets(testFixture.Services, [ruleSetEntity]);

        // Act
        var response = await _httpClient.PostAsync($"/rule-sets/{ruleSetEntity.RuleSetId}/recover", null);

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
    public async Task EvaluateRuleSet_NotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var evaluateRequest = FakeEvaluateRuleSetRequestDto.CreateValid(_fixture);

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/rule-sets/{evaluateRequest.RuleSetId}/evaluate", evaluateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task EvaluateRuleSet_AuthenticatedAsUser_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var evaluateRequest = FakeEvaluateRuleSetRequestDto.CreateValid(_fixture);

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/rule-sets/{evaluateRequest.RuleSetId}/evaluate", evaluateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Theory]
    [InlineData(UserRole.Manager)]
    [InlineData(UserRole.Admin)]
    public async Task EvaluateRuleSet_AuthenticatedAsPrivilegedUser_ShouldReturnOk(UserRole role)
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [role], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        var ruleSetEntity = FakeRuleSetEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            IsEnabled = true,
            IsDeleted = false
        };

        await DatabaseHelper.CreateRuleSets(testFixture.Services, [ruleSetEntity]);

        var evaluateRequest = FakeEvaluateRuleSetRequestDto.CreateValid(_fixture) with
        {
            RuleSetId = ruleSetEntity.RuleSetId
        };

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/rule-sets/{evaluateRequest.RuleSetId}/evaluate", evaluateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task EvaluateRuleSet_DoesNotBelongToOrganization_ShouldReturnForbidden()
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
        var evaluateRequest = FakeEvaluateRuleSetRequestDto.CreateValid(_fixture) with
        {
            RuleSetId = ruleSetEntity.RuleSetId
        };

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/rule-sets/{evaluateRequest.RuleSetId}/evaluate", evaluateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task EvaluateRuleSet_BelongsToDisabledOrganization_ShouldReturnForbidden()
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
        var evaluateRequest = FakeEvaluateRuleSetRequestDto.CreateValid(_fixture) with
        {
            RuleSetId = ruleSetEntity.RuleSetId
        };

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/rule-sets/{evaluateRequest.RuleSetId}/evaluate", evaluateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task EvaluateRuleSet_GivenInvalidRequest_ShouldReturnBadRequest()
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
            IsDeleted = false
        };

        await DatabaseHelper.CreateRuleSets(testFixture.Services, [ruleSetEntity]);

        var createdApiClient = await AuthenticationHelper.CreateApiClientAsync(testFixture.Services, createApiClientRequest);
        await AuthenticationHelper.SetClientCredentialsTokenAsync(_httpClient, createdApiClient.ApiClient.ClientId, createdApiClient.ClientSecret);
        var evaluateRequest = FakeEvaluateRuleSetRequestDto.CreateValid(_fixture) with
        {
            RuleSetId = ruleSetEntity.RuleSetId
        };

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/rule-sets/{evaluateRequest.RuleSetId}/evaluate", evaluateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task EvaluateRuleSet_GivenValidRequest_ShouldReturnOk()
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
            IsEnabled = true,
            IsDeleted = false
        };

        await DatabaseHelper.CreateRuleSets(testFixture.Services, [ruleSetEntity]);

        var createdApiClient = await AuthenticationHelper.CreateApiClientAsync(testFixture.Services, createApiClientRequest);
        await AuthenticationHelper.SetClientCredentialsTokenAsync(_httpClient, createdApiClient.ApiClient.ClientId, createdApiClient.ClientSecret);
        var evaluateRequest = FakeEvaluateRuleSetRequestDto.CreateValid(_fixture) with
        {
            RuleSetId = ruleSetEntity.RuleSetId,
            CreateCase = true
        };

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/rule-sets/{evaluateRequest.RuleSetId}/evaluate", evaluateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
        var usageEntity = await context.Usages.CountAsync();
        usageEntity.Should().Be(1);
    }

    [Fact]
    public async Task EvaluateRuleSet_SeveralRulesFire_ShouldOpenOneCaseAtTheHighestSeverityListingEveryMatch()
    {
        // Arrange
        var applicationUserEntity = (FakeApplicationUser.CreateValid(_fixture) with
        {
            OrganizationId = new OrganizationId(OrganizationId),
            IsDeleted = new IsDeleted(false)
        }).ToEntity();

        await DatabaseHelper.CreateApplicationUsers(testFixture.Services, [applicationUserEntity]);

        var lowRule = MatchingRule("Key", "Value", RuleSeverity.Low);
        var highRule = MatchingRule("Key", "Value", RuleSeverity.High);
        var quietRule = MatchingRule("Key", "Something else entirely", RuleSeverity.Critical);

        var ruleSetEntity = FakeRuleSetEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            IsEnabled = true,
            IsDeleted = false,
            ReviewThreshold = null,
            Rules = [lowRule, highRule, quietRule]
        };

        await DatabaseHelper.CreateRuleSets(testFixture.Services, [ruleSetEntity]);

        var createApiClientRequest = FakeCreateApiClientRequest.CreateValid(_fixture) with
        {
            OrganizationId = new OrganizationId(OrganizationId),
            UserId = new ApplicationUserId(applicationUserEntity.Id)
        };

        var createdApiClient = await AuthenticationHelper.CreateApiClientAsync(testFixture.Services, createApiClientRequest);
        await AuthenticationHelper.SetClientCredentialsTokenAsync(_httpClient, createdApiClient.ApiClient.ClientId, createdApiClient.ClientSecret);

        _httpClient.DefaultRequestHeaders.Add(HeaderNames.CreateCase, "true");

        var payload = new JsonObject { ["Key"] = "Value" };

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/rule-sets/{ruleSetEntity.RuleSetId}/evaluate", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadDataFromJsonAsync<EvaluateRuleSetResourceDto>();
        body!.Result.Case.Should().NotBeNull();

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var cases = await context.Cases.ToListAsync();
        cases.Should().ContainSingle();
        cases[0].CaseId.Should().Be(body.Result.Case!.CaseId);
        cases[0].SeverityId.Should().Be((int)RuleSeverity.High);

        var links = await context.CaseRuleLinks.Where(x => x.CaseId == cases[0].CaseId).ToListAsync();
        links.Should().HaveCount(2);

        var evaluations = await context.RuleEvaluations.ToListAsync();
        evaluations.Should().HaveCount(3);

        var linkedRuleIds = evaluations
            .Where(x => links.Exists(link => link.RuleEvaluationId == x.RuleEvaluationId))
            .Select(x => x.RuleId);

        linkedRuleIds.Should().BeEquivalentTo([lowRule.RuleId, highRule.RuleId]);

        evaluations.Where(x => x.Matched).Should().OnlyContain(x => x.ConditionMatches != null);
        evaluations.Single(x => x.RuleId == quietRule.RuleId).ConditionMatches.Should().BeNull();
    }

    [Fact]
    public async Task EvaluateRuleSet_OpeningACase_ShouldExposeTheFiredVersionAndMatchedValuesOnTheCase()
    {
        // Arrange
        var applicationUserEntity = (FakeApplicationUser.CreateValid(_fixture) with
        {
            OrganizationId = new OrganizationId(OrganizationId),
            IsDeleted = new IsDeleted(false)
        }).ToEntity();

        await DatabaseHelper.CreateApplicationUsers(testFixture.Services, [applicationUserEntity]);

        var rule = MatchingRule("Key", "Value", RuleSeverity.High);

        var ruleSetEntity = FakeRuleSetEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            IsEnabled = true,
            IsDeleted = false,
            ReviewThreshold = null,
            Rules = [rule]
        };

        await DatabaseHelper.CreateRuleSets(testFixture.Services, [ruleSetEntity]);

        var createApiClientRequest = FakeCreateApiClientRequest.CreateValid(_fixture) with
        {
            OrganizationId = new OrganizationId(OrganizationId),
            UserId = new ApplicationUserId(applicationUserEntity.Id)
        };

        var createdApiClient = await AuthenticationHelper.CreateApiClientAsync(testFixture.Services, createApiClientRequest);
        await AuthenticationHelper.SetClientCredentialsTokenAsync(_httpClient, createdApiClient.ApiClient.ClientId, createdApiClient.ClientSecret);

        _httpClient.DefaultRequestHeaders.Add(HeaderNames.CreateCase, "true");

        var payload = new JsonObject { ["Key"] = "Value" };

        var response = await _httpClient.PostAsJsonAsync($"/rule-sets/{ruleSetEntity.RuleSetId}/evaluate", payload);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadDataFromJsonAsync<EvaluateRuleSetResourceDto>();

        var analystClient = testFixture.CreateClient();
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(analystClient, Email, Password);

        // Act
        var caseResponse = await analystClient.GetAsync($"/cases/{body!.Result.Case!.CaseId}");

        // Assert
        caseResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var @case = await caseResponse.Content.ReadDataFromJsonAsync<CaseResourceDto>();
        @case!.Case.Severity.Should().Be(RuleSeverity.High.ToString());
        @case.Case.RuleEvaluations.Should().ContainSingle();

        var evaluation = @case.Case.RuleEvaluations[0];
        evaluation.RuleId.Should().Be(rule.RuleId);
        evaluation.RuleVersion.RuleVersionId.Should().Be(rule.CurrentRuleVersionId);
        evaluation.RuleVersion.VersionNumber.Should().Be(rule.CurrentVersionNumber);
        evaluation.RuleVersion.Severity.Should().Be(RuleSeverity.High.ToString());

        evaluation.ConditionMatches.Should().ContainSingle();
        evaluation.ConditionMatches![0].Field.Should().Be("Key");
        evaluation.ConditionMatches[0].Condition.Should().Be(RuleCondition.EqualTo.ToString());
        evaluation.ConditionMatches[0].Expected!.ToJsonString().Should().Be("\"Value\"");
        evaluation.ConditionMatches[0].Actual!.ToJsonString().Should().Be("\"Value\"");
        evaluation.ConditionMatches[0].Matched.Should().BeTrue();
    }

    private RuleEntity MatchingRule(string field, string value, RuleSeverity severity)
    {
        var ruleBody = new RuleBody
        {
            Condition = RuleCondition.EqualTo,
            Field = new RuleField(field),
            Value = new RuleValue(value),
            And = null,
            Or = null,
            Not = null
        };

        var ruleEntity = FakeRuleEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            RuleBody = ruleBody.ToBase64String(),
            MatchPolarityId = (int)RuleMatchPolarity.MatchesWhenTrue,
            ActionId = (int)RuleAction.Review,
            SeverityId = (int)severity,
            RiskWeight = 0,
            HasActiveDeployment = false,
            IsEnabled = true,
            IsDeleted = false
        };

        return ruleEntity with
        {
            RuleVersions =
            [
                ruleEntity.RuleVersions.First() with
                {
                    RuleBody = ruleEntity.RuleBody,
                    MatchPolarityId = ruleEntity.MatchPolarityId,
                    ActionId = ruleEntity.ActionId,
                    SeverityId = ruleEntity.SeverityId,
                    RiskWeight = ruleEntity.RiskWeight,
                    OrganizationId = ruleEntity.OrganizationId
                }
            ]
        };
    }

    private async Task ArrangeEvaluableRuleSetsAsync(int ruleSetCount)
    {
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

        await DatabaseHelper.CreateRuleSets(testFixture.Services, Enumerable.Range(0, ruleSetCount).Select(
            _ => FakeRuleSetEntity.CreateValid(_fixture) with
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
            })
        );

        var createdApiClient = await AuthenticationHelper.CreateApiClientAsync(testFixture.Services, createApiClientRequest);
        await AuthenticationHelper.SetClientCredentialsTokenAsync(_httpClient, createdApiClient.ApiClient.ClientId, createdApiClient.ClientSecret);
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
