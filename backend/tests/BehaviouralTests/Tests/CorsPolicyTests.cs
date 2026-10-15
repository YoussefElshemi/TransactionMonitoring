using BehaviouralTests.TestHelpers;
using FluentAssertions;
using Presentation.Constants;

namespace BehaviouralTests.Tests;

public class CorsPolicyTests(TestFixture testFixture) : IClassFixture<TestFixture>
{
    private readonly HttpClient _httpClient = testFixture.CreateClient();

    [Theory]
    [InlineData(HeaderNames.CorrelationId)]
    [InlineData(HeaderNames.IdempotentReplay)]
    [InlineData(HeaderNames.ApiVersion)]
    [InlineData(HeaderNames.Deprecation)]
    [InlineData(HeaderNames.Sunset)]
    public async Task CrossOriginResponse_ExposesTheHeadersTheClientReads(string headerName)
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add("Origin", "http://localhost:3000");

        // Act
        var response = await _httpClient.SendAsync(request);

        // Assert
        response.Headers.TryGetValues("Access-Control-Expose-Headers", out var exposed).Should().BeTrue();
        Flatten(exposed).Should().Contain(headerName);
    }

    private static string[] Flatten(IEnumerable<string>? values)
    {
        return (values ?? [])
            .SelectMany(x => x.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            .ToArray();
    }
}
