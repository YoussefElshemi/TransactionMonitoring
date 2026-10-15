using System.Net;
using System.Net.Http.Json;
using AutoFixture;
using BehaviouralTests.TestHelpers;
using Core.Constants;
using Core.Enums;
using FluentAssertions;
using Infrastructure.Configurations;
using Infrastructure.Entities;
using Presentation.Constants;
using Presentation.Models.Common;
using Presentation.Models.Transactions;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

namespace BehaviouralTests.Tests.Controllers;

public class TransactionControllerTests(TestFixture testFixture) : IClassFixture<TestFixture>, IAsyncLifetime
{
    private readonly Fixture _fixture = AutoFixtureHelper.Create();
    private readonly HttpClient _httpClient = testFixture.CreateClient();

    private const string Email = "username@mail.com";
    private const string Password = "P@$$w0rd";
    private static readonly Guid OrganizationId = new("4d6389e9-f5d1-4325-a004-915865e3adec");
    private static readonly Guid DisabledOrganizationId = new("03a9b48e-4af2-40bd-a180-cac58bd930f3");

    [Fact]
    public async Task ListTransactions_NotAuthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _httpClient.GetAsync("/transactions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task ListTransactions_DoesNotBelongToOrganization_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], SystemConstants.DefaultOrganization);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.GetAsync("/transactions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task ListTransactions_BelongsToDisabledOrganization_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], DisabledOrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.GetAsync("/transactions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task ListTransactions_GivenInvalidRequest_ShouldReturnBadRequest()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.GetAsync("/transactions?PageSize=200");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task ListTransactions_GivenValidRequest_ShouldReturnOk()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        await DatabaseHelper.CreateTransactions(testFixture.Services, Enumerable.Range(0, 50).Select(CreateTransaction));

        // Act
        var response = await _httpClient.GetAsync("/transactions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);

        var transactions = await response.Content.ReadFromJsonAsync<PagedResponseDto<TransactionSummaryResponseDto>>();
        transactions.Should().NotBeNull();
        transactions!.Metadata.PageCount.Should().Be(5);
        transactions.Metadata.TotalCount.Should().Be(50);
        transactions.Data.Should().OnlyContain(x => x.ExternalReference != null);
    }

    [Fact]
    public async Task ListTransactions_ShouldNotReportTheEvaluationsAndCasesTheDetailReadCarries()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        var transactionEntity = CreateTransaction(0);
        await DatabaseHelper.CreateTransactions(testFixture.Services, [transactionEntity]);

        // Act
        var listResponse = await _httpClient.GetAsync("/transactions");
        var detailResponse = await _httpClient.GetAsync($"/transactions/{transactionEntity.TransactionId}");

        // Assert
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        detailResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var listBody = await listResponse.Content.ReadAsStringAsync();
        listBody.Should().NotContain("ruleEvaluations");
        listBody.Should().NotContain("cases");

        var detailBody = await detailResponse.Content.ReadAsStringAsync();
        detailBody.Should().Contain("ruleEvaluations");
        detailBody.Should().Contain("cases");
    }

