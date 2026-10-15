using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using AutoFixture;
using BehaviouralTests.TestHelpers;
using Core.Enums;
using Core.Extensions;
using Core.Helpers;
using Core.Models.AuditLogs;
using FluentAssertions;
using Infrastructure;
using Infrastructure.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Constants;
using Presentation.Models.AuditLogs;
using Presentation.Models.Common;
using Presentation.Models.Transactions;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

namespace BehaviouralTests.Tests.Controllers;

public class PayloadProtectionTests(TestFixture testFixture) : IClassFixture<TestFixture>, IAsyncLifetime
{
    private readonly Fixture _fixture = AutoFixtureHelper.Create();
    private readonly HttpClient _httpClient = testFixture.CreateClient();

    private const string Email = "analyst@mail.com";
    private const string Password = "P@$$w0rd";
    private const string CardNumber = "4111111111111111";
    private const string SenderName = "Bob Smith";
    private static readonly Guid OrganizationId = new("2f1c8a41-6d3e-4b52-9c07-8e1a4d9f2b63");

    private static JsonObject Payload()
    {
        var payload = new JsonObject
        {
            ["amount"] = 24500,
            ["currency"] = "GBP",
            ["sender"] = new JsonObject
            {
                ["name"] = SenderName,
                ["card"] = CardNumber
            }
        };

        return payload;
    }

