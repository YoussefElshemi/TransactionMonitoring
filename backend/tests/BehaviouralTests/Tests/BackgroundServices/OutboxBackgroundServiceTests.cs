using System.Net.Http.Json;
using AutoFixture;
using BehaviouralTests.TestHelpers;
using Polly;
using Polly.Retry;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

namespace BehaviouralTests.Tests.BackgroundServices;

public class OutboxBackgroundServiceTests(TestFixture testFixture) : IClassFixture<TestFixture>, IAsyncLifetime
{
    private const string WebhookBaseUrl = "http://localhost:5746";
    private readonly Fixture _fixture = AutoFixtureHelper.Create();
    private readonly AsyncRetryPolicy _retryPolicy = Policy
        .Handle<Exception>()
        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

    private string _webhookSessionId = null!;

    [Fact]
    public async Task ExecuteAsync_NoWebhooksToSend_DoesNotSendWebhooks()
    {
        await AssertWebhookIsSentAsync(0);
    }

    [Fact]
    public async Task ExecuteAsync_ManyWebhooksToSend_SendWebhooks()
    {
        // Arrange
        const int count = 5;
        var outboxMessageEntities = Enumerable.Range(0, count)
            .Select(_ => FakeOutboxMessageEntity.CreateValid(_fixture) with
            {
                Destination = $"{WebhookBaseUrl}/{_webhookSessionId}",
                Attempts = 0,
                IsProcessed = false,
                IsDeleted = false,
                UpdatedAt = DateTimeOffset.UtcNow
            }).ToList();

        // Act
        await DatabaseHelper.CreateOutboxMessages(testFixture.Services, outboxMessageEntities);

        // Assert
        await AssertWebhookIsSentAsync(count);
    }

    private static async Task<string> CreateWebhookSessionAsync()
    {
        using var httpClient = new HttpClient();
        var request = new
        {
            status_code = 200
        };

        var response = await httpClient.PostAsJsonAsync($"{WebhookBaseUrl}/api/session", request);
        response.EnsureSuccessStatusCode();

        var session = await response.Content.ReadFromJsonAsync<CreateSessionResponse>();
        return session!.Uuid;
    }

    private async Task DeleteWebhookSessionAsync()
    {
        using var httpClient = new HttpClient();
        await httpClient.DeleteAsync($"{WebhookBaseUrl}/api/session/{_webhookSessionId}");
    }

    private async Task AssertWebhookIsSentAsync(int expectedCount)
    {
        await _retryPolicy.ExecuteAsync(async () =>
        {
            using var httpClient = new HttpClient();
            var response = await httpClient.GetAsync($"{WebhookBaseUrl}/api/session/{_webhookSessionId}/requests");

            if (!response.IsSuccessStatusCode)
            {
                if (expectedCount > 0)
                {
                    throw new Exception($"Expected {expectedCount} requests but the API call failed with status code {response.StatusCode}.");
                }

                return;
            }

            var requests = await response.Content.ReadFromJsonAsync<List<object>>();
            if (requests == null || requests.Count != expectedCount)
            {
                throw new Exception($"Expected {expectedCount} requests but received {requests?.Count ?? 0}.");
            }
        });
    }

    public async Task InitializeAsync()
    {
        _webhookSessionId = await CreateWebhookSessionAsync();
        await testFixture.ResetDatabaseAsync();
    }

    public async Task DisposeAsync()
    {
        await DeleteWebhookSessionAsync();
    }

    private sealed record CreateSessionResponse(string Uuid);
}