    [Fact]
    public async Task GetTransaction_NotAuthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _httpClient.GetAsync($"/transactions/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task GetTransaction_GivenUnknownTransaction_ShouldReturnNotFound()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.GetAsync($"/transactions/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task GetTransaction_BelongingToAnotherOrganization_ShouldReturnNotFound()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        var transactionEntity = CreateTransaction(0) with
        {
            OrganizationId = DisabledOrganizationId
        };

        await DatabaseHelper.CreateTransactions(testFixture.Services, [transactionEntity]);

        // Act
        var response = await _httpClient.GetAsync($"/transactions/{transactionEntity.TransactionId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task GetTransaction_GivenValidRequest_ShouldReturnOk()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        var transactionEntity = CreateTransaction(0);
        await DatabaseHelper.CreateTransactions(testFixture.Services, [transactionEntity]);

        // Act
        var response = await _httpClient.GetAsync($"/transactions/{transactionEntity.TransactionId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);

        var transaction = await response.Content.ReadDataFromJsonAsync<TransactionResourceDto>();
        transaction.Should().NotBeNull();
        transaction!.Transaction.TransactionId.Should().Be(transactionEntity.TransactionId);
        transaction.Transaction.ExternalReference.Should().Be(transactionEntity.ExternalReference);
        transaction.Transaction.Payload.Should().NotBeNull();
    }

    [Fact]
    public async Task ListTransactions_FilteredByExternalReference_ShouldFindThePaymentByItsOwnReference()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        var transactionEntity = CreateTransaction(0) with
        {
            ExternalReference = "pay_9f3c21"
        };

        await DatabaseHelper.CreateTransactions(testFixture.Services, [transactionEntity, CreateTransaction(1)]);

        // Act
        var response = await _httpClient.GetAsync("/transactions?externalReference=pay_9f3c21");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);

        var transactions = await response.Content.ReadFromJsonAsync<PagedResponseDto<TransactionSummaryResponseDto>>();
        transactions.Should().NotBeNull();
        transactions!.Data.Should().ContainSingle();
        transactions.Data.Single().TransactionId.Should().Be(transactionEntity.TransactionId);
        transactions.Data.Single().ExternalReference.Should().Be("pay_9f3c21");
    }

    [Fact]
    public async Task GetTransaction_GivenTransactionWithoutDecision_ShouldReturnNullDecision()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        var transactionEntity = CreateTransaction(0);
        await DatabaseHelper.CreateTransactions(testFixture.Services, [transactionEntity]);

        // Act
        var response = await _httpClient.GetAsync($"/transactions/{transactionEntity.TransactionId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var transaction = await response.Content.ReadDataFromJsonAsync<TransactionResourceDto>();
        transaction.Should().NotBeNull();
        transaction!.Transaction.Decision.Should().BeNull();
    }

    [Fact]
    public async Task GetTransaction_GivenHeldDecision_ShouldCarryTheDecision()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        var transactionEntity = CreateTransaction(0) with
        {
            ExternalReference = "pay_held_42"
        };

        await DatabaseHelper.CreateTransactions(testFixture.Services, [transactionEntity]);

        var decisionEntity = FakeDecisionEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            TransactionId = transactionEntity.TransactionId,
            Transaction = null!
        };

        await DatabaseHelper.CreateDecisions(testFixture.Services, [decisionEntity]);

        // Act
        var response = await _httpClient.GetAsync($"/transactions/{transactionEntity.TransactionId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);

        var transaction = await response.Content.ReadDataFromJsonAsync<TransactionResourceDto>();
        transaction.Should().NotBeNull();

        var decision = transaction!.Transaction.Decision;
        decision.Should().NotBeNull();
        decision!.DecisionId.Should().Be(decisionEntity.DecisionId);
        decision.TransactionId.Should().Be(transactionEntity.TransactionId);
        decision.TransactionReference.Should().Be("pay_held_42");
        decision.Status.Should().Be(nameof(DecisionStatus.Held));
        decision.ReasonCode.Should().Be(nameof(DecisionReasonCode.RuleReviewRequired));
        decision.ResolvedAt.Should().BeNull();
    }

    [Fact]
    public async Task GetTransactionHistory_AsUser_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        var transactionEntity = CreateTransaction(0);
        await DatabaseHelper.CreateTransactions(testFixture.Services, [transactionEntity]);

        // Act
        var response = await _httpClient.GetAsync($"/transactions/{transactionEntity.TransactionId}/history");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task GetTransactionHistory_AsManager_ShouldReturnOk()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.Manager], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        var transactionEntity = CreateTransaction(0);
        await DatabaseHelper.CreateTransactions(testFixture.Services, [transactionEntity]);

        // Act
        var response = await _httpClient.GetAsync($"/transactions/{transactionEntity.TransactionId}/history");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    private TransactionEntity CreateTransaction(int index)
    {
        return FakeTransactionEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            FriendlyName = $"TX{index:D10}",
            ExternalReference = $"pay_{index:D6}",
            IsDeleted = false
        };
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