    [Fact]
    public async Task Evaluate_ShouldNotWriteTheCardNumberToTheDatabase()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Manager);

        // Act
        await EvaluateAsync();

        // Assert
        var stored = await StoredPayloadAsync();
        stored.Should().NotContain(CardNumber);
        stored.Should().StartWith(PayloadCipher.Version);
    }

    [Fact]
    public async Task Evaluate_ShouldNotWriteThePayloadInTheClear()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Manager);

        // Act
        await EvaluateAsync();

        // Assert
        var stored = await StoredPayloadAsync();
        stored.Should().NotContain(SenderName);
        stored.Should().NotContain("24500");
        stored.Should().NotContain("GBP");
    }

    [Fact]
    public async Task Evaluate_ShouldGiveTheSameCardTheSameTokenAcrossTwoPayments()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Manager);

        // Act
        var first = await EvaluateAsync();
        var second = await EvaluateAsync();

        // Assert
        var firstCard = await RevealedCardAsync(first);
        var secondCard = await RevealedCardAsync(second);

        firstCard.Should().Be(secondCard);
        firstCard.Should().StartWith(CardTokeniser.Prefix);
    }

    [Fact]
    public async Task GetTransaction_ShouldMaskThePayload()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Manager);
        var transactionId = await EvaluateAsync();

        // Act
        var response = await _httpClient.GetAsync($"/transactions/{transactionId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var resource = await response.Content.ReadDataFromJsonAsync<TransactionResourceDto>();
        var payload = resource!.Transaction.Payload;

        resource!.Transaction.PayloadMasked.Should().BeTrue();
        payload.Serialize().Should().NotContain(SenderName);
        payload["sender"]!["name"]!.GetValue<string>().Should().Be(PayloadMasker.Redacted);
        payload["amount"]!.GetValue<string>().Should().Be(PayloadMasker.Redacted);

        payload.Should().ContainKey("sender");
        payload["sender"]!.AsObject().Should().ContainKey("name");
    }

    [Fact]
    public async Task GetTransaction_ShouldShowTheLastFourOfACard()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Manager);
        var transactionId = await EvaluateAsync();

        // Act
        var response = await _httpClient.GetAsync($"/transactions/{transactionId}");

        // Assert
        var resource = await response.Content.ReadDataFromJsonAsync<TransactionResourceDto>();
        resource!.Transaction.Payload["sender"]!["card"]!.GetValue<string>().Should().EndWith("1111");
    }

    [Fact]
    public async Task ListTransactions_ShouldMaskEveryPayload()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Manager);
        await EvaluateAsync();

        // Act
        var response = await _httpClient.GetAsync("/transactions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var page = await response.Content.ReadFromJsonAsync<PagedResponseDto<TransactionSummaryResponseDto>>();
        page!.Data.Should().NotBeEmpty();
        page!.Data.Should().OnlyContain(x => x.PayloadMasked);
        page!.Data.Serialize().Should().NotContain(SenderName);
    }

    [Fact]
    public async Task RevealTransaction_ShouldReturnThePaymentAsItWasSent()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Manager);
        var transactionId = await EvaluateAsync();

        // Act
        var response = await RevealAsync(transactionId, "Reviewing a flagged payment");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var resource = await response.Content.ReadDataFromJsonAsync<TransactionResourceDto>();
        resource!.Transaction.PayloadMasked.Should().BeFalse();
        resource!.Transaction.Payload["sender"]!["name"]!.GetValue<string>().Should().Be(SenderName);
        resource!.Transaction.Payload["amount"]!.GetValue<int>().Should().Be(24500);
    }

    [Fact]
    public async Task RevealTransaction_ShouldNotHandBackTheCardNumber()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Manager);
        var transactionId = await EvaluateAsync();

        // Act
        var response = await RevealAsync(transactionId, "Reviewing a flagged payment");

        // Assert
        var resource = await response.Content.ReadDataFromJsonAsync<TransactionResourceDto>();
        resource!.Transaction.Payload.Serialize().Should().NotContain(CardNumber);
        resource!.Transaction.Payload["sender"]!["card"]!.GetValue<string>().Should().StartWith(CardTokeniser.Prefix);
    }

    [Fact]
    public async Task RevealTransaction_ShouldRecordItselfOnTheTransactionHistory()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Manager);
        var transactionId = await EvaluateAsync();
        const string reason = "Confirming the beneficiary before filing";

        // Act
        await RevealAsync(transactionId, reason);

        // Assert
        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var auditLog = await context.AuditLogs
            .SingleAsync(x => x.EntityId == transactionId && x.OperationId == (int)AuditOperation.Revealed);

        auditLog.OrganizationId.Should().Be(OrganizationId);
        auditLog.ChangedByUserId.Should().NotBeNull();
        auditLog.Snapshot.Should().Contain(reason);
    }

    [Fact]
    public async Task RevealTransaction_ShouldNotRecordThePayloadItRevealed()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Manager);
        var transactionId = await EvaluateAsync();

        // Act
        await RevealAsync(transactionId, "Reviewing a flagged payment");

        // Assert
        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var auditLog = await context.AuditLogs
            .SingleAsync(x => x.EntityId == transactionId && x.OperationId == (int)AuditOperation.Revealed);

        auditLog.Snapshot.Should().NotContain(SenderName);
        auditLog.Snapshot.Should().NotContain("24500");
    }

    [Fact]
    public async Task RevealTransaction_ShouldNotDisturbTheHistoryEitherSideOfIt()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Manager);
        var transactionId = await EvaluateAsync();

        // Act
        await RevealAsync(transactionId, "Reviewing a flagged payment");

        var response = await _httpClient.GetAsync($"/transactions/{transactionId}/history");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var page = await response.Content.ReadFromJsonAsync<PagedResponseDto<AuditLogResourceDto>>();
        var logs = page!.Data.Select(x => x.AuditLog).ToList();

        var reveal = logs.Single(x => x.Operation == nameof(AuditOperation.Revealed));
        reveal.Changes.Should().ContainSingle();
        reveal.Changes.Single().Field.Should().Be(nameof(RecordRevealRequest.Reason));

        logs.Single(x => x.Operation == nameof(AuditOperation.Created)).Changes.Should().NotBeEmpty();
    }

    [Fact]
    public async Task RevealTransaction_WithoutAReason_ShouldReturnBadRequest()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Manager);
        var transactionId = await EvaluateAsync();

        // Act
        var response = await RevealAsync(transactionId, string.Empty);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task RevealTransaction_AsAnOrdinaryUser_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Manager);
        var transactionId = await EvaluateAsync();

        await AuthenticationHelper.CreateUserAsync(testFixture.Services, "junior@mail.com", Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, "junior@mail.com", Password);

        // Act
        var response = await RevealAsync(transactionId, "Curiosity");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Evaluate_ShouldRoundTripAPayloadThroughEncryption()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Manager);

        // Act
        var transactionId = await EvaluateAsync();
        var response = await RevealAsync(transactionId, "Checking the round trip");

        // Assert
        var resource = await response.Content.ReadDataFromJsonAsync<TransactionResourceDto>();
        resource!.Transaction.Payload["currency"]!.GetValue<string>().Should().Be("GBP");
        resource!.Transaction.Payload["sender"]!["name"]!.GetValue<string>().Should().Be(SenderName);
    }

    [Fact]
    public async Task Evaluate_ShouldMintExactlyOneDataKeyPerOrganisation()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Manager);

        // Act
        await EvaluateAsync();
        await EvaluateAsync();
        await EvaluateAsync();

        // Assert
        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var keys = await context.OrganizationDataKeys.Where(x => x.OrganizationId == OrganizationId).ToListAsync();

        keys.Should().HaveCount(1);
        keys[0].WrappedDataKey.Should().StartWith(PayloadCipher.Version);
    }

    // Built by hand: the payload is the body and the rest are headers.
    private async Task<Guid> EvaluateAsync()
    {
        var reference = Guid.NewGuid().ToString();

        using var request = new HttpRequestMessage(HttpMethod.Post, "/rule-sets/evaluate")
        {
            Content = JsonContent.Create(Payload())
        };

        request.Headers.Add(HeaderNames.TransactionReference, reference);
        request.Headers.Add(HeaderNames.CreateCase, "false");

        var response = await _httpClient.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        return await context.Transactions
            .Where(x => x.OrganizationId == OrganizationId && x.ExternalReference == reference)
            .Select(x => x.TransactionId)
            .SingleAsync();
    }

    private Task<HttpResponseMessage> RevealAsync(Guid transactionId, string reason)
    {
        return _httpClient.PostAsJsonAsync($"/transactions/{transactionId}/reveal",
            new RevealTransactionBodyDto { Reason = reason });
    }

    private async Task<string> RevealedCardAsync(Guid transactionId)
    {
        var response = await RevealAsync(transactionId, "Comparing two payments");
        var resource = await response.Content.ReadDataFromJsonAsync<TransactionResourceDto>();

        return resource!.Transaction.Payload["sender"]!["card"]!.GetValue<string>();
    }

    private async Task<string> StoredPayloadAsync()
    {
        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        return await context.Transactions
            .Where(x => x.OrganizationId == OrganizationId)
            .Select(x => x.Payload)
            .FirstAsync();
    }

    private async Task AuthenticateAsync(UserRole userRole)
    {
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [userRole], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
    }

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
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}
