using System.Net;
using System.Text.Json;
using BehaviouralTests.TestHelpers;
using FluentAssertions;
using Presentation.Constants;

namespace BehaviouralTests.Tests;

public class SwaggerDocumentTests(TestFixture testFixture) : IClassFixture<TestFixture>
{
    private readonly HttpClient _httpClient = testFixture.CreateClient();

    private static readonly string[] IdempotentOperations =
    [
        "/rules/{RuleId}/evaluate",
        "/rule-sets/evaluate",
        "/rule-sets/{RuleSetId}/evaluate"
    ];

    [Theory]
    [MemberData(nameof(IdempotentOperationPaths))]
    public async Task SwaggerDocument_IdempotentOperation_DocumentsIdempotencyKeyHeader(string path)
    {
        // Arrange
        var document = await GetSwaggerDocumentAsync();

        // Act
        var parameters = document
            .RootElement.GetProperty("paths")
            .GetProperty(path)
            .GetProperty("post")
            .GetProperty("parameters")
            .EnumerateArray()
            .Where(x => x.GetProperty("in").GetString() == "header")
            .Select(x => x.GetProperty("name").GetString())
            .ToList();

        // Assert
        parameters.Should().Contain(HeaderNames.IdempotencyKey);
    }

    [Theory]
    [MemberData(nameof(IdempotentOperationPaths))]
    public async Task SwaggerDocument_IdempotentOperation_DocumentsReplayHeaderAndConflict(string path)
    {
        // Arrange
        var document = await GetSwaggerDocumentAsync();

        // Act
        var responses = document
            .RootElement.GetProperty("paths")
            .GetProperty(path)
            .GetProperty("post")
            .GetProperty("responses");

        // Assert
        responses.GetProperty("200").GetProperty("headers").TryGetProperty(HeaderNames.IdempotentReplay, out _)
            .Should().BeTrue();
        responses.TryGetProperty("409", out _).Should().BeTrue();
        responses.TryGetProperty("400", out _).Should().BeTrue();
    }

    [Fact]
    public async Task SwaggerDocument_EveryOperationOfferingTheIdempotencyKey_AlsoDocumentsTheReplayHeader()
    {
        // Arrange
        var document = await GetSwaggerDocumentAsync();
        var missingReplayHeader = new List<string>();

        // Act
        foreach (var path in document.RootElement.GetProperty("paths").EnumerateObject())
        {
            foreach (var operation in path.Value.EnumerateObject())
            {
                if (!OffersIdempotencyKey(operation.Value))
                {
                    continue;
                }

                if (!DocumentsReplayHeader(operation.Value))
                {
                    missingReplayHeader.Add($"{operation.Name.ToUpperInvariant()} {path.Name}");
                }
            }
        }

        // Assert
        missingReplayHeader.Should().BeEmpty();
    }

    private static bool OffersIdempotencyKey(JsonElement operation)
    {
        return operation.TryGetProperty("parameters", out var parameters) &&
               parameters.EnumerateArray().Any(x =>
                   x.GetProperty("in").GetString() == "header" &&
                   x.GetProperty("name").GetString() == HeaderNames.IdempotencyKey);
    }

    private static bool DocumentsReplayHeader(JsonElement operation)
    {
        return operation.GetProperty("responses").EnumerateObject()
            .Where(x => int.TryParse(x.Name, out var statusCode) && statusCode is >= 200 and < 300)
            .Any(x => x.Value.TryGetProperty("headers", out var headers) &&
                      headers.TryGetProperty(HeaderNames.IdempotentReplay, out _));
    }

    [Fact]
    public async Task SwaggerDocument_ExemptOperation_DoesNotDocumentIdempotencyKeyHeader()
    {
        // Arrange
        var document = await GetSwaggerDocumentAsync();

        // Act
        var body = document.RootElement
            .GetProperty("paths")
            .GetProperty("/oauth/token")
            .GetProperty("post")
            .ToString();

        // Assert
        body.Should().NotContain(HeaderNames.IdempotencyKey);
    }

    public static TheoryData<string> IdempotentOperationPaths()
    {
        var data = new TheoryData<string>();

        foreach (var path in IdempotentOperations)
        {
            data.Add(path);
        }

        return data;
    }

    private static readonly string[] OperationsWithoutResponseBody =
    [
        "POST /auth/logout",
        "POST /auth/resendConfirmationEmail",
        "POST /auth/forgotPassword",
        "POST /auth/resetPassword",
        "POST /rules/validate",
        "POST /organizations/{OrganizationId}/webhook-configurations/{WebhookConfigurationId}/test",
        "POST /organizations/my/webhook-configurations/{webhookConfigurationId}/test",

        "POST /payments/webhooks/provider"
    ];

    [Fact]
    public async Task SwaggerDocument_EveryOperation_DocumentsItsSuccessResponseModel()
    {
        // Arrange
        var document = await GetSwaggerDocumentAsync();
        var undocumented = new List<string>();

        // Act
        foreach (var path in document.RootElement.GetProperty("paths").EnumerateObject())
        {
            if (path.Name.StartsWith("/quartz-api", StringComparison.Ordinal))
            {
                continue;
            }

            foreach (var operation in path.Value.EnumerateObject())
            {
                var name = $"{operation.Name.ToUpperInvariant()} {path.Name}";

                if (!OperationsWithoutResponseBody.Contains(name) && !DocumentsSuccessResponseModel(operation.Value))
                {
                    undocumented.Add(name);
                }
            }
        }

        // Assert
        undocumented.Should().BeEmpty();
    }

    private static bool DocumentsSuccessResponseModel(JsonElement operation)
    {
        return operation.GetProperty("responses").EnumerateObject()
            .Where(x => int.TryParse(x.Name, out var statusCode) && statusCode is >= 200 and < 300)
            .Any(x => x.Name == "204" || x.Value.TryGetProperty("content", out _));
    }

    [Fact]
    public async Task SwaggerDocument_SecuritySchemes_DescribeBearerAndClientCredentials()
    {
        // Arrange
        var document = await GetSwaggerDocumentAsync();

        // Act
        var securitySchemes = document
            .RootElement.GetProperty("components")
            .GetProperty("securitySchemes");

        // Assert
        securitySchemes.TryGetProperty("ApiKey", out _).Should().BeFalse();
        securitySchemes.TryGetProperty("Bearer", out _).Should().BeTrue();

        var clientCredentials = securitySchemes
            .GetProperty("OAuth2")
            .GetProperty("flows")
            .GetProperty("clientCredentials");

        clientCredentials.GetProperty("tokenUrl").GetString().Should().Be("/oauth/token");
        clientCredentials.GetProperty("scopes").EnumerateObject().Select(x => x.Name)
            .Should().BeEquivalentTo("rules:evaluate", "rulesets:evaluate");
    }

    [Fact]
    public async Task SwaggerDocument_TokenEndpoint_AcceptsTheFormEncodedRfcRequest()
    {
        // Arrange
        var document = await GetSwaggerDocumentAsync();

        // Act
        var mediaTypes = document
            .RootElement.GetProperty("paths")
            .GetProperty("/oauth/token")
            .GetProperty("post")
            .GetProperty("requestBody")
            .GetProperty("content")
            .EnumerateObject()
            .Select(x => x.Name)
            .ToList();

        // Assert
        mediaTypes.Should().Contain("application/x-www-form-urlencoded");
    }

    private async Task<JsonDocument> GetSwaggerDocumentAsync()
    {
        var response = await _httpClient.GetAsync("/swagger/v1/swagger.json");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        return JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    }
}
