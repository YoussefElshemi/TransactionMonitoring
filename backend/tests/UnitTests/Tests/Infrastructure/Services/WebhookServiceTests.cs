using System.Net;
using System.Runtime.CompilerServices;
using Core.Configs;
using Core.Constants;
using Core.ValueObjects;
using FluentAssertions;
using Infrastructure.Constants;
using Infrastructure.Services;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.OutboxMessages;

namespace UnitTests.Tests.Infrastructure.Services;

public class WebhookServiceTests : BaseTestClass
{
    private readonly Mock<IHttpClientFactory> _mockHttpClientFactory = new();
    private readonly WebhookService _sut;

    public WebhookServiceTests()
    {
        _sut = new WebhookService(_mockHttpClientFactory.Object, Options.Create(new AppConfig()));
    }

    [Fact]
    public async Task SendAsync_ThrowsException_ReturnsFalse()
    {
        // Arrange
        var outboxMessage = FakeOutboxMessage.CreateValid(Fixture);
        _mockHttpClientFactory
            .Setup(m => m.CreateClient(It.IsAny<string>()))
            .Throws(new Exception());

        // Act
        var result = await _sut.SendAsync(outboxMessage, CancellationToken.None);

        // Assert
        result.Result.Should().Be(new Result(false));
    }

    [Fact]
    public async Task SendAsync_ReturnsUnsuccessfulStatus_ReturnsFalse()
    {
        var outboxMessage = FakeOutboxMessage.CreateValid(Fixture);
        var httpClient = SetUpHttpClient(HttpStatusCode.InternalServerError);
        _mockHttpClientFactory
            .Setup(m => m.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);

        // Act
        var result = await _sut.SendAsync(outboxMessage, CancellationToken.None);

        // Assert
        result.Result.Should().Be(new Result(false));
    }

    [Fact]
    public async Task SendAsync_ReturnsSuccessfulStatus_ReturnsTrue()
    {
        var outboxMessage = FakeOutboxMessage.CreateValid(Fixture);
        var httpClient = SetUpHttpClient(HttpStatusCode.OK);
        _mockHttpClientFactory
            .Setup(m => m.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);

        // Act
        var result = await _sut.SendAsync(outboxMessage, CancellationToken.None);

        // Assert
        result.Result.Should().Be(new Result(true));
    }

    [Fact]
    public async Task SendAsync_GivenAReplay_EchoesTheEventVersionAndOriginalInTheHeaders()
    {
        // Arrange
        var replayOf = new OutboxMessageId(Guid.NewGuid());
        var outboxMessage = FakeOutboxMessage.CreateValid(Fixture) with { ReplayOfOutboxMessageId = replayOf };

        var httpClient = SetUpCapturingHttpClient(HttpStatusCode.OK, out var captured);
        _mockHttpClientFactory
            .Setup(m => m.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);

        // Act
        await _sut.SendAsync(outboxMessage, CancellationToken.None);

        // Assert
        captured.Value.Should().NotBeNull();
        captured.Value!.Headers.GetValues(HeaderNames.WebhookEvent).Should().ContainSingle(x => x == WebhookEvents.CaseCreated);
        captured.Value.Headers.GetValues(HeaderNames.WebhookVersion)
            .Should().ContainSingle(x => x == WebhookPayloadVersions.Current.ToString());
        captured.Value.Headers.GetValues(HeaderNames.WebhookReplayOf)
            .Should().ContainSingle(x => x == replayOf.ToString());
    }

    [Fact]
    public async Task SendAsync_GivenARowThatIsNotAReplay_OmitsTheReplayHeader()
    {
        // Arrange
        var outboxMessage = FakeOutboxMessage.CreateValid(Fixture);

        var httpClient = SetUpCapturingHttpClient(HttpStatusCode.OK, out var captured);
        _mockHttpClientFactory
            .Setup(m => m.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);

        // Act
        await _sut.SendAsync(outboxMessage, CancellationToken.None);

        // Assert
        captured.Value!.Headers.Contains(HeaderNames.WebhookReplayOf).Should().BeFalse();
    }

    [Fact]
    public async Task SendAsync_GivenAnOversizedResponseBody_KeepsOnlyWhatTheLogCanShow()
    {
        // Arrange
        var limit = new AppConfig().WebhookConfig.ResponseBodyLimit;

        var outboxMessage = FakeOutboxMessage.CreateValid(Fixture);
        _mockHttpClientFactory
            .Setup(m => m.CreateClient(It.IsAny<string>()))
            .Returns(SetUpHttpClient(HttpStatusCode.InternalServerError, new string('x', limit + 100)));

        // Act
        var result = await _sut.SendAsync(outboxMessage, CancellationToken.None);

        // Assert
        result.ResponseBody!.Value.ToString().Should().HaveLength(limit);
        ((int)result.StatusCode!.Value).Should().Be(500);
    }

    [Fact]
    public async Task SendAsync_GivenAnEmptyResponseBody_RecordsNothingRatherThanAnEmptyString()
    {
        // Arrange
        var outboxMessage = FakeOutboxMessage.CreateValid(Fixture);
        _mockHttpClientFactory
            .Setup(m => m.CreateClient(It.IsAny<string>()))
            .Returns(SetUpHttpClient(HttpStatusCode.OK));

        // Act
        var result = await _sut.SendAsync(outboxMessage, CancellationToken.None);

        // Assert
        result.ResponseBody.Should().BeNull();
    }

    [Fact]
    public async Task SendAsync_GivenARequestThatNeverGotAnAnswer_RecordsTheErrorWithoutAStatus()
    {
        // Arrange
        var outboxMessage = FakeOutboxMessage.CreateValid(Fixture);
        _mockHttpClientFactory
            .Setup(m => m.CreateClient(It.IsAny<string>()))
            .Throws(new HttpRequestException("Connection refused"));

        // Act
        var result = await _sut.SendAsync(outboxMessage, CancellationToken.None);

        // Assert
        result.StatusCode.Should().BeNull();
        result.ResponseBody!.Value.ToString().Should().Be("Connection refused");
    }

    private static HttpClient SetUpCapturingHttpClient(HttpStatusCode statusCode, out StrongBox<HttpRequestMessage?> captured)
    {
        var box = new StrongBox<HttpRequestMessage?>(null);
        captured = box;

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((request, _) => box.Value = request)
            .ReturnsAsync(new HttpResponseMessage { StatusCode = statusCode });

        return new HttpClient(handlerMock.Object);
    }
}